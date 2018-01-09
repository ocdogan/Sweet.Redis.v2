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
    internal class RedisManagedNode : RedisDisposable, IRedisHeartBeatProbe
    {
        #region Field Members

        private int m_PulseState;
        private int m_ProbeAttached;
        private int m_PulseFailCount;

        protected object m_Seed;
        protected RedisRole m_Role;
        protected RedisEndPoint m_EndPoint;
        protected RedisManagedNodeStatus m_Status;

        private bool m_OwnsSeed;
        private RedisConnectionSettings m_Settings;

        private Action<object, RedisCardioPulseStatus> m_OnPulseStateChange;

        private long m_Id = RedisIDGenerator<RedisManagedNode>.NextId();

        #endregion Field Members

        #region .Ctors

        protected RedisManagedNode(RedisManagerSettings settings, RedisRole role, object seed,
                                Action<object, RedisCardioPulseStatus> onPulseStateChange, bool ownsSeed = true)
        {
            m_Role = role;
            m_OwnsSeed = ownsSeed;
            m_EndPoint = RedisEndPoint.Empty;

            m_Settings = settings;
            m_OnPulseStateChange = onPulseStateChange;

            ExchangeSeedInternal(seed);
            AttachToCardio();
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnBeforeDispose(bool disposing, bool alreadyDisposed)
        {
            DetachFromCardio();
            base.OnBeforeDispose(disposing, alreadyDisposed);
        }

        protected override void OnDispose(bool disposing)
        {
            m_Status |= RedisManagedNodeStatus.Disposed;
            Interlocked.Exchange(ref m_OnPulseStateChange, null);

            base.OnDispose(disposing);

            var seed = ExchangeSeedInternal(null);

            if (m_OwnsSeed && !ReferenceEquals(seed, null))
            {
                var disposable = seed as IDisposable;
                if (!ReferenceEquals(disposable, null))
                    disposable.Dispose();
                else
                {
                    var rDisposable = seed as IRedisDisposable;
                    if (!ReferenceEquals(rDisposable, null))
                        rDisposable.Dispose();
                    else
                    {
                        var iDisposable = seed as RedisInternalDisposable;
                        if (!ReferenceEquals(iDisposable, null))
                            iDisposable.Dispose();
                    }
                }
            }
        }

        #endregion Destructors

        #region Properties

        public override bool Disposed
        {
            get
            {
                return base.Disposed ||
                       m_Status.HasFlag(RedisManagedNodeStatus.Disposed);
            }
        }

        public RedisEndPoint EndPoint { get { return m_EndPoint; } }

        public long Id { get { return m_Id; } }

        public virtual bool IsClosed
        {
            get { return m_Status != RedisManagedNodeStatus.Open || base.Disposed; }
            set
            {
                if (!Disposed)
                {
                    if (value)
                        m_Status |= RedisManagedNodeStatus.Closed;
                    else
                        m_Status &= ~(RedisManagedNodeStatus.Closed | RedisManagedNodeStatus.HalfClosed);
                }
            }
        }

        public virtual bool IsHalfClosed
        {
            get { return ((RedisManagedNodeStatus)m_Status).HasFlag(RedisManagedNodeStatus.HalfClosed); }
            set
            {
                if (value)
                    m_Status |= RedisManagedNodeStatus.HalfClosed;
                else
                    m_Status &= ~RedisManagedNodeStatus.HalfClosed;
            }
        }

        public bool IsOpen
        {
            get { return !IsClosed; }
            set { IsClosed = !value; }
        }

        public virtual bool IsSeedAlive
        {
            get
            {
                var seed = m_Seed;
                if (!ReferenceEquals(seed, null))
                {
                    var rDisposable = seed as IRedisDisposable;
                    if (!ReferenceEquals(rDisposable, null))
                        return !rDisposable.Disposed;

                    var iDisposable = seed as RedisInternalDisposable;
                    if (!ReferenceEquals(iDisposable, null))
                        return !iDisposable.Disposed;

                    return true;
                }
                return false;
            }
        }

        public virtual bool IsSeedDown
        {
            get
            {
                var seed = m_Seed;
                if (!ReferenceEquals(seed, null))
                {
                    var server = seed as RedisManagedServer;
                    if (!ReferenceEquals(server, null))
                        return server.IsDown;

                    var channel = seed as RedisPubSubChannel;
                    if (!ReferenceEquals(channel, null))
                        return channel.Disposed;

                    return true;
                }
                return true;
            }
        }

        public bool OwnsSeed { get { return m_OwnsSeed; } }

        public object Seed { get { return m_Seed; } }

        public virtual RedisRole Role
        {
            get { return m_Role; }
            set { m_Role = value; }
        }

        public RedisConnectionSettings Settings { get { return m_Settings; } }

        int IRedisHeartBeatProbe.PulseFailCount
        {
            get { return m_PulseFailCount; }
        }

        bool IRedisHeartBeatProbe.Pulsing
        {
            get { return m_PulseState != 0; }
        }

        #endregion Properties

        #region Methods

        public RedisNodeInfo GetNodeInfo()
        {
            return new RedisNodeInfo(m_EndPoint, Role);
        }

        public object ExchangeSeed(object seed)
        {
            ValidateNotDisposed();
            return ExchangeSeedInternal(seed);
        }

        protected virtual object ExchangeSeedInternal(object seed)
        {
            if (!ReferenceEquals(seed, null))
                AttachToCardio();
            else DetachFromCardio();

            return Interlocked.Exchange(ref m_Seed, seed);
        }

        public virtual bool Ping()
        {
            return !IsClosed;
        }

        public void SetOnPulseStateChange(Action<object, RedisCardioPulseStatus> onPulseStateChange)
        {
            Interlocked.Exchange(ref m_OnPulseStateChange, onPulseStateChange);
        }

        protected virtual void OnPoolPulseStateChange(object sender, RedisCardioPulseStatus status)
        {
            var onPulseStateChange = m_OnPulseStateChange;
            if (onPulseStateChange != null)
                onPulseStateChange(sender, status);
        }

        #region Pulse

        protected void AttachToCardio()
        {
            if (!Disposed)
            {
                var settings = Settings;
                if (settings != null && settings.HeartBeatEnabled &&
                    Interlocked.CompareExchange(ref m_ProbeAttached, 1, 0) == 0)
                    RedisCardio.Default.Attach(this, settings.HearBeatIntervalInSecs);
            }
        }

        protected void DetachFromCardio()
        {
            if (Interlocked.CompareExchange(ref m_ProbeAttached, 0, 1) == 1)
                RedisCardio.Default.Detach(this);
        }

        RedisHeartBeatPulseResult IRedisHeartBeatProbe.Pulse()
        {
            if (!Disposed && m_PulseState != 0)
            {
                var success = false;
                try
                {
                    success = Ping();
                }
                catch (Exception) { }
                finally
                {
                    if (success)
                        Interlocked.Exchange(ref m_PulseFailCount, 0);
                    else if (m_PulseFailCount < int.MaxValue)
                        Interlocked.Add(ref m_PulseFailCount, 1);

                    Interlocked.Exchange(ref m_PulseState, 0);
                }

                return success ? RedisHeartBeatPulseResult.Success : RedisHeartBeatPulseResult.Failed;
            }
            return RedisHeartBeatPulseResult.Unknown;
        }

        void IRedisHeartBeatProbe.ResetPulseFailCounter()
        {
            Interlocked.Exchange(ref m_PulseFailCount, 0);
        }

        void IRedisHeartBeatProbe.PulseStateChanged(RedisCardioPulseStatus status)
        {
            OnPulseStateChanged(status);
        }

        protected virtual void OnPulseStateChanged(RedisCardioPulseStatus status)
        {
            var onPulseFail = m_OnPulseStateChange;
            if (onPulseFail != null)
                onPulseFail.InvokeAsync(this, status);
        }

        #endregion Pulse

        #endregion Methods
    }
}
