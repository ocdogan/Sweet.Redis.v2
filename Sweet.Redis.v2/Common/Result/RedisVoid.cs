﻿#region License
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

namespace Sweet.Redis.v2
{
    public class RedisVoid : RedisResult<RedisVoidVal>
    {
        #region Static Members

        public static readonly RedisVoid Default = new RedisVoid(true);

        #endregion

        #region Field Members

        private bool m_IsDefault;

        #endregion Field Members

        #region .Ctors

        private RedisVoid(bool isDefault)
        {
            m_IsDefault = isDefault;
            TrySetResult(null);
        }
        
        internal RedisVoid()
        {
            m_RawData = RedisVoidVal.Value;
        }

        internal RedisVoid(int dummy)
        {
            TrySetResult(null);
        }

        #endregion .Ctors

        #region Properties

        protected internal override object RawData
        {
            get { return RedisVoidVal.Value; }
            set
            {
                TrySetResult(null);
            }
        }

        public override RedisResultType Type
        {
            get { return RedisResultType.Void; }
        }

        public override RedisVoidVal Value
        {
            get
            {
                ValidateCompleted();
                return RedisVoidVal.Value;
            }
            internal set
            {
                TrySetResult(null);
            }
        }

        #endregion Properties

        #region Methods

        #region Overrides

        protected override void ValidateCompleted()
        {
            if (!m_IsDefault)
                base.ValidateCompleted();
        }

        protected internal override void TrySetResult(object value)
        {
            m_Value = RedisVoidVal.Value;
            m_RawData = RedisVoidVal.Value;
            TrySetCompleted();
        }

        protected internal override void TrySetException(Exception exception)
        {
            if (!m_IsDefault)
                base.TrySetException(exception);
        }

        protected internal override void TryCancel()
        {
            if (!m_IsDefault)
                base.TryCancel();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            if (ReferenceEquals(obj, this))
                return true;

            if (obj is RedisVoid)
                return true;

            return Object.Equals(obj, null);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return "void";
        }

        #endregion Methods

        #endregion Overrides

        #region Operator Overloads        		

        public static bool operator ==(RedisVoid a, RedisVoid b)
        {
            return true;
        }

        public static bool operator !=(RedisVoid a, RedisVoid b)
        {
            return !(a == b);
        }

        #endregion Operator Overloads
    }
}
