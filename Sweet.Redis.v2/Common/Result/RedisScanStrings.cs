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

namespace Sweet.Redis.v2
{
    public class RedisScanStrings : RedisResult<RedisScanStringsData>
    {
        #region .Ctors

        internal RedisScanStrings()
        { }

        public RedisScanStrings(RedisScanStringsData value)
            : base(value)
        { }

        #endregion .Ctors

        #region Properties

        public override RedisResultType Type { get { return RedisResultType.Scan; } }

        #endregion Properties

        #region Overrides

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            if (ReferenceEquals(obj, this))
                return true;

            var status = m_Status;

            var bObj = obj as RedisScanStrings;
            if (!ReferenceEquals(bObj, null))
                return (bObj.m_Status == status) && (bObj.m_RawData == m_RawData);

            var rObj = obj as RedisResult<RedisScanStrings>;
            if (!ReferenceEquals(rObj, null))
                return (rObj.Status == (RedisResultStatus)status) && (rObj.RawData == m_RawData);

            return false;
        }

        public override int GetHashCode()
        {
            var value = m_RawData;
            if (ReferenceEquals(value, null))
                return base.GetHashCode();
            return value.GetHashCode();
        }

        #endregion Methods

        #region Conversion Methods

        public static implicit operator RedisScanStrings(RedisScanStringsData value)  // implicit RedisScan conversion operator
        {
            return new RedisScanStrings(value);
        }

        public static implicit operator RedisScanStringsData(RedisScanStrings value)  // implicit RedisScan conversion operator
        {
            if (value == null)
                return null;

            return value.Value;
        }

        #endregion Conversion Methods

        #region Operator Overloads

        public static bool operator ==(RedisScanStrings a, RedisScanStrings b)
        {
            if (ReferenceEquals(a, null))
                return ReferenceEquals(b, null);

            if (ReferenceEquals(b, null))
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(RedisScanStrings a, RedisScanStrings b)
        {
            return !(b == a);
        }

        #endregion Operator Overloads
    }
}
