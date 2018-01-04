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
    internal class RedisManagedServerNode : RedisManagedNode
    {
        #region Field Members

        private Action<object, RedisCardioPulseStatus> m_OnPulseStateChange;

        #endregion Field Members
        #region .Ctors

        public RedisManagedServerNode(RedisManagerSettings settings, RedisRole role, RedisManagedServer server,
                                    Action<object, RedisCardioPulseStatus> onPulseStateChange, bool ownsSeed = true)
            : base(settings, role, server, onPulseStateChange, ownsSeed)
        {
            m_OnPulseStateChange = onPulseStateChange;
            m_EndPoint = (server != null) ? server.EndPoint : RedisEndPoint.Empty;
            if (server != null)
                server.SetOnPulseStateChange(onPulseStateChange);
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            var server = (RedisManagedServer)m_Seed;
            if (server != null)
                server.SetOnPulseStateChange(null);
        }

        #endregion Destructors

        #region Properties

        public override bool Disposed
        {
            get
            {
                if (!base.Disposed)
                    return !((RedisManagedServer)m_Seed).IsAlive();
                return true;
            }
        }

        public RedisManagedServer Server
        {
            get { return (RedisManagedServer)m_Seed; }
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
                        var server = (RedisManagedServer)m_Seed;
                        if (!ReferenceEquals(server, null))
                            closed = server.IsDown;
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

                    var server = (RedisManagedServer)m_Seed;
                    if (!ReferenceEquals(server, null))
                        server.IsDown = value;
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
                        var server = (RedisManagedServer)m_Seed;
                        if (!ReferenceEquals(server, null))
                            closed = server.SDown;
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

                    var server = (RedisManagedServer)m_Seed;
                    if (!ReferenceEquals(server, null))
                        server.SDown = value;
                }
            }
        }

        public override RedisRole Role
        {
            get { return base.Role; }
            set
            {
                base.Role = value;

                var server = (RedisManagedServer)m_Seed;
                if (!ReferenceEquals(server, null))
                    server.Role = value;
            }
        }

        public RedisManagedNodeStatus Status
        {
            get { return m_Status; }
            set
            {
                if (!m_Status.HasFlag(RedisManagedNodeStatus.Disposed))
                    m_Status = value;
            }
        }

        #endregion Properties

        #region Methods

        protected override object ExchangeSeedInternal(object seed)
        {
            if (!(ReferenceEquals(seed, null) || seed is RedisManagedServer))
                throw new RedisException("Invalid seed type");

            var server = (RedisManagedServer)seed;

            var oldServer = (RedisManagedServer)Interlocked.Exchange(ref m_Seed, server);
            if (oldServer != null)
                oldServer.SetOnPulseStateChange(null);

            if (!Disposed)
                m_EndPoint = server.IsAlive() ? server.EndPoint : RedisEndPoint.Empty;

            if (server.IsAlive())
            {
                server.Role = m_Role;
                server.SetOnPulseStateChange(m_OnPulseStateChange);
            }

            return oldServer;
        }

        public override bool Ping()
        {
            if (((RedisManagedServer)m_Seed).IsAlive())
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
