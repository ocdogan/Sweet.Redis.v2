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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Sweet.Redis.v2
{
    public class RedisResult<TValue> : RedisResult
    {
        #region Field Members

        protected TValue m_Value;

        #endregion Field Members

        #region .Ctors

        internal RedisResult()
        { }

        internal RedisResult(int expectedLength)
            : base(expectedLength)
        { }

        internal RedisResult(TValue value, int expectedLength)
            : base(value, expectedLength)
        {
            m_Value = value;
        }

        public RedisResult(TValue value)
            : base(value)
        {
            m_Value = value;
        }

        public RedisResult(TValue value, RedisResultStatus status)
            : base(value, status)
        {
            m_Value = value;
        }

        #endregion .Ctors

        #region Properties

        public virtual TValue Value
        {
            get
            {
                ValidateCompleted();
                return m_Value;
            }
            internal set
            {
                TrySetResult(value);
            }
        }

        #endregion Properties

        #region Methods

        protected internal override void TrySetResult(object value)
        {
            m_Value = (TValue)value;
            base.TrySetResult(value);
        }

        #region Overrides

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion Overrides

        #endregion Methods

        #region Operator Overloads

        public static bool operator ==(RedisResult<TValue> a, RedisResult<TValue> b)
        {
            if (ReferenceEquals(a, null))
            {
                if (ReferenceEquals(b, null))
                    return true;

                var raw = b.m_RawData;
                return ReferenceEquals(raw, null) || Object.Equals(raw, null);
            }

            if (ReferenceEquals(b, null))
            {
                var raw = a.m_RawData;
                return ReferenceEquals(raw, null) || Object.Equals(raw, null);
            }

            if (ReferenceEquals(a, b))
                return true;

            return (a.Status == b.Status) && Object.Equals(a.m_RawData, b.m_RawData);
        }

        public static bool operator !=(RedisResult<TValue> a, RedisResult<TValue> b)
        {
            return !(a == b);
        }

        #endregion Operator Overloads
    }

    public class RedisResult
    {
        #region Constants

        protected static readonly int Uninitialized = (int)RedisResultStatus.Uninitialized;
        protected static readonly int Completed = (int)RedisResultStatus.Completed;
        protected static readonly int Canceled = (int)RedisResultStatus.Canceled;
        protected static readonly int Failed = (int)RedisResultStatus.Failed;

        #endregion Constants

        #region Field Members

        protected int m_Status;
        protected object m_RawData;
        protected Exception m_Exception;

        protected int m_ExpectedLength = -1;

        #endregion Field Members

        #region .Ctors

        protected RedisResult()
        { }

        protected RedisResult(int expectedLength)
        {
            m_ExpectedLength = Math.Max(-1, expectedLength);
        }

        protected RedisResult(object value)
        {
            m_RawData = value;
            m_Status = Completed;
        }
        
        protected RedisResult(object value, int expectedLength)
        {
            m_RawData = value;
            m_Status = Completed;
            m_ExpectedLength = Math.Max(-1, expectedLength);
        }

        protected RedisResult(object value, RedisResultStatus status)
        {
            m_RawData = value;
            m_Status = (int)status;
        }

        #endregion .Ctors

        #region Properties

        public bool IsCanceled
        {
            get { return m_Status == Canceled; }
        }

        public bool IsCompleted
        {
            get { return m_Status != Uninitialized; }
            protected set
            {
                Interlocked.Exchange(ref m_Status, value ? Completed : Uninitialized);
            }
        }

        public bool IsFaulted
        {
            get { return m_Status == Failed; }
        }

        public virtual int Length
        {
            get
            {
                ValidateCompleted();
                return 0;
            }
        }

        public int ExpectedLength
        {
            get { return m_ExpectedLength; }
        }

        protected internal virtual object RawData
        {
            get
            {
                return m_RawData;
            }
            set
            {
                TrySetResult(value);
            }
        }

        public RedisResultStatus Status
        {
            get { return (RedisResultStatus)m_Status; }
            internal set { Interlocked.Exchange(ref m_Status, (int)value); }
        }

        public virtual RedisResultType Type
        {
            get { return RedisResultType.Custom; }
        }

        #endregion Properties

        #region Methods

        protected virtual void ValidateCompleted()
        {
            if (!IsCompleted)
                throw new RedisException("Result is not completed", RedisErrorCode.UncompleteTransaction);
        }

        protected internal virtual void TrySetCompleted()
        {
            Interlocked.Exchange(ref m_Status, Completed);
        }

        protected internal virtual void TrySetResult(object value)
        {
            m_RawData = value;
            Interlocked.Exchange(ref m_Status, Completed);
        }

        protected internal virtual void TrySetException(Exception exception)
        {
            if (exception != null)
            {
                Interlocked.Exchange(ref m_Status, Failed);
                Interlocked.Exchange(ref m_Exception, exception);

                GC.KeepAlive(exception);
            }
        }

        protected internal virtual void TryCancel()
        {
            Interlocked.CompareExchange(ref m_Status, Canceled, Uninitialized);
        }

        #region Overrides

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                var value = m_RawData;
                return ReferenceEquals(value, null) || (value == null);
            }

            if (ReferenceEquals(obj, this))
                return true;

            if (obj is RedisResult &&
               Object.Equals(m_RawData, ((RedisResult)obj).m_RawData))
                return true;

            return Object.Equals(m_RawData, obj);
        }

        public override int GetHashCode()
        {
            var value = m_RawData;
            if (ReferenceEquals(value, null))
                return base.GetHashCode();
            return value.GetHashCode();
        }

        public override string ToString()
        {
            if (!IsCompleted)
                return "(nil) - [Not completed]";

            var value = m_RawData;
            if (ReferenceEquals(value, null))
                return "(nil)";

            if (value is IEnumerable)
            {
                var enumerable = (IEnumerable)value;

                var i = 0;
                var sBuilder = new StringBuilder();

                foreach (var item in enumerable)
                {
                    sBuilder.Append(++i);
                    sBuilder.Append(") ");

                    if (item == null)
                        sBuilder.Append("(nil)");
                    else
                        sBuilder.Append(item);

                    sBuilder.AppendLine();
                }

                return sBuilder.ToString();
            }

            return value.ToString();
        }

        #endregion Overrides

        #endregion Methods

        #region Operator Overloads

        public static bool operator ==(RedisResult a, RedisResult b)
        {
            if (ReferenceEquals(a, null))
            {
                if (ReferenceEquals(b, null))
                    return true;

                var raw = b.m_RawData;
                return ReferenceEquals(raw, null) || Object.Equals(raw, null);
            }

            if (ReferenceEquals(b, null))
            {
                var raw = a.m_RawData;
                return ReferenceEquals(raw, null) || Object.Equals(raw, null);
            }

            if (ReferenceEquals(a, b))
                return true;

            return (a.Status == b.Status) && Object.Equals(a.m_RawData, b.m_RawData);
        }

        public static bool operator !=(RedisResult a, RedisResult b)
        {
            return !(a == b);
        }

        #endregion Operator Overloads
    }
}
