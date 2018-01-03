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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Sweet.Redis.v2
{
    public class RedisAsyncSocketBase : RedisDisposable
    {
        #region Constants

        private const byte DollarSign = (byte)'$';
        private const byte AsterixSign = (byte)'*';

        private const int MinCapacity = 512;
        private const int MaxCapacity = 64 * RedisConstants.KByte;
        private const int MinAvailableCapacity = 64;

        #endregion Constants

        #region Field Members

        #region Readonly

        protected readonly object m_BufferLock = new object();
        protected readonly RedisSynchronizedQueue<RedisAsyncTask> m_ReceiveWaitingQ = new RedisSynchronizedQueue<RedisAsyncTask>();

        #endregion Readonly

        protected Stream m_NetStream;
        protected Stream m_OutStream;

        private Socket m_Socket;
        private IPEndPoint m_EndPoint;
        private RedisBufferParser m_Parser;

        protected int m_BulkSendFactor;

        private int m_Capacity;
        private byte[] m_Buffer;

        private int m_Offset;
        private int m_CurrentPos;

        private long m_ReadCount;
        private int m_ConnectionStatus = RedisAsyncClientStatus.Idle;

        private int m_SendTimeout;
        private int m_ReceiveTimeout;

        private long m_Id = RedisIDGenerator<RedisAsyncSocketBase>.NextId();

        #endregion Field Members

        #region .Ctors

        public RedisAsyncSocketBase(IPEndPoint endPoint,
            int receiveTimeout = RedisConstants.DefaultReceiveTimeout,
            int sendTimeout = RedisConstants.DefaultSendTimeout,
            int capacity = 0,
            int bulkSendFactor = 0)
        {
            m_EndPoint = endPoint;
            capacity = Math.Min(MaxCapacity, Math.Max(capacity, MinCapacity));

            m_BulkSendFactor = (bulkSendFactor < 1 ? RedisConstants.DefaultBulkSendFactor : Math.Min(bulkSendFactor, RedisConstants.MaxBulkSendFactor));

            m_SendTimeout = Math.Max(RedisConstants.MinSendTimeout, Math.Min(RedisConstants.MaxSendTimeout, sendTimeout));
            m_ReceiveTimeout = Math.Max(RedisConstants.MinReceiveTimeout, Math.Min(RedisConstants.MaxReceiveTimeout, receiveTimeout));

            m_Capacity = capacity;
            m_Buffer = new byte[capacity];

            m_Parser = new RedisBufferParser();
            m_Socket = new RedisNativeSocket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            Configure(m_Socket);
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
            using (var socket = Interlocked.Exchange(ref m_Socket, null))
            {
                try
                {
                    Interlocked.Exchange(ref m_ReadCount, RedisConstants.Zero);
                    Interlocked.Exchange(ref m_ConnectionStatus, RedisAsyncClientStatus.Idle);

                    OnCloseSocket();
                }
                catch (Exception)
                { }
                finally
                {
                    CloseStreams();
                }
            }
        }

        protected virtual void OnCloseSocket()
        {
            CancelAllReceiveQ();
        }

        #endregion Destructors

        #region Properties

        public bool Connected
        {
            get
            {
                if (m_ConnectionStatus == RedisAsyncClientStatus.Connected)
                {
                    if (m_Socket.IsConnected())
                        return true;

                    Interlocked.Exchange(ref m_ConnectionStatus, RedisAsyncClientStatus.Idle);
                }
                return false;
            }
        }

        public long Id { get { return m_Id; } }

        public bool Reading
        {
            get
            {
                return m_ReadCount > RedisConstants.Zero;
            }
        }

        public long ReadCount
        {
            get
            {
                return m_ReadCount;
            }
        }

        public int Capacity { get { return m_Capacity; } }

        public int CurrentPos
        {
            get { return m_CurrentPos; }
            set
            {
                lock (m_BufferLock)
                {
                    value = Math.Min(m_Capacity, Math.Max(0, value));
                    if (value != m_CurrentPos)
                        m_CurrentPos = Math.Max(value, m_Offset);
                }
            }
        }

        public int Offset
        {
            get { return m_Offset; }
            set
            {
                lock (m_BufferLock)
                {
                    value = Math.Min(m_Capacity, Math.Max(0, value));
                    if (value != m_Offset)
                    {
                        m_Offset = value;
                        m_CurrentPos = Math.Max(m_CurrentPos, m_Offset);
                    }
                }
            }
        }

        public byte[] Buffer
        {
            get
            {
                lock (m_BufferLock)
                {
                    return m_Buffer;
                }
            }
        }

        public bool HasData
        {
            get
            {
                lock (m_BufferLock)
                {
                    return (m_CurrentPos - m_Offset > 0);
                }
            }
        }

        public bool HasSpace
        {
            get
            {
                lock (m_BufferLock)
                {
                    return (m_Capacity > 0 && m_CurrentPos < m_Capacity);
                }
            }
        }

        public int AvailableCapacity
        {
            get
            {
                lock (m_BufferLock)
                {
                    return m_Capacity - m_CurrentPos;
                }
            }
        }

        public virtual bool ReceiveCallRequired
        {
            get
            {
                lock (m_BufferLock)
                {
                    return (m_ReadCount == RedisConstants.Zero &&
                            !m_ReceiveWaitingQ.IsEmpty);
                }
            }
        }

        #endregion Properties

        #region Methods

        #region Init

        public void ReInitialize()
        {
            lock (m_BufferLock)
            {
                m_CurrentPos = 0;
                if (m_Buffer == null)
                    m_Buffer = new byte[m_Capacity];
            }
        }

        private void Configure(Socket socket)
        {
            SetIOLoopbackFastPath(socket);

            if (m_SendTimeout > 0)
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout,
                                        m_SendTimeout == int.MaxValue ? Timeout.Infinite : m_SendTimeout);
            }

            if (m_ReceiveTimeout > 0)
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout,
                                        m_ReceiveTimeout == int.MaxValue ? Timeout.Infinite : m_ReceiveTimeout);
            }

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            socket.LingerState.Enabled = false;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

            socket.NoDelay = true;
        }

        private void SetIOLoopbackFastPath(Socket socket)
        {
            if (RedisCommon.IsWinPlatform)
            {
                try
                {
                    var ops = BitConverter.GetBytes(1);
                    socket.IOControl(RedisConstants.SIO_LOOPBACK_FAST_PATH, ops, null);
                }
                catch (Exception)
                { }
            }
        }

        #endregion Init

        #region Connect

        private void CloseStreams()
        {
            SwitchStream(ref m_OutStream, null);
            SwitchStream(ref m_NetStream, null);
        }

        private void SwitchStream(ref Stream currStream, Stream newStream)
        {
            if (newStream != currStream)
            {
                try
                {
                    using (var prevStream = Interlocked.Exchange(ref currStream, newStream))
                    {
                        if (prevStream != null)
                            prevStream.Close();
                    }
                }
                catch (Exception)
                { }
            }
        }

        private static AsyncCallback BeginConnectCallback = (asyncResult) =>
        {
            var tuple = (Tuple<RedisAsyncSocketBase, Socket, Action<RedisAsyncSocketBase>, TaskCompletionSource<bool>>)asyncResult.AsyncState;

            var tcs = tuple.Item4;
            var socket = tuple.Item1;

            bool? initialized = null;
            try
            {
                var nativeSocket = tuple.Item2;
                try
                {
                    nativeSocket.EndConnect(asyncResult);
                }
                catch (Exception)
                { }

                Interlocked.Exchange(ref socket.m_ReadCount, RedisConstants.Zero);
                if (!nativeSocket.IsConnected())
                {
                    Interlocked.Exchange(ref socket.m_ConnectionStatus, RedisAsyncClientStatus.Idle);
                    throw new RedisFatalException("Cannot connect to endpoint");
                }

                Interlocked.Exchange(ref socket.m_ConnectionStatus, RedisAsyncClientStatus.Connected);

                socket.RefreshStreams(nativeSocket);

                initialized = true;

                socket.OnConnect(nativeSocket);
            }
            catch (OperationCanceledException)
            {
                initialized = false;
                tcs.TrySetCanceled();
            }
            catch (Exception e)
            {
                initialized = false;
                tcs.TrySetException(e);
            }
            finally
            {
                if (!initialized.HasValue)
                    tcs.TrySetCanceled();
                else if (initialized.Value)
                {
                    try
                    {
                        var callback = tuple.Item3;
                        if (callback != null)
                            callback(socket);
                    }
                    finally
                    {
                        tcs.TrySetResult(true);
                    }
                }
            }
        };

        protected virtual void OnConnect(Socket socket)
        { }

        private void RefreshStreams(Socket socket)
        {
            var netStream = new NetworkStream(socket, false);
            var outStream = new BufferedStream(netStream, RedisConstants.WriteBufferSize);

            SwitchStream(ref m_OutStream, outStream);
            SwitchStream(ref m_NetStream, netStream);
        }

        public void BeginConnect(Action<RedisAsyncSocketBase> callback)
        {
            if (!Disposed &&
                Interlocked.CompareExchange(ref m_ConnectionStatus, RedisAsyncClientStatus.Connecting, RedisAsyncClientStatus.Idle) ==
                RedisAsyncClientStatus.Idle)
            {
                try
                {
                    using (var oldOutStream = Interlocked.Exchange(ref m_OutStream, null))
                    {
                        try
                        {
                            using (var oldNetStream = Interlocked.Exchange(ref m_NetStream, null))
                            {
                                try
                                {
                                    var socket = m_Socket;
                                    if (socket != null)
                                    {
                                        var tcs = new TaskCompletionSource<bool>();

                                        socket.BeginConnect(m_EndPoint, BeginConnectCallback, Tuple.Create(this, socket, callback, tcs));
                                        tcs.Task.Wait();
                                    }
                                }
                                finally
                                {
                                    if (oldNetStream != null)
                                        oldNetStream.Close();
                                }
                            }
                        }
                        finally
                        {
                            if (oldOutStream != null)
                                oldOutStream.Close();
                        }
                    }
                }
                catch (Exception)
                {
                    Interlocked.Exchange(ref m_ConnectionStatus, RedisAsyncClientStatus.Idle);
                    throw;
                }
            }
        }

        private static AsyncCallback BeginDisconnectCallback = (asyncResult) =>
        {
            var tuple = (Tuple<RedisAsyncSocketBase, Socket, Action<RedisAsyncSocketBase>, TaskCompletionSource<bool>>)asyncResult.AsyncState;

            var tcs = tuple.Item4;
            var socket = tuple.Item1;
            try
            {
                Interlocked.Exchange(ref socket.m_ReadCount, RedisConstants.Zero);

                var nativeSocket = tuple.Item2;
                try
                {
                    nativeSocket.EndDisconnect(asyncResult);
                }
                catch (Exception)
                { }

                Interlocked.Exchange(ref socket.m_ConnectionStatus, RedisAsyncClientStatus.Idle);
                socket.CloseStreams();

                tcs.TrySetResult(true);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
            finally
            {
                var callback = tuple.Item3;
                if (callback != null)
                    callback(socket);
            }
        };

        public void BeginDisconnect(Action<RedisAsyncSocketBase> callback)
        {
            if (!Disposed)
            {
                var socket = m_Socket;
                if (socket != null)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    socket.BeginDisconnect(true, BeginDisconnectCallback, Tuple.Create(this, socket, callback, tcs));
                    tcs.Task.Wait();
                }
            }
        }

        public void Close()
        {
            try
            {
                var socket = m_Socket;
                if (socket.IsConnected())
                {
                    socket.Shutdown(SocketShutdown.Both);

                    var tcs = new TaskCompletionSource<bool>();
                    socket.BeginDisconnect(true, BeginDisconnectCallback,
                        Tuple.Create(this, socket, (Action<RedisAsyncSocketBase>)null, tcs));
                    tcs.Task.Wait();
                }
            }
            catch (Exception)
            { }
        }

        #endregion Connect

        #region Send

        protected virtual bool Write(RedisCommand command, bool flush = true)
        {
            if (command != null)
            {
                var stream = m_OutStream;
                if (stream != null)
                {
                    var cmd = command.Command;
                    if (!cmd.IsEmpty)
                    {
                        var arguments = command.Arguments;
                        var argsCount = arguments != null ? arguments.Length : 0;

                        // *(Argument.Count + 1)
                        stream.WriteByte(AsterixSign);
                        var number = (argsCount + 1).ToBytes();
                        stream.Write(number, 0, number.Length);
                        stream.Write(RedisConstants.LineEnd, 0, 2);

                        // Command
                        var data = cmd.Data;
                        // $Command.Length
                        stream.WriteByte(DollarSign);
                        number = data.Length.ToBytes();
                        stream.Write(number, 0, number.Length);
                        stream.Write(RedisConstants.LineEnd, 0, 2);
                        // Command
                        stream.Write(data, 0, data.Length);
                        stream.Write(RedisConstants.LineEnd, 0, 2);

                        if (argsCount > 0)
                        {
                            byte[] arg;
                            for (var i = 0; i < argsCount; i++)
                            {
                                arg = arguments[i];
                                if (arg == null)
                                {
                                    // $-1
                                    stream.Write(RedisConstants.NullBulkString, 0, 3);
                                }
                                else
                                {
                                    int argLength = arg.Length;
                                    if (argLength > 0)
                                    {
                                        // $Argument.Length
                                        stream.WriteByte(DollarSign);
                                        number = argLength.ToBytes();
                                        stream.Write(number, 0, number.Length);
                                        stream.Write(RedisConstants.LineEnd, 0, 2);
                                        // Argument
                                        stream.Write(arg, 0, argLength);
                                    }
                                    else
                                    {
                                        // $0
                                        stream.Write(RedisConstants.EmptyBulkString, 0, 2);
                                        stream.Write(RedisConstants.LineEnd, 0, 2);
                                    }
                                }
                                stream.Write(RedisConstants.LineEnd, 0, 2);
                            }
                        }

                        if (flush)
                            stream.Flush();

                        return true;
                    }
                }
            }
            return false;
        }

        public virtual void Write(RedisAsyncTask asyncTask, bool flush = true)
        {
            m_ReceiveWaitingQ.Enqueue(asyncTask);
            try
            {
                if (Write(asyncTask.Command, flush) && flush)
                    BeginReceive();
            }
            catch (Exception e)
            {
                try
                {
                    if (e is SocketException)
                    {
                        CancelAllReceiveQ();
                        throw;
                    }
                }
                finally
                {
                    asyncTask.TrySetException(e);
                }
            }
        }

        public virtual void Write(IList<RedisAsyncTask> asyncTasks)
        {
            if (asyncTasks != null)
            {
                var count = asyncTasks.Count;
                if (count > 0)
                {
                    var receive = false;
                    var stream = m_OutStream;
                    var bulkSendFactor = m_BulkSendFactor;

                    try
                    {
                        var index = 0;
                        while (index < count)
                        {
                            var asyncTask = asyncTasks[index++];

                            var command = asyncTask.Command;
                            if (command.CommandType != RedisCommandType.SendNotReceive)
                            {
                                receive = true;
                                m_ReceiveWaitingQ.Enqueue(asyncTask);
                            }

                            try
                            {
                                Write(command, false);
                                if (receive && (index % bulkSendFactor == 0))
                                {
                                    receive = false;

                                    stream.Flush();
                                    BeginReceive();
                                }
                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    if (e is SocketException)
                                    {
                                        CancelAllReceiveQ();
                                        break;
                                    }
                                }
                                finally
                                {
                                    asyncTask.TrySetException(e);
                                }
                            }
                        }

                        // Cancel rest of list if any error occured
                        while (index < count)
                        {
                            try
                            {
                                asyncTasks[index++].TrySetCanceled();
                            }
                            catch (Exception)
                            { }
                        }
                    }
                    finally
                    {
                        if (receive && (count % bulkSendFactor != 0))
                        {
                            stream.Flush();
                            BeginReceive();
                        }
                    }
                }
            }
        }

        protected void CancelAllReceiveQ()
        {
            RedisAsyncTask asyncTask;
            while (m_ReceiveWaitingQ.TryDequeue(out asyncTask))
            {
                try
                {
                    asyncTask.TrySetCanceled();
                }
                catch (Exception)
                { }
            }
        }

        #endregion Send

        #region Receive

        private static WaitCallback BeginReceiveCallback = (asyncResult) =>
        {
            var socket = (RedisAsyncSocketBase)asyncResult;
            try
            {
                socket.BeginRead(false);
            }
            catch (Exception) { }
            finally
            {
                Interlocked.Add(ref socket.m_ReadCount, RedisConstants.MinusOne);
            }
        };

        public void BeginReceive()
        {
            if (!Disposed)
            {
                var readCount = Interlocked.Add(ref m_ReadCount, RedisConstants.One);
                if (readCount > RedisConstants.One)
                {
                    Interlocked.Add(ref m_ReadCount, RedisConstants.MinusOne);
                    return;
                }

                ThreadPool.QueueUserWorkItem(BeginReceiveCallback, this);
            }
        }

        private static AsyncCallback BeginReadCallback = (asyncResult) =>
        {
            var tuple = (Tuple<RedisAsyncSocketBase, Stream>)asyncResult.AsyncState;
            try
            {
                var readLen = (tuple.Item2 != null) ? tuple.Item2.EndRead(asyncResult) : 0;
                tuple.Item1.EndRead(asyncResult, readLen);
            }
            catch (Exception e)
            {
                try
                {
                    if (e is IOException || e is SocketException)
                        tuple.Item1.Close();
                }
                catch (Exception)
                { }
            }
            finally
            {
                Interlocked.Add(ref tuple.Item1.m_ReadCount, RedisConstants.MinusOne);
            }
        };

        protected virtual bool ContinueReading()
        {
            return !m_ReceiveWaitingQ.IsEmpty;
        }

        private void EndRead(IAsyncResult asyncResult, int readLen)
        {
            if ((asyncResult == null) || !asyncResult.IsCompleted)
            {
                Close();
                return;
            }

            if (readLen > 0 && !Disposed)
            {
                Interlocked.Add(ref m_CurrentPos, readLen);
                while (ReadResponse())
                { }

                if (!Disposed && ContinueReading())
                    BeginRead(true);

                return;
            }

            Close();
        }

        public void EnsureCapacity(int expected)
        {
            expected = Math.Min(MaxCapacity, Math.Max(expected, MinCapacity) + MinAvailableCapacity);
            lock (m_BufferLock)
            {
                if (m_Buffer == null)
                {
                    var newCapacity = Math.Max(m_Capacity, expected);

                    m_Offset = 0;
                    m_CurrentPos = 0;
                    Interlocked.Exchange(ref m_Capacity, newCapacity);

                    m_Buffer = new byte[newCapacity];
                    return;
                }

                if ((m_Capacity - m_CurrentPos) /* AvailableCapacity */ < expected)
                {
                    var newCapacity = m_CurrentPos + expected;
                    var newBuffer = new byte[newCapacity];

                    Array.Copy(m_Buffer, 0, newBuffer, 0, m_CurrentPos);

                    m_Buffer = newBuffer;
                    Interlocked.Exchange(ref m_Capacity, newCapacity);
                }
            }
        }

        public void EnsureCapacity()
        {
            lock (m_BufferLock)
            {
                if (m_Buffer == null)
                {
                    m_Offset = 0;
                    m_CurrentPos = 0;

                    m_Buffer = new byte[m_Capacity];
                    return;
                }

                if ((m_Capacity - m_CurrentPos) /* AvailableCapacity */ < MinAvailableCapacity)
                {
                    var newCapacity = m_Capacity + MinCapacity;

                    var newBuffer = new byte[newCapacity];
                    Array.Copy(m_Buffer, 0, newBuffer, 0, m_CurrentPos);

                    m_Buffer = newBuffer;
                    Interlocked.Exchange(ref m_Capacity, newCapacity);
                }
            }
        }

        private void BeginRead(bool afterAsyncEnd)
        {
            if (Disposed)
                return;

            Interlocked.Add(ref m_ReadCount, RedisConstants.One);
            try
            {
                var stream = m_NetStream;
                if (stream == null || !Connected)
                {
                    BeginConnect(null);
                    stream = m_NetStream;
                }

                var socket = m_Socket;
                do
                {
                    // Get available
                    var expected = socket.Available;
                    if (expected == 0) // Not available
                    {
                        EnsureCapacity();
                        try
                        {
                            Interlocked.Add(ref m_ReadCount, RedisConstants.One);
                            stream.BeginRead(m_Buffer, m_CurrentPos, (m_Capacity - m_CurrentPos) /* AvailableCapacity */, BeginReadCallback, Tuple.Create(this, stream));
                        }
                        catch (Exception)
                        {
                            Interlocked.Add(ref m_ReadCount, RedisConstants.MinusOne);
                            throw;
                        }
                        return;
                    }

                    // Has more in socket's system buffer
                    EnsureCapacity(expected);

                    var readLen = stream.Read(m_Buffer, m_CurrentPos, (m_Capacity - m_CurrentPos) /* AvailableCapacity */);

                    if (readLen <= 0 || Disposed)
                    {
                        Close();
                        return;
                    }

                    Interlocked.Add(ref m_CurrentPos, readLen);
                    while (ReadResponse())
                    { }
                }
                while (!Disposed && ContinueReading());
            }
            finally
            {
                Interlocked.Add(ref m_ReadCount, RedisConstants.MinusOne);
            }
        }

        private bool ReadResponse()
        {
            if (Disposed)
                return false;

            var buffer = m_Buffer;
            if (buffer == null)
                return false;

            var offset = m_Offset;

            var dataLength = m_CurrentPos - offset;
            if (dataLength <= 0)
                return false;

            var context = new RedisBufferContext
            {
                Buffer = buffer,
                Offset = offset,
                Length = dataLength
            };

            m_Parser.TryParse(context);
            if (Disposed)
                return false;

            if (context.Completed)
            {
                bool @continue;
                RedisAsyncTask asyncTask;

                DoBeforeCompleteContext(context, out @continue, out asyncTask);
                if (!@continue || Disposed)
                    return false;

                try
                {
                    var remaining = Math.Max(0, m_CurrentPos - context.Offset);
                    if (remaining == 0)
                    {
                        m_Offset = 0;
                        m_CurrentPos = 0;

                        return false;
                    }

                    // Has more
                    m_Offset = context.Offset;
                    if ((m_Capacity - m_CurrentPos) /* AvailableCapacity */ < MinAvailableCapacity)
                    {
                        Array.Copy(buffer, context.Offset, buffer, 0, remaining);

                        m_Offset = 0;
                        m_CurrentPos = remaining;
                    }

                    return true;
                }
                finally
                {
                    if (!Disposed)
                        DoAfterCompleteContext(context, asyncTask);
                }
            }

            if (m_Offset > 0 && dataLength > 0)
            {
                Array.Copy(buffer, m_Offset, buffer, 0, dataLength);

                m_Offset = 0;
                m_CurrentPos = dataLength;
            }
            return false;
        }

        protected virtual void DoBeforeCompleteContext(RedisBufferContext context, out bool @continue, out RedisAsyncTask asyncTask)
        {
            @continue = m_ReceiveWaitingQ.TryDequeue(out asyncTask);
        }

        protected virtual void DoAfterCompleteContext(RedisBufferContext context, RedisAsyncTask asyncTask)
        {
            if (asyncTask != null)
                asyncTask.TrySetCompleted(context.Result);
        }

        #endregion Receive

        #endregion Methods
    }
}
