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
    public class RedisNull : RedisResult<RedisNullValue>
    {
        #region Static Members

        private static readonly RedisNull Default = new RedisNull(RedisNullValue.Value);

        #endregion Static Members

        #region .Ctors

        internal RedisNull()
        { }

        private RedisNull(RedisNullValue value)
            : base(value ?? RedisNullValue.Value)
        { }

        #endregion .Ctors

        #region Properties

        public override RedisNullValue Value
        {
            get
            {
                ValidateCompleted();
                return RedisNullValue.Value;
            }
            internal set
            {
                base.Value = (value is RedisNullValue) ? value : RedisNullValue.Value;
            }
        }

        public override RedisResultType Type
        {
            get { return RedisResultType.Null; }
        }

        internal protected override object RawData
        {
            get
            {
                return base.RawData;
            }
            set
            {
                base.RawData = (value is RedisNullValue) ? value : RedisNullValue.Value;
            }
        }

        #endregion Properties

        #region Methods

        #region Overrides

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return true;

            if (ReferenceEquals(obj, this))
                return true;

            if (obj is RedisNull)
                return true;

            if (obj is RedisNullValue)
                return true;

            return Object.Equals(obj, null);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return "(nil)";
        }

        #endregion Overrides

        #endregion Methods

        #region Operator Overloads

        public static bool operator ==(RedisNull a, RedisNull b)
        {
            return true;
        }

        public static bool operator !=(RedisNull a, RedisNull b)
        {
            return !(a == b);
        }

        #endregion Operator Overloads
    }
}
