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
    internal class RedisHeartBeatProbe : RedisDisposable, IRedisHeartBeatProbe
    {
        #region Field Members

        private int m_PulseState;
        private bool m_ProbeAttached;
        private int m_PulseFailCount;

        private IRedisPingable m_Client;
        private RedisConnectionSettings m_Settings;
        private Action<object, RedisCardioPulseStatus> m_OnPulseStateChange;

        #endregion Field Members

        #region .Ctors

        public RedisHeartBeatProbe(RedisConnectionSettings settings, IRedisPingable client, Action<object, RedisCardioPulseStatus> onPulseStateChange)
        {
            m_Client = client;
            m_Settings = settings;
            m_OnPulseStateChange = onPulseStateChange;
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
            base.OnDispose(disposing);

            Interlocked.Exchange(ref m_Client, null);
            Interlocked.Exchange(ref m_OnPulseStateChange, null);
        }

        #endregion Destructors

        #region Properties

        public int PulseFailCount
        {
            get { return m_PulseFailCount; }
        }

        public bool Pulsing
        {
            get { return m_PulseState != 0; }
        }

        #endregion Properties

        #region Methods

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

        protected virtual void OnPubSubPulseStateChange(object sender, RedisCardioPulseStatus status)
        {
            var onPulseStateChange = m_OnPulseStateChange;
            if (onPulseStateChange != null)
                onPulseStateChange(sender, status);
        }

        #region Pulse

        public void AttachToCardio()
        {
            if (!Disposed && !m_ProbeAttached)
            {
                var settings = m_Settings;
                if (settings != null && settings.HeartBeatEnabled)
                {
                    m_ProbeAttached = true;
                    RedisCardio.Default.Attach(this, settings.HearBeatIntervalInSecs);
                }
            }
        }

        public void DetachFromCardio()
        {
            if (m_ProbeAttached && !Disposed)
                RedisCardio.Default.Detach(this);
        }

        RedisHeartBeatPulseResult IRedisHeartBeatProbe.Pulse()
        {
            if (!Disposed && Interlocked.CompareExchange(ref m_PulseState, 1, 0) == 0)
            {
                var success = false;
                try
                {
                    var client = m_Client;
                    if (client != null)
                        success = client.Ping();
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
