#region License
//  The MIT License (MIT)
//
//  Copyright (c) 2017, Cagatay Dogan
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//      The above copyright notice and this permission notice shall be included in
//      all copies or substantial portions of the Software.
//
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//      IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//      FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//      AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//      LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//      OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//      THE SOFTWARE.
#endregion License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Sweet.Redis.v2
{
    public class RedisAsyncClient : RedisDisposable, IRedisClient, IRedisPingable, IRedisIdentifiedObject
    {
        #region Constants

        private const int MaxBulkCount = 50000;

        #endregion Constants

        #region Static Members

        private static readonly int s_WaitTimeout = RedisCommon.IsWinPlatform ? 0 : 5;

        #endregion Static Members

        #region Field Members

        #region Readonly

        private readonly object m_Lock = new object();
        private readonly RedisAsyncQueue<RedisAsyncTask> m_SendWaitingQ = new RedisAsyncQueue<RedisAsyncTask>();

        #endregion Readonly

        private bool m_Authenticated;
        private int m_DbIndex = RedisConstants.UninitializedDbIndex;

        private IPEndPoint m_EndPoint;
        private RedisAsyncSocketBase m_Socket;
        private RedisConnectionSettings m_Settings;

        private long m_SendStatus;
        private bool m_UseBackgroundThread;

        private bool m_Constructed;
        private long m_ThreadRunning;
        private Thread m_BackgroundThread;
        private int m_ReceiveTimeout = RedisConstants.DefaultReceiveTimeout;

        private RedisRole m_ServerRole;
        private RedisServerMode m_ServerMode;

        protected long m_Id = RedisIDGenerator<RedisAsyncClient>.NextId();

        #endregion Field Members

        #region .Ctors

        public RedisAsyncClient(RedisConnectionSettings settings)
        {
            m_Settings = settings;
            m_ReceiveTimeout = settings.ReceiveTimeout;
            m_UseBackgroundThread = settings.UseBackgroundThread;

            Initialize(true, false);
            m_Constructed = true;
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnBeforeDispose(bool disposing, bool alreadyDisposed)
        {
            QuitInternal();
            base.OnBeforeDispose(disposing, alreadyDisposed);
        }

        protected override void OnDispose(bool disposing)
        {
            if (UseBackgroundThread)
            {
                AbortBackgroundThread();
            }

            base.OnDispose(disposing);
            lock (m_Lock)
            {
                SwitchSocket(null);
            }
        }

        #endregion Destructors

        #region Properties

        public bool Authenticated
        {
            get { return m_Authenticated; }
        }

        public bool Connected
        {
            get
            {
                lock (m_Lock)
                {
                    var socket = m_Socket;
                    if (socket == null)
                        return false;
                    return socket.Connected;
                }
            }
        }

        public virtual int DbIndex
        {
            get { return -1; }
        }

        public virtual EndPoint EndPoint
        {
            get
            {
                var endPoint = (EndPoint)m_EndPoint;
                if (endPoint == null)
                {
                    var socket = m_Socket;
                    if (socket != null)
                    {
                        endPoint = socket.EndPoint;
                        m_EndPoint = endPoint as IPEndPoint;

                        if (m_EndPoint == null)
                        {
                            var settings = Settings;
                            if (settings != null)
                            {
                                var endPoints = settings.EndPoints;
                                if (!endPoints.IsEmpty())
                                {
                                    foreach (var ep in endPoints)
                                        if (!ReferenceEquals(ep, null))
                                        {
                                            var ipEPs = ep.ResolveHost();
                                            if (!ipEPs.IsEmpty())
                                            {
                                                endPoint = (m_EndPoint = new IPEndPoint(ipEPs[0], ep.Port));
                                                break;
                                            }
                                        }
                                }
                            }
                        }
                    }
                }
                return endPoint;
            }
        }

        public virtual RedisRole ExpectedRole
        {
            get { return RedisRole.Undefined; }
        }

        public virtual long Id
        {
            get { return m_Id; }
        }

        public bool Reading
        {
            get
            {
                lock (m_Lock)
                {
                    var socket = m_Socket;
                    if (socket == null)
                        return false;
                    return socket.Reading;
                }
            }
        }

        public virtual RedisRole Role
        {
            get { return m_ServerRole; }
        }

        public bool Sending
        {
            get { return (m_SendStatus != RedisAsyncClientStatus.Idle && Connected); }
        }

        public RedisServerMode ServerMode
        {
            get { return m_ServerMode; }
        }

        public RedisConnectionSettings Settings
        {
            get { return m_Settings; }
        }

        protected internal virtual bool UseBackgroundThread
        {
            get { return m_UseBackgroundThread; }
            set
            {
                if (m_UseBackgroundThread != value)
                {
                    m_UseBackgroundThread = value;
                    if (value)
                        AbortBackgroundThread();
                    else if (m_SendStatus == 0 && !m_SendWaitingQ.IsEmpty)
                        InitBackgroundThread();
                }
            }
        }

        protected bool ThreadRunning
        {
            get { return m_ThreadRunning != RedisConstants.Zero; }
        }

        #endregion Properties

        #region Methods

        #region Connect / Disconnect

        public void Disconnect()
        {
            lock (m_Lock)
            {
                SwitchSocket(null);
            }
        }

        public virtual bool Ping()
        {
            if (!Disposed)
            {
                try
                {
                    var socket = m_Socket;
                    if (socket.IsAlive())
                    {
                        var response = RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                                     RedisCommandList.Ping,
                                                     RedisCommandType.SendAndReceive)
                        { Priority = RedisCommandPriority.High });

                        return ProcessPingResponse(response);
                    }
                }
                catch (Exception)
                { }
            }
            return false;
        }

        protected virtual bool ProcessPingResponse(RedisResult response)
        {
            return (response as RedisString)  == RedisConstants.PONG;
        }

        public void Quit()
        {
            if (!Disposed)
                QuitInternal();
        }

        private void QuitInternal()
        {
            var success = false;
            try
            {
                var socket = m_Socket;
                if (socket.IsAlive())
                {
                    var result = RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                                RedisCommandList.Quit,
                                                RedisCommandType.SendAndReceive)
                    { Priority = RedisCommandPriority.High }) as RedisString;

                    success = (result == RedisConstants.OK);
                }
            }
            catch (Exception) { }
            finally
            {
                try
                {
                    CancelAllSendQ();
                    Disconnect();
                }
                catch (Exception) { }
                finally
                {
                    if (success)
                        OnQuit();
                }
            }
        }

        protected virtual void OnQuit()
        { }

        private static ParameterizedThreadStart SenderThreadCallback = (state) =>
        {
            var client = (RedisAsyncClient)state;
            try
            {
                client.LoopBackgroundThread();
            }
            finally
            {
                Interlocked.Exchange(ref client.m_ThreadRunning, RedisConstants.Zero);
            }
        };

        protected virtual void LoopBackgroundThread()
        {
            while (!Disposed)
            {
                try
                {
                    lock (m_SendWaitingQ)
                    {
                        if (m_SendWaitingQ.IsEmpty)
                        {
                            if (Disposed)
                                return;
                            Monitor.Wait(m_SendWaitingQ);
                            if (Disposed)
                                return;
                            if (m_SendWaitingQ.IsEmpty)
                                continue;
                        }
                    }
                    TrySendQ(false);
                }
                catch (Exception)
                { }
            }
        }

        protected virtual void InitBackgroundThread()
        {
            if (!Disposed && UseBackgroundThread &&
                Interlocked.CompareExchange(ref m_ThreadRunning, RedisConstants.One, RedisConstants.Zero) == RedisConstants.Zero)
            {
                try
                {
                    m_BackgroundThread = new Thread(SenderThreadCallback);
                    m_BackgroundThread.IsBackground = true;
                    m_BackgroundThread.Start(this);
                }
                catch (Exception)
                {
                    Interlocked.Exchange(ref m_ThreadRunning, RedisConstants.Zero);
                }
            }
        }

        private bool Initialize(bool ctorCall, bool throwException = true)
        {
            ValidateNotDisposed();

            var success = false;
            try
            {
                lock (m_Lock)
                {
                    if (m_EndPoint == null)
                    {
                        var endPoints = m_Settings.EndPoints;
                        if (endPoints != null)
                        {
                            foreach (var endPoint in endPoints)
                            {
                                try
                                {
                                    if (!ReferenceEquals(endPoint, null))
                                    {
                                        var ipAddresses = endPoint.ResolveHost();
                                        if (!ipAddresses.IsEmpty())
                                        {
                                            Interlocked.Exchange(ref m_EndPoint, new IPEndPoint(ipAddresses[0], endPoint.Port));
                                            break;
                                        }
                                    }
                                }
                                catch (Exception)
                                { }
                            }
                        }

                        if (m_EndPoint == null)
                            throw new RedisFatalException("Can not resolve host address");
                    }

                    DoAfterInit();
                }
                success = true;
            }
            catch (Exception)
            {
                if (throwException)
                    throw;
            }
            finally
            {
                if ((success || !ctorCall) && UseBackgroundThread)
                    InitBackgroundThread();
            }
            return success;
        }

        protected virtual void DoAfterInit()
        {
            if (!Disposed)
            {
                var socket = m_Socket;
                if (socket == null || !socket.Connected)
                    CreateSocket(m_EndPoint);
            }
        }

        protected virtual void CreateSocket(IPEndPoint endPoint)
        {
            CloseSocket(m_Socket);
            NewSocket(endPoint).BeginConnect((socket) =>
            {
                DoConnected((RedisAsyncSocketBase)socket);
            });
        }

        protected virtual RedisAsyncSocketBase NewSocket(IPEndPoint endPoint)
        {
            var settings = m_Settings;
            return new RedisAsyncSocket(endPoint, settings.ReceiveTimeout, settings.SendTimeout,
                settings.ReadBufferSize, settings.BulkSendFactor);
        }

        private static WaitCallback TrySendQCallback = (state) =>
        {
            ((RedisAsyncClient)state).TrySendQ(false);
        };

        private void DoConnected(RedisAsyncSocketBase socket)
        {
            var connected = (socket != null) && socket.Connected;
            SwitchSocket(connected ? socket : null);

            var current = m_Socket;
            connected = (current != null) && current.Connected;

            if (!connected)
                CloseSocket(socket);
            else
            {
                OnConnect();

                if (UseBackgroundThread)
                    InitBackgroundThread();
                else if (m_Constructed && !m_SendWaitingQ.IsEmpty &&
                    m_SendStatus == RedisAsyncClientStatus.Idle)
                    ThreadPool.QueueUserWorkItem(TrySendQCallback, this);
            }
        }

        protected virtual void OnConnect()
        {
            var settings = m_Settings;

            Auth(settings.Password);
            SetClientName(settings.ClientName);

            if (!NeedsToDiscoverRole())
                m_ServerRole = ExpectedRole;
            else
            {
                var role = (m_ServerRole = DiscoverRole());
                ValidateRole(role);
            }

            var serverMode = (m_ServerMode = NeedsToDiscoverMode() ? DiscoverMode() : RedisServerMode.Standalone);
            if (serverMode == RedisServerMode.Cluster)
            {
                DiscoverClusterSlots();
            }
        }

        private void SwitchSocket(RedisAsyncSocketBase socket)
        {
            using (var oldSocket = Interlocked.Exchange(ref m_Socket, socket))
            {
                CloseSocket(oldSocket);
            }
        }

        private static void CloseSocket(RedisAsyncSocketBase socket)
        {
            try
            {
                if (socket != null)
                    socket.Close();
            }
            catch (Exception)
            { }
        }

        #endregion Connect / Disconnect

        #region Send

        public RedisResult[] Expect(RedisCommand[] commands)
        {
            if (commands != null)
            {
                var count = commands.Length;
                if (count > 0)
                {
                    ValidateNotDisposed();

                    var dbIndex = RedisConstants.UninitializedDbIndex;
                    for (var i = 0; i < count; i++)
                    {
                        var command = commands[i];

                        var tmpDbIndex = command.DbIndex;
                        if (dbIndex != tmpDbIndex &&
                            tmpDbIndex > RedisConstants.UninitializedDbIndex)
                        {
                            if (dbIndex > RedisConstants.UninitializedDbIndex)
                                throw new RedisFatalException("Multi-database commands not allowed in a single batch task");
                            dbIndex = tmpDbIndex;
                        }
                    }

                    if (m_DbIndex != dbIndex &&
                        dbIndex > RedisConstants.UninitializedDbIndex)
                        SelectDB(dbIndex);

                    return RunSyncTask(commands);
                }
            }
            return null;
        }

        public RedisResult Expect(RedisCommand command)
        {
            if (command != null)
            {
                ValidateNotDisposed();

                var dbIndex = command.DbIndex;
                if (m_DbIndex != dbIndex &&
                    dbIndex > RedisConstants.UninitializedDbIndex)
                    SelectDB(dbIndex);

                return RunSyncTask(command);
            }
            return null;
        }

        protected RedisResult RunSyncTask(RedisCommand command)
        {
            ValidateNotDisposed();

            var asyncTask = new RedisAsyncTask(command);
            if (!UseBackgroundThread)
                m_SendWaitingQ.Enqueue(asyncTask, command.Priority == RedisCommandPriority.High);
            else
            {
                lock (m_SendWaitingQ)
                {
                    m_SendWaitingQ.Enqueue(asyncTask, command.Priority == RedisCommandPriority.High);
                    Monitor.PulseAll(m_SendWaitingQ);
                }
            }

            if (UseBackgroundThread)
                InitBackgroundThread();
            else TrySendQ(false);

            if (command.CommandType == RedisCommandType.SendNotReceive)
                return RedisVoid.Default;

            if (!asyncTask.IsCompleted)
            {
                var spinCount = 0;
                var spinStartTime = Environment.TickCount;
                var notWinPlatform = !RedisCommon.IsWinPlatform;

                do
                {
                    if (Environment.TickCount - spinStartTime >= m_ReceiveTimeout)
                        throw new RedisFatalException("Request Timeout", RedisErrorCode.SocketError);

                    var socket = m_Socket;
                    if ((socket != null) && socket.ReceiveCallRequired)
                    {
                        socket.BeginReceive();
                        asyncTask.Wait(1);
                    }
                }
                while (asyncTask.Wait(notWinPlatform || ++spinCount % 50 != 0 ? s_WaitTimeout : spinCount / 10));
            }

            var result = asyncTask.Result;
            if (result == null && asyncTask.IsCanceled)
                throw new RedisFatalException(asyncTask.Status.ToString("F"));

            if (result is RedisError)
                throw new RedisFatalException(((RedisError)result).Value);

            return result;
        }

        protected RedisResult[] RunSyncTask(RedisCommand[] commands)
        {
            ValidateNotDisposed();

            var count = commands.Length;
            var asyncTasks = new Queue<RedisAsyncTask>(count);

            lock (m_SendWaitingQ.SyncLock)
            {
                for (var i = 0; i < count; i++)
                {
                    var command = commands[i];

                    var asyncTask = new RedisAsyncTask(command);
                    asyncTasks.Enqueue(asyncTask);

                    m_SendWaitingQ.Enqueue(asyncTask, command.Priority == RedisCommandPriority.High);
                }

                if (UseBackgroundThread)
                {
                    lock (m_SendWaitingQ)
                    {
                        Monitor.PulseAll(m_SendWaitingQ);
                    }
                }
            }

            if (UseBackgroundThread)
                InitBackgroundThread();
            else TrySendQ(false);

            var results = new List<RedisResult>(count);
            while (asyncTasks.Count > 0)
            {
                var asyncTask = asyncTasks.Dequeue();
                if (!asyncTask.IsCompleted)
                {
                    var spinCount = 0;
                    var spinStartTime = Environment.TickCount;
                    var notWinPlatform = !RedisCommon.IsWinPlatform;

                    do
                    {
                        if (Environment.TickCount - spinStartTime >= m_ReceiveTimeout)
                            throw new RedisFatalException("Request Timeout", RedisErrorCode.SocketError);

                        var socket = m_Socket;
                        if ((socket != null) && socket.ReceiveCallRequired)
                        {
                            socket.BeginReceive();
                            asyncTask.Wait(1);
                        }
                    }
                    while (asyncTask.Wait(notWinPlatform || ++spinCount % 50 != 0 ? s_WaitTimeout : spinCount / 10));
                }

                var result = asyncTask.Result;
                if (result == null && asyncTask.IsCanceled)
                    throw new RedisFatalException(asyncTask.Status.ToString("F"));

                if (result is RedisError)
                    throw new RedisFatalException(((RedisError)result).Value);

                results.Add(result);
            }

            return results.ToArray();
        }

        public Task<RedisResult> ExpectAsync(RedisCommand command)
        {
            if (command != null)
            {
                return RunAsyncTask(command);
            }
            return null;
        }

        protected Task<RedisResult> RunAsyncTask(RedisCommand command)
        {
            var tcs = new TaskCompletionSource<RedisResult>();
            var asyncTask = new RedisAsyncTask(command, tcs);

            if (!UseBackgroundThread)
                m_SendWaitingQ.Enqueue(asyncTask, command.Priority == RedisCommandPriority.High);
            else
            {
                lock (m_SendWaitingQ)
                {
                    m_SendWaitingQ.Enqueue(asyncTask, command.Priority == RedisCommandPriority.High);
                    Monitor.PulseAll(m_SendWaitingQ);
                }
            }

            if (UseBackgroundThread)
                InitBackgroundThread();
            else TrySendQ(false);

            return tcs.Task;
        }

        private static WaitCallback ProcessQCallback = (state) =>
        {
            ((RedisAsyncClient)state).ProcessQ(true);
        };

        private void TrySendQ(bool forceAsync = false)
        {
            if (Interlocked.CompareExchange(ref m_SendStatus, RedisAsyncClientStatus.Sending, RedisAsyncClientStatus.Idle) ==
                       RedisAsyncClientStatus.Idle)
            {
                try
                {
                    var count = m_SendWaitingQ.Count;
                    if (count == 0)
                        Interlocked.Exchange(ref m_SendStatus, RedisAsyncClientStatus.Idle);
                    else
                    {
                        if (!forceAsync && count == 1)
                        {
                            ProcessQ();
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(ProcessQCallback, this);
                    }
                }
                catch (Exception)
                {
                    Interlocked.Exchange(ref m_SendStatus, RedisAsyncClientStatus.Idle);
                    throw;
                }
            }
        }

        private void ProcessQ(bool isAsync = false)
        {
            try
            {
                do
                {
                    var socket = m_Socket;
                    if (socket == null || !socket.Connected)
                    {
                        if (!Initialize(false, false))
                            socket = null;
                        else socket = m_Socket;

                        if (socket == null || !socket.Connected)
                        {
                            CancelAllSendQ();
                            return;
                        }
                    }

                    var count = m_SendWaitingQ.Count;
                    if (count == 0)
                        return;

                    if (count > 1)
                    {
                        do
                        {
                            var bulkCount = 0;
                            var asyncTasks = new List<RedisAsyncTask>(Math.Min(MaxBulkCount, count));

                            RedisAsyncTask asyncTask;
                            while (bulkCount++ < MaxBulkCount &&
                                m_SendWaitingQ.TryDequeue(out asyncTask))
                                asyncTasks.Add(asyncTask);

                            try
                            {
                                socket.Write(asyncTasks);
                            }
                            catch (Exception e)
                            {
                                var anySync = false;
                                for (var i = 0; i < count; i++)
                                {
                                    asyncTask = asyncTasks[i];

                                    asyncTask.TrySetException(e);
                                    if (!asyncTask.IsAsync)
                                        anySync = true;
                                }

                                if (anySync)
                                    throw;
                            }
                        } while (!m_SendWaitingQ.IsEmpty);
                    }
                    else
                    {
                        RedisAsyncTask asyncTask;
                        while (m_SendWaitingQ.TryDequeue(out asyncTask))
                        {
                            try
                            {
                                socket.Write(asyncTask);
                            }
                            catch (Exception e)
                            {
                                asyncTask.TrySetException(e);
                                if (!asyncTask.IsAsync)
                                    throw;

                                if ((e is SocketException) ||
                                    (e is IOException))
                                {
                                    if (!Initialize(false, false))
                                        socket = null;
                                    else socket = m_Socket;
                                }
                            }
                        }
                    }
                } while (!Disposed && isAsync);
            }
            finally
            {
                Interlocked.Exchange(ref m_SendStatus, RedisAsyncClientStatus.Idle);
                if (!(Disposed || m_SendWaitingQ.IsEmpty))
                {
                    // If is async call and not running in background thread
                    TrySendQ(isAsync && !UseBackgroundThread);
                }
            }
        }

        private void CancelAllSendQ()
        {
            try
            {
                RedisAsyncTask asyncTask;
                while (m_SendWaitingQ.TryDequeue(out asyncTask))
                {
                    try
                    {
                        asyncTask.TrySetCanceled();
                    }
                    catch (Exception)
                    { }
                }
            }
            catch (Exception)
            { }
        }

        #endregion Send

        #region Receive

        protected virtual void Receive()
        {
            var socket = Connect();
            if (socket != null && socket.Connected)
                socket.BeginReceive();
        }

        protected virtual RedisAsyncSocketBase Connect()
        {
            var socket = m_Socket;
            if (socket == null || !socket.Connected)
            {
                if (!Initialize(false))
                    socket = null;
                else socket = m_Socket;
            }
            return (socket != null && socket.Connected) ? socket : null;
        }

        #endregion Receive

        #region Base Methods

        protected void AbortBackgroundThread()
        {
            try
            {
                var thread = Interlocked.Exchange(ref m_BackgroundThread, null);
                lock (m_SendWaitingQ)
                {
                    Monitor.PulseAll(m_SendWaitingQ);
                }

                if (thread != null)
                    thread.Abort();
            }
            catch (Exception)
            { }
        }

        protected bool Auth(string password)
        {
            if (!password.IsEmpty())
            {
                ValidateNotDisposed();
                m_Authenticated = false;

                var result = RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                            RedisCommandList.Auth,
                                            RedisCommandType.SendAndReceive,
                                            password.ToBytes())
                { Priority = RedisCommandPriority.High }) as RedisString;

                if (!ReferenceEquals(result, null))
                    m_Authenticated = result.Value == RedisConstants.OK;
            }
            return true;
        }

        protected bool SetClientName(string clientName)
        {
            if (!clientName.IsEmpty())
            {
                ValidateNotDisposed();

                var result = RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                            RedisCommandList.Client,
                                            RedisCommandType.SendAndReceive,
                                            RedisCommandList.SetName,
                                            clientName.ToBytes())
                { Priority = RedisCommandPriority.High }) as RedisString;

                if (!ReferenceEquals(result, null))
                    return result.Value == RedisConstants.OK;
            }
            return false;
        }

        protected bool SelectDB(int dbIndex)
        {
            if (dbIndex >= RedisConstants.MinDbIndex && dbIndex <= RedisConstants.MaxDbIndex)
            {
                var isOK = false;
                try
                {
                    var result = RunSyncTask(new RedisCommand(dbIndex,
                                                RedisCommandList.Select,
                                                RedisCommandType.SendAndReceive,
                                                dbIndex.ToBytes())
                    { Priority = RedisCommandPriority.High }) as RedisString;

                    if (!ReferenceEquals(result, null))
                    {
                        isOK = result.Value == RedisConstants.OK;
                        if (isOK)
                            Interlocked.Exchange(ref m_DbIndex, dbIndex);
                    }
                }
                catch (Exception)
                { }
                return isOK;
            }
            return true;
        }

        public RedisServerInfo GetServerInfo()
        {
            try
            {
                if (!Disposed)
                {
                    var bytes = RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                                RedisCommandList.Info,
                                                RedisCommandType.SendAndReceive) { Priority = RedisCommandPriority.High }) as RedisBytes;

                    if (!ReferenceEquals(bytes, null))
                    {
                        var lines = bytes.Value.ToUTF8String();
                        if (!lines.IsEmpty())
                            return RedisServerInfo.Parse(lines);
                    }
                }
            }
            catch (Exception)
            { }
            return null;
        }

        #endregion Base Methods

        #region Clusters

        protected virtual bool NeedsToDiscoverMode()
        {
            return Role != RedisRole.Sentinel;
        }

        protected virtual RedisServerMode DiscoverMode()
        {
            if (!Disposed)
            {
                try
                {
                    var info = GetServerInfo();
                    if (!ReferenceEquals(info, null))
                    {
                        var serverInfo = info.Server;
                        if (!ReferenceEquals(serverInfo, null))
                        {
                            var redisMode = (serverInfo.RedisMode ?? String.Empty).ToLowerInvariant();
                            if (redisMode == "sentinel")
                                return RedisServerMode.Sentinel;

                            if (redisMode == "cluster")
                                return RedisServerMode.Cluster;

                            return RedisServerMode.Standalone;
                        }
                    }
                }
                catch (Exception e)
                { }
            }
            return RedisServerMode.Standalone;
        }

        protected virtual void DiscoverClusterSlots()
        {
            try
            {
                if (!Disposed && m_ServerMode == RedisServerMode.Cluster)
                {
                    var ep = EndPoint;
                    if (ep != null)
                    {
                        var host = (string)null;
                        var port = -1;

                        var rep = ep as RedisEndPoint;
                        if (!ReferenceEquals(rep, null))
                        {
                            host = rep.Host;
                            port = rep.Port;
                        }
                        else
                        {
                            var ipEp = ep as IPEndPoint;
                            if (ipEp != null)
                            {
                                host = ipEp.Address.ToString();
                                port = ipEp.Port;
                            }
                            else
                            {
                                var dnsEp = ep as DnsEndPoint;
                                if (dnsEp != null)
                                {
                                    host = dnsEp.Host;
                                    port = dnsEp.Port;
                                }
                            }
                        }

                        if (!host.IsEmpty() && port > -1)
                        {
                            var bytes = RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                                    RedisCommandList.Cluster,
                                                    RedisCommandType.SendAndReceive,
                                                    RedisCommandList.ClusterNodes) { Priority = RedisCommandPriority.High }) as RedisBytes;

                            if (!ReferenceEquals(bytes, null))
                            {
                                var lines = bytes.Value.ToUTF8String();
                                if (!lines.IsEmpty())
                                {
                                    var nodes = RedisClusterNodeInfo.Parse(lines);
                                    if (!nodes.IsEmpty())
                                    {
                                        var myIPAndPort = host + ":" + port;

                                        var node = nodes.FirstOrDefault(n => n.IpPort == myIPAndPort);
                                        if (node != null)
                                        {
                                            var slots = node.Slots;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            { }
        }

        #endregion Clusters

        #region Roles

        protected virtual bool NeedsToDiscoverRole()
        {
            return true;
        }

        protected virtual void ValidateRole(RedisRole commandRole)
        {
            if (!(commandRole == RedisRole.Undefined || commandRole == RedisRole.Any))
            {
                var serverRole = Role;
                if (serverRole != RedisRole.Any && serverRole != commandRole &&
                    (serverRole == RedisRole.Sentinel || commandRole == RedisRole.Sentinel ||
                     (serverRole == RedisRole.Slave && commandRole == RedisRole.Master)))
                    throw new RedisException(String.Format("Connected server's {0} role does not satisfy the command's desired {1} role",
                                                           serverRole.ToString("F"), commandRole.ToString("F")), RedisErrorCode.NotSupported);
            }
        }

        protected virtual RedisRole DiscoverRole()
        {
            if (!Disposed)
            {
                var role = RedisRole.Undefined;
                try
                {
                    var array = RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                                RedisCommandList.Role,
                                                RedisCommandType.SendAndReceive) { Priority = RedisCommandPriority.High }) as RedisArray;

                    if (!ReferenceEquals(array, null) && array.IsCompleted)
                    {
                        var info = RedisRoleInfo.Parse(array);
                        if (!ReferenceEquals(info, null))
                            role = info.Role;
                    }
                }
                catch (Exception e)
                {
                    var exception = e;
                    while (exception != null)
                    {
                        if (exception is SocketException)
                            return RedisRole.Undefined;

                        var re = e as RedisException;
                        if (re != null && (re.ErrorCode == RedisErrorCode.ConnectionError ||
                            re.ErrorCode == RedisErrorCode.SocketError))
                            return RedisRole.Undefined;

                        exception = exception.InnerException;
                    }
                }

                if (role == RedisRole.Undefined)
                {
                    try
                    {
                        var serverInfo = GetServerInfo();
                        if (serverInfo != null)
                        {
                            var serverSection = serverInfo.Server;
                            if (serverSection != null)
                                role = (serverSection.RedisMode ?? String.Empty).Trim().ToRedisRole();

                            if (role == RedisRole.Undefined)
                            {
                                var replicationSection = serverInfo.Replication;
                                if (replicationSection != null)
                                    role = (replicationSection.Role ?? String.Empty).Trim().ToRedisRole();

                                if (role == RedisRole.Undefined)
                                {
                                    var sentinelSection = serverInfo.Sentinel;
                                    if (sentinelSection != null && sentinelSection.SentinelMasters.HasValue)
                                        role = RedisRole.Sentinel;

                                    if (role == RedisRole.Undefined)
                                        role = RedisRole.Master;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    { }
                }
                return role;
            }
            return RedisRole.Undefined;
        }

        #endregion Roles

        #endregion Methods
    }
}
