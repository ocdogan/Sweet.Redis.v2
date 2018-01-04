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
using System.Net;
using System.Threading;

namespace Sweet.Redis.v2
{
    internal class RedisPubSubChannel : RedisAsyncClient, IRedisPubSubChannel
    {
        #region RedisPubSubSubscriptions

        private class RedisPubSubSubscriptions : RedisCallbackHub<RedisPubSubMessage>
        {
            #region Methods

            public override void Invoke(string keyword, RedisPubSubMessage msg)
            {
                if (!keyword.IsEmpty() &&
                    !msg.IsEmpty &&
                    (msg.Type == RedisPubSubMessageType.Message ||
                    msg.Type == RedisPubSubMessageType.PMessage))
                {
                    var callbacks = CallbacksOf(keyword);
                    if (callbacks != null && callbacks.Count > 0)
                    {
                        foreach (var callback in callbacks)
                        {
                            try
                            {
                                if (Disposed)
                                    return;

                                callback.InvokeAsync(msg);
                            }
                            catch (Exception)
                            { }
                        }
                    }
                }
            }

            #endregion Methods
        }

        #endregion RedisPubSubSubscriptions

        #region Field Members

        #region Readonly

        private long m_ReceiveState;

        private DateTime m_LastMessageSeenTime;

        private readonly object m_SubscriptionLock = new object();
        private readonly object m_PSubscriptionLock = new object();

        private readonly RedisPubSubSubscriptions m_Subscriptions = new RedisPubSubSubscriptions();
        private readonly RedisPubSubSubscriptions m_PSubscriptions = new RedisPubSubSubscriptions();

        private readonly RedisAsyncQueue<RedisAsyncTask> m_SendWaitingQ = new RedisAsyncQueue<RedisAsyncTask>();

        #endregion Readonly

        private long m_PubSubState;

        #endregion Field Members

        #region .Ctors

        internal RedisPubSubChannel(RedisConnectionSettings settings)
            : base(settings)
        { }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            Interlocked.Exchange(ref m_ReceiveState, RedisConstants.Zero);

            lock (m_SubscriptionLock)
            {
                m_Subscriptions.Dispose();
            }
            lock (m_PSubscriptionLock)
            {
                m_PSubscriptions.Dispose();
            }

            base.OnDispose(disposing);
        }

        #endregion Destructors

        #region Properties

        public virtual RedisEndPoint EndPoint
        {
            get
            {
                var settings = Settings;
                if (settings != null)
                {
                    var endPoints = settings.EndPoints;
                    if (!endPoints.IsEmpty())
                    {
                        foreach (var ep in endPoints)
                            if (ep != null)
                                return (RedisEndPoint)ep.Clone();
                    }
                }
                return RedisEndPoint.Empty;
            }
        }

        public bool PubSubing
        {
            get
            {
                return m_PubSubState != RedisConstants.Zero;
            }
            private set
            {
                Interlocked.Exchange(ref m_PubSubState, value ? RedisConstants.One : RedisConstants.Zero);
            }
        }

        protected override bool UseBackgroundThread
        {
            get { return true; }
        }

        public DateTime LastMessageSeenTime
        {
            get { return m_LastMessageSeenTime; }
        }

        #endregion Properties

        #region Methods

        #region IRedisPubSubChannel

        #region Subscribe

        public void Subscribe(Action<RedisPubSubMessage> callback, RedisParam channel, params RedisParam[] channels)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            if (channel.IsEmpty)
                throw new ArgumentNullException("channel");

            ValidateNotDisposed();

            var subscriptions = m_Subscriptions;
            if (subscriptions != null)
            {
                var newItems = new List<RedisParam>(channels.Length + 1);

                lock (m_SubscriptionLock)
                {
                    if (!subscriptions.Exists(channel))
                    {
                        newItems.Add(channel.Data);
                        subscriptions.Register(channel, callback);
                    }
                }

                foreach (var chnl in channels)
                {
                    if (!chnl.IsEmpty)
                    {
                        lock (m_SubscriptionLock)
                        {
                            if (!subscriptions.Exists(chnl, callback))
                            {
                                newItems.Add(chnl.Data);
                                subscriptions.Register(chnl, callback);
                            }
                        }
                    }
                }

                if (newItems.Count > 0)
                {
                    RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                        RedisCommandList.Subscribe,
                                        RedisCommandType.SendNotReceive,
                                        newItems.ToArray()) { Priority = RedisCommandPriority.High });
                }
            }
        }

        public void Unsubscribe(params string[] channels)
        {
            ValidateNotDisposed();

            if (channels.Length == 0)
                RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                             RedisCommandList.Unsubscribe,
                                             RedisCommandType.SendNotReceive)
                { Priority = RedisCommandPriority.High });
            else
                RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                            RedisCommandList.Unsubscribe,
                                            RedisCommandType.SendNotReceive,
                                            channels.ToParams())
                { Priority = RedisCommandPriority.High });
        }

        public void UnregisterSubscription(Action<RedisPubSubMessage> callback)
        {
            if (callback != null)
            {
                ValidateNotDisposed();

                lock (m_SubscriptionLock)
                {
                    m_Subscriptions.Unregister(callback);
                }
            }
        }

        public void UnregisterSubscription(RedisParam channel, Action<RedisPubSubMessage> callback)
        {
            if (callback != null)
            {
                ValidateNotDisposed();

                lock (m_SubscriptionLock)
                {
                    m_Subscriptions.Unregister(channel, callback);
                }
            }
        }

        #endregion Subscribe

        #region PSubscribe

        public void PSubscribe(Action<RedisPubSubMessage> callback, RedisParam pattern, params RedisParam[] patterns)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            if (pattern.IsEmpty)
                throw new ArgumentNullException("pattern");

            ValidateNotDisposed();

            var pSubscriptions = m_PSubscriptions;
            if (pSubscriptions != null)
            {
                var newItems = new List<RedisParam>(patterns.Length + 1);

                lock (m_PSubscriptionLock)
                {
                    if (!pSubscriptions.Exists(pattern))
                    {
                        newItems.Add(pattern.Data);
                        pSubscriptions.Register(pattern, callback);
                    }
                }

                foreach (var ptrn in patterns)
                {
                    if (!ptrn.IsEmpty)
                    {
                        lock (m_PSubscriptionLock)
                        {
                            if (!pSubscriptions.Exists(ptrn, callback))
                            {
                                newItems.Add(ptrn.Data);
                                pSubscriptions.Register(ptrn, callback);
                            }
                        }
                    }
                }

                if (newItems.Count > 0)
                {
                    RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                        RedisCommandList.PSubscribe,
                                        RedisCommandType.SendNotReceive,
                                        newItems.ToArray()) { Priority = RedisCommandPriority.High });
                }
            }
        }

        public void PUnsubscribe(params RedisParam[] channels)
        {
            ValidateNotDisposed();

            if (channels.Length == 0)
                RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                             RedisCommandList.PUnsubscribe,
                                             RedisCommandType.SendNotReceive)
                { Priority = RedisCommandPriority.High });
            else
                RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                            RedisCommandList.PUnsubscribe,
                                            RedisCommandType.SendNotReceive,
                                            channels)
                { Priority = RedisCommandPriority.High });
        }

        public void UnregisterPSubscription(Action<RedisPubSubMessage> callback)
        {
            if (callback != null)
            {
                ValidateNotDisposed();

                lock (m_PSubscriptionLock)
                {
                    m_PSubscriptions.Unregister(callback);
                }
            }
        }

        public void UnregisterPSubscription(RedisParam channel, Action<RedisPubSubMessage> callback)
        {
            if (callback != null)
            {
                ValidateNotDisposed();

                lock (m_PSubscriptionLock)
                {
                    m_PSubscriptions.Unregister(channel, callback);
                }
            }
        }

        #endregion PSubscribe

        protected override void OnQuit()
        {
            if (!Disposed)
                Interlocked.Exchange(ref m_PubSubState, RedisConstants.Zero);
        }

        public void ResubscribeAll()
        {
            var receive = RefreshSubscriptions();
            receive = RefreshPSubscriptions() || receive;

            if (receive)
            {
                Receive();
                Interlocked.Exchange(ref m_PubSubState, RedisConstants.One);
            }
        }

        private bool RefreshSubscriptions()
        {
            ValidateNotDisposed();

            var channels = (List<RedisParam>)null;

            lock (m_SubscriptionLock)
            {
                var subscriptions = m_Subscriptions.Subscriptions();
                if (subscriptions != null && subscriptions.Count > 0)
                {
                    channels = new List<RedisParam>(subscriptions.Count);

                    foreach (var kv in subscriptions)
                    {
                        if (!kv.Value.IsEmpty())
                            channels.Add(kv.Key);
                    }
                }
            }

            if (!channels.IsEmpty())
            {
                RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                    RedisCommandList.Subscribe,
                                    RedisCommandType.SendNotReceive,
                                    channels.ToArray())
                { Priority = RedisCommandPriority.High });
                return true;
            }
            return false;
        }

        private bool RefreshPSubscriptions()
        {
            ValidateNotDisposed();

            var patterns = (List<RedisParam>)null;

            lock (m_PSubscriptionLock)
            {
                var pSubscriptions = m_PSubscriptions.Subscriptions();
                if (pSubscriptions != null && pSubscriptions.Count > 0)
                {
                    patterns = new List<RedisParam>(pSubscriptions.Count);

                    foreach (var kv in pSubscriptions)
                    {
                        if (!kv.Value.IsEmpty())
                            patterns.Add(kv.Key);
                    }
                }
            }

            if (!patterns.IsEmpty())
            {
                RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                    RedisCommandList.PSubscribe,
                                    RedisCommandType.SendNotReceive,
                                    patterns.ToArray())
                { Priority = RedisCommandPriority.High });
                return true;
            }
            return false;
        }

        public void UnsubscribeAll()
        {
            lock (m_SubscriptionLock)
            {
                m_Subscriptions.UnregisterAll();
            }

            lock (m_PSubscriptionLock)
            {
                m_PSubscriptions.UnregisterAll();
            }
            
            RunSyncTask(
                new[] { new RedisCommand(RedisConstants.UninitializedDbIndex,
                    RedisCommandList.PUnsubscribe,
                    RedisCommandType.SendNotReceive) { Priority = RedisCommandPriority.High },
                new RedisCommand(RedisConstants.UninitializedDbIndex,
                    RedisCommandList.Unsubscribe,
                    RedisCommandType.SendNotReceive) { Priority = RedisCommandPriority.High } });
        }

        #endregion IRedisPubSubChannel

        #region Base Methods

        private bool HasSubscription()
        {
            var result = false;
            lock (m_SubscriptionLock)
            {
                var subscriptions = m_Subscriptions;
                if (subscriptions != null)
                    result = subscriptions.HasSubscription;
            }

            if (!result)
            {
                lock (m_PSubscriptionLock)
                {
                    var subscriptions = m_PSubscriptions;
                    if (subscriptions != null)
                        result = subscriptions.HasSubscription;
                }
            }
            return result;
        }

        public virtual bool Ping()
        {
            if (!Disposed)
            {
                try
                {
                    RunSyncTask(new RedisCommand(RedisConstants.UninitializedDbIndex,
                                            RedisCommandList.Ping,
                                            RedisCommandType.SendAndReceive)
                    { Priority = RedisCommandPriority.High });
                    return true;
                }
                catch (Exception)
                { }
            }
            return false;
        }

        protected override bool NeedsToDiscoverRole()
        {
            return false;
        }

        protected override RedisAsyncSocketBase NewSocket(IPEndPoint endPoint)
        {
            var settings = Settings;
            return new RedisPubSubSocket(endPoint, MessageReceived, settings.ReceiveTimeout,
                settings.SendTimeout, settings.ReadBufferSize, settings.BulkSendFactor);
        }

        protected virtual void MessageReceived(RedisPubSubMessage message)
        {
            if (!Disposed && 
                !ReferenceEquals(message, null) && !message.IsEmpty &&
                CanSendMessage(message))
            {
                m_LastMessageSeenTime = DateTime.UtcNow;

                if (!message.IsEmpty)
                {
                    switch (message.Type)
                    {
                        case RedisPubSubMessageType.Message:
                            {
                                var subscriptions = m_Subscriptions;
                                if (subscriptions != null &&
                                    subscriptions.HasCallbacks(message.Channel))
                                    subscriptions.Invoke(message.Channel, message);
                            }
                            break;
                        case RedisPubSubMessageType.PMessage:
                            {
                                var subscriptions = m_PSubscriptions;
                                if (subscriptions != null &&
                                    subscriptions.HasCallbacks(message.Pattern))
                                    subscriptions.Invoke(message.Pattern, message);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        protected virtual bool CanSendMessage(RedisPubSubMessage message)
        {
            return true;
        }

        #endregion Base Methods

        #endregion Methods
    }
}
