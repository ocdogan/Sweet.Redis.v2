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
using System.Linq;
using System.Net;
using System.Threading;

namespace Sweet.Redis.v2
{
    internal class RedisManagedSentinelNode : RedisManagedNode
    {
        #region Field Members

        private Action<object, RedisCardioPulseStatus> m_OnPulseStateChange;

        #endregion Field Members
        
        #region .Ctors

        public RedisManagedSentinelNode(RedisManagerSettings settings, RedisManagedSentinelListener sentinel,
                                    Action<object, RedisCardioPulseStatus> onPulseStateChange, bool ownsSeed = true)
            : base(settings, RedisRole.Sentinel, sentinel, onPulseStateChange, ownsSeed)
        {
            m_OnPulseStateChange = onPulseStateChange;

            m_EndPoint = GetEndPoint(sentinel);

            if (sentinel != null)
                sentinel.SetOnPulseStateChange(onPulseStateChange);
        }

        private RedisEndPoint GetEndPoint(RedisManagedSentinelListener sentinel)
        {
            var result = (RedisEndPoint)null;
            if (sentinel != null)
            {
                var sEndPoint = sentinel.EndPoint;
                if (sEndPoint != null)
                {
                    var ipEP = sEndPoint as IPEndPoint;
                    if (ipEP != null)
                        result = new RedisEndPoint(ipEP.Address.ToString(), ipEP.Port);
                    else
                    {
                        result = sEndPoint as RedisEndPoint;
                        if (ReferenceEquals(result, null))
                        {
                            var dnsEP = sEndPoint as DnsEndPoint;
                            if (dnsEP != null)
                                result = new RedisEndPoint(dnsEP.Host, ipEP.Port);
                        }
                    }
                }
            }

            if (ReferenceEquals(result, null) || result.IsEmpty)
            {
                var endPoints = Settings.EndPoints;
                if (!endPoints.IsEmpty())
                    result = endPoints.FirstOrDefault(ep => !ReferenceEquals(ep, null) && !ep.IsEmpty);
            }

            return result ?? RedisEndPoint.Empty;
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            var sentinel = (RedisManagedSentinelListener)m_Seed;
            if (sentinel != null)
                sentinel.SetOnPulseStateChange(null);
        }

        #endregion Destructors

        #region Properties

        public override bool Disposed
        {
            get
            {
                if (!base.Disposed)
                    return !((RedisManagedSentinelListener)m_Seed).IsAlive();
                return true;
            }
        }

        public RedisManagedSentinelListener Listener
        {
            get { return (RedisManagedSentinelListener)m_Seed; }
        }

        public override bool IsClosed
        {
            get
            {
                if (!Disposed)
                {
                    var closed = base.IsClosed;
                    if (!closed)
                    {
                        var sentinel = (RedisManagedSentinelListener)m_Seed;
                        if (!ReferenceEquals(sentinel, null))
                            closed = sentinel.IsDown;
                    }
                    return closed;
                }
                return true;
            }
            set
            {
                if (!Disposed)
                {
                    base.IsClosed = value;

                    var sentinel = (RedisManagedSentinelListener)m_Seed;
                    if (!ReferenceEquals(sentinel, null))
                        sentinel.IsDown = value;
                }
            }
        }

        public override bool IsHalfClosed
        {
            get
            {
                if (!Disposed)
                {
                    var closed = base.IsHalfClosed;
                    if (!closed)
                    {
                        var sentinel = (RedisManagedSentinelListener)m_Seed;
                        if (!ReferenceEquals(sentinel, null))
                            closed = sentinel.SDown;
                    }
                    return closed;
                }
                return true;
            }
            set
            {
                if (!Disposed)
                {
                    base.IsHalfClosed = value;

                    var sentinel = (RedisManagedSentinelListener)m_Seed;
                    if (!ReferenceEquals(sentinel, null))
                        sentinel.SDown = value;
                }
            }
        }

        public override RedisRole Role
        {
            get { return RedisRole.Sentinel; }
            set { }
        }

        #endregion Properties

        #region Methods

        protected override object ExchangeSeedInternal(object seed)
        {
            if (!(ReferenceEquals(seed, null) || seed is RedisManagedSentinelListener))
                throw new RedisException("Invalid seed type");

            try
            {
                var sentinel = (RedisManagedSentinelListener)seed;

                var oldSentinel = (RedisManagedSentinelListener)Interlocked.Exchange(ref m_Seed, sentinel);
                if (oldSentinel != null)
                {
                    oldSentinel.SetOnPulseStateChange(null);
                    oldSentinel.Dispose();
                }

                if (!Disposed)
                    m_EndPoint = GetEndPoint(sentinel);

                if (sentinel.IsAlive())
                    sentinel.SetOnPulseStateChange(m_OnPulseStateChange);

                return oldSentinel;
            }
            finally
            {
                if (!ReferenceEquals(seed, null))
                    AttachToCardio();
                else DetachFromCardio();
            }
        }

        public override bool Ping()
        {
            if (((RedisManagedSentinelListener)m_Seed).IsAlive())
            {
                try
                {
                    return ((IRedisPingable)m_Seed).Ping();
                }
                catch (Exception)
                { }
            }
            return false;
        }

        #endregion Methods
    }
}
