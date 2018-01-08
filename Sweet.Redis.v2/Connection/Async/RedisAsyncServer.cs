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
using System.Threading;

namespace Sweet.Redis.v2
{
    public class RedisAsyncServer : RedisDisposable, IRedisServer, IRedisPingable
    {
        #region Field Members

        private RedisConnectionSettings m_Settings;

        private int m_ClientIndex;
        private int m_ClientCount;
        private RedisAsyncClient[] m_Clients;
        private readonly object m_ClientLock = new object();

        private RedisAsyncClient m_TransactionalClient;
        private readonly object m_TransClientLock = new object();

        private RedisMonitorChannel m_MonitorChannel;
        private readonly object m_MonitorChannelLock = new object();

        private RedisPubSubChannel m_PubSubChannel;
        private readonly object m_PubSubChannelLock = new object();

        private RedisHeartBeatProbe m_HeartBeatProbe;

        #endregion Field Members

        #region .Ctors

        protected internal RedisAsyncServer(RedisAsyncClient client, RedisConnectionSettings settings)
        {
            if (client == null)
                throw new RedisFatalException(new ArgumentNullException("client"), RedisErrorCode.MissingParameter);

            settings = settings ?? client.Settings;

            Init(settings);
            if (client.IsAlive())
                m_Clients[0] = client;
        }

        public RedisAsyncServer(RedisConnectionSettings settings)
        {
            if (settings == null)
                throw new RedisFatalException(new ArgumentNullException("settings"), RedisErrorCode.MissingParameter);

            Init(settings);
        }

        private void Init(RedisConnectionSettings settings)
        {
            m_Settings = settings;
            m_ClientCount = settings.ConnectionCount;
            m_Clients = new RedisAsyncClient[m_ClientCount];

            if (settings.HeartBeatEnabled)
            {
                m_HeartBeatProbe = new RedisHeartBeatProbe(settings, this, null);
                m_HeartBeatProbe.SetOnPulseStateChange(OnPulseStateChange);

                m_HeartBeatProbe.AttachToCardio();
            }
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            using (Interlocked.Exchange(ref m_HeartBeatProbe, null)) { }

            var monitorChannel = Interlocked.Exchange(ref m_MonitorChannel, null);
            if (monitorChannel != null)
                monitorChannel.Dispose();

            var pubSubChannel = Interlocked.Exchange(ref m_PubSubChannel, null);
            if (pubSubChannel != null)
                pubSubChannel.Dispose();

            base.OnDispose(disposing);

            var clients = Interlocked.Exchange(ref m_Clients, null);
            if (clients != null)
            {
                foreach (var client in clients)
                {
                    try
                    {
                        client.Dispose();
                    }
                    catch (Exception)
                    { }
                }
            }
        }

        #endregion Destructors

        #region Properties

        public virtual bool IsDown
        {
            get { return !Disposed; }
            protected internal set { }
        }

        public IRedisMonitorChannel MonitorChannel
        {
            get
            {
                ValidateNotDisposed();

                var channel = m_MonitorChannel;
                if (channel == null)
                {
                    lock (m_MonitorChannelLock)
                    {
                        channel = m_MonitorChannel;
                        if (channel == null)
                            channel = (m_MonitorChannel = new RedisMonitorChannel(m_Settings));
                    }
                }
                return channel;
            }
        }

        public IRedisPubSubChannel PubSubChannel
        {
            get
            {
                ValidateNotDisposed();

                var channel = m_PubSubChannel;
                if (channel == null)
                {
                    lock (m_PubSubChannelLock)
                    {
                        channel = m_PubSubChannel;
                        if (channel == null)
                            channel = (m_PubSubChannel = new RedisPubSubChannel(m_Settings));
                    }
                }
                return channel;
            }
        }

        public RedisConnectionSettings Settings
        {
            get { return m_Settings; }
        }

        #endregion Properties

        #region Methods

        protected virtual void ApplyRole(RedisRole role)
        { }

        protected virtual void OnPulseStateChange(object sender, RedisCardioPulseStatus status)
        { }

        bool IRedisPingable.Ping()
        {
            if (!Disposed)
            {
                var clients = m_Clients;
                if (clients != null)
                {
                    var result = false;
                    foreach (var client in clients)
                    {
                        if (client != null)
                        {
                            try
                            {
                                if (client.Ping())
                                    result = true;
                            }
                            catch (Exception)
                            { }
                        }
                    }

                    return result;
                }
            }
            return false;
        }

        private RedisAsyncClient GetClient()
        {
            lock (m_ClientLock)
            {
                var index = Interlocked.Add(ref m_ClientIndex, 1);
                if (index >= m_ClientCount)
                {
                    index = 0;
                    Interlocked.Exchange(ref m_ClientIndex, 0);
                }

                var client = m_Clients[index];
                if (client == null)
                    client = (m_Clients[index] = new RedisAsyncClient(m_Settings));
                return client;
            }
        }

        public IRedisAdmin GetAdmin()
        {
            ValidateNotDisposed();
            return new RedisAdmin(GetClient(), RedisConstants.UninitializedDbIndex, m_Settings.ThrowOnError);
        }

        public IRedisDb GetDb(int dbIndex = 0)
        {
            ValidateNotDisposed();
            return new RedisDb(GetClient(), dbIndex, m_Settings.ThrowOnError);
        }

        #region Batch

        private RedisAsyncClient GetTransactionalClient()
        {
            var client = m_TransactionalClient;
            if (client == null)
            {
                lock (m_TransClientLock)
                {
                    client = m_TransactionalClient;
                    if (client == null)
                        client = (m_TransactionalClient = new RedisAsyncClient(m_Settings));
                }
            }
            return client;
        }

        public IRedisTransaction BeginTransaction(int dbIndex = 0)
        {
            ValidateNotDisposed();
            return new RedisTransaction(GetTransactionalClient(), dbIndex, m_Settings.ThrowOnError);
        }

        public IRedisPipeline CreatePipeline(int dbIndex = 0)
        {
            ValidateNotDisposed();
            return new RedisPipeline(GetClient(), dbIndex, m_Settings.ThrowOnError);
        }

        #endregion Batch

        #endregion Methods
    }
}
