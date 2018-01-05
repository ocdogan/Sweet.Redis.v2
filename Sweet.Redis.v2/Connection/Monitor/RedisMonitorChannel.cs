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
using System.Net;
using System.Threading;

namespace Sweet.Redis.v2
{
    internal class RedisMonitorChannel : RedisAsyncClient, IRedisMonitorChannel
    {
        #region Constants

        private const string MonitorChannel = "*";

        #endregion Constants

        #region Field Members

        #region Readonly

        private readonly object m_SubscriptionLock = new object();
        private readonly RedisCallbackHub<RedisMonitorMessage> m_Subscriptions = new RedisCallbackHub<RedisMonitorMessage>();

        private readonly RedisAsyncQueue<RedisAsyncTask> m_SendWaitingQ = new RedisAsyncQueue<RedisAsyncTask>();

        #endregion Readonly

        private long m_MonitoringState;

        #endregion Field Members

        #region .Ctors

        internal RedisMonitorChannel(RedisConnectionSettings settings)
            : base(settings)
        { }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            lock (m_SubscriptionLock)
            {
                m_Subscriptions.Dispose();
            }
            base.OnDispose(disposing);
        }

        #endregion Destructors

        #region Properties

        public bool Monitoring
        {
            get
            {
                return m_MonitoringState != RedisConstants.Zero;
            }
            private set
            {
                Interlocked.Exchange(ref m_MonitoringState, value ? RedisConstants.One : RedisConstants.Zero);
            }
        }

        protected override bool UseBackgroundThread
        {
            get { return true; }
        }

        #endregion Properties

        #region Methods

        #region IRedisMonitorChannel

        public void Subscribe(Action<RedisMonitorMessage> callback)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            ValidateNotDisposed();

            var subscriptions = m_Subscriptions;
            if (subscriptions != null)
            {
                lock (m_SubscriptionLock)
                {
                    if (!subscriptions.Exists(MonitorChannel))
                    {
                        subscriptions.Register(MonitorChannel, callback);
                    }
                }

                Monitor();
            }
        }

        public void Monitor()
        {
            ValidateNotDisposed();
            if (!Disposed)
            {
                RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                            RedisCommandList.Monitor,
                                            RedisCommandType.SendNotReceive)
                { Priority = RedisCommandPriority.High });

                Receive();
                Interlocked.Exchange(ref m_MonitoringState, RedisConstants.One);
            }
        }

        public void UnregisterSubscription(Action<RedisMonitorMessage> callback)
        {
            if (callback != null)
            {
                lock (m_SubscriptionLock)
                {
                    m_Subscriptions.Unregister(callback);
                }
                OnUnsubscribe(callback);
            }
        }

        protected virtual void OnUnsubscribe(Action<RedisMonitorMessage> callback)
        { }

        protected override void OnQuit()
        {
            if (!Disposed)
                Interlocked.Exchange(ref m_MonitoringState, RedisConstants.Zero);
        }

        #endregion IRedisMonitorChannel

        #region Base Methods

        protected override bool NeedsToDiscoverRole()
        {
            return false;
        }

        protected override RedisAsyncSocketBase NewSocket(IPEndPoint endPoint)
        {
            var settings = Settings;
            return new RedisMonitorSocket(endPoint, MessageReceived, settings.ReceiveTimeout,
                settings.SendTimeout, settings.ReadBufferSize, settings.BulkSendFactor);
        }

        protected virtual void MessageReceived(RedisMonitorMessage message)
        {
            if (CanSendMessage(message))
            {
                var subscriptions = m_Subscriptions;
                if (subscriptions != null)
                    subscriptions.Invoke(MonitorChannel, message);
            }
        }

        protected virtual bool CanSendMessage(RedisMonitorMessage message)
        {
            return !ReferenceEquals(message, null) && !message.IsEmpty;
        }

        #endregion Base Methods

        #endregion Methods
    }
}
