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
    public class RedisInternalDisposable : IRedisDisposableBase
    {
        #region Field Members

        private bool m_Disposed;
        private int m_Disposing;
        private bool m_FinalizationSuppressed;
        private event Action<RedisInternalDisposable> m_OnDispose;

        #endregion Field Members

        #region .Ctors

        public RedisInternalDisposable()
        {
            if (!UsesFinalization())
            {
                m_FinalizationSuppressed = true;
                GC.SuppressFinalize(this);
            }
        }

        public RedisInternalDisposable(Action<RedisInternalDisposable> onDispose)
        {
            m_OnDispose = onDispose;
            if (!UsesFinalization())
            {
                m_FinalizationSuppressed = true;
                GC.SuppressFinalize(this);
            }
        }

        #endregion .Ctors

        #region Destructors

        ~RedisInternalDisposable()
        {
            Dispose(false);
        }

        protected internal virtual void Dispose()
        {
            Dispose(true);
        }

        protected virtual bool UsesFinalization()
        {
            return true;
        }

        protected virtual bool SuppressFinalization()
        {
            return true;
        }

        private void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref m_Disposing, 1, 0) == 0)
            {
                try
                {
                    DoDispose(disposing);
                }
                finally
                {
                    Interlocked.Exchange(ref m_Disposing, 0);
                }
            }
        }

        private void DoDispose(bool disposing)
        {
            var alreadyDisposed = m_Disposed;
            try
            {
                OnBeforeDispose(disposing, alreadyDisposed);
            }
            finally
            {
                m_Disposed = true;
                try
                {
                    var onDispose = Interlocked.Exchange(ref m_OnDispose, null);
                    if (onDispose != null)
                        onDispose(this);
                }
                finally
                {
                    if (disposing && !m_FinalizationSuppressed
                        && SuppressFinalization())
                    {
                        m_FinalizationSuppressed = true;
                        GC.SuppressFinalize(this);
                    }

                    if (!disposing)
                        OnFinalize();
                    else if (!alreadyDisposed)
                        OnDispose(disposing);
                }
            }
        }

        protected virtual void OnBeforeDispose(bool disposing, bool alreadyDisposed)
        { }

        protected virtual void OnDispose(bool disposing)
        { }

        protected virtual void OnFinalize()
        { }

        internal void AddOnDispose(Action<RedisInternalDisposable> onDispose)
        {
            m_OnDispose += onDispose;
        }

        internal void RemoveOnDispose(Action<RedisInternalDisposable> onDispose)
        {
            m_OnDispose -= onDispose;
        }

        #endregion Destructors

        #region Properties

        public virtual bool Disposed
        {
            get { return m_Disposed; }
        }

        #endregion Properties

        #region Methods

        public virtual void ValidateNotDisposed()
        {
            if (Disposed)
                throw new RedisException(GetType().Name + " is disposed");
        }

        #endregion Methods
    }
}
