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
    public class RedisNullableDouble : RedisResult<double?>
    {
        #region .Ctors

        internal RedisNullableDouble()
        { }

        public RedisNullableDouble(double? value)
            : base(value)
        { }

        #endregion .Ctors

        #region Properties

        public override RedisResultType Type { get { return RedisResultType.NullableDouble; } }

        #endregion Properties

        #region Methods

        #region Overrides

        #endregion Methods

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            if (ReferenceEquals(obj, this))
                return true;

            var rObj = obj as RedisNullableDouble;
            if (!ReferenceEquals(rObj, null))
                return (rObj.m_Status == m_Status) && (rObj.m_RawData == m_RawData);
            return false;
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
            var value = (double?)m_RawData;
            if (value == null)
                return "(nil)";

            return "\"" + value.Value.ToString() + "\"";
        }

        #endregion Overrides

        #region Conversion Methods

        public static implicit operator RedisNullableDouble(double? value)  // implicit double? to RedisNullableDouble conversion operator
        {
            return new RedisNullableDouble(value);
        }

        public static implicit operator RedisNullableDouble(double value)  // implicit double to RedisNullableDouble conversion operator
        {
            return new RedisNullableDouble(value);
        }

        public static implicit operator double? (RedisNullableDouble value)  // implicit RedisNullableDouble to double? conversion operator
        {
            return value.Value;
        }

        public static implicit operator RedisNullableDouble(long? value)  // implicit long? to RedisNullableDouble conversion operator
        {
            return new RedisNullableDouble(value);
        }

        public static implicit operator RedisNullableDouble(long value)  // implicit long to RedisNullableDouble conversion operator
        {
            return new RedisNullableDouble(value);
        }

        public static implicit operator RedisNullableDouble(int? value)  // implicit int? to RedisNullableDouble conversion operator
        {
            return new RedisNullableDouble(value);
        }

        public static implicit operator RedisNullableDouble(int value)  // implicit int? to RedisNullableDouble conversion operator
        {
            return new RedisNullableDouble(value);
        }

        public static implicit operator RedisNullableDouble(RedisInteger value)  // implicit RedisInt to RedisNullableDouble conversion operator
        {
            return new RedisNullableDouble(value);
        }

        public static implicit operator RedisNullableDouble(RedisNullableInteger value)  // implicit RedisNullableInt to RedisNullableDouble conversion operator
        {
            return new RedisNullableDouble(value);
        }

        #endregion Conversion Methods

        #region Operator Overloads

        public static bool operator ==(RedisNullableDouble a, RedisNullableDouble b)
        {
            if (ReferenceEquals(a, null))
                return ReferenceEquals(b, null);

            if (ReferenceEquals(b, null))
                return false;

            if (ReferenceEquals(a, b))
                return true;

            return (a.m_Status == b.m_Status) && (a.m_RawData == b.m_RawData);
        }

        public static bool operator !=(RedisNullableDouble a, RedisNullableDouble b)
        {
            return !(a == b);
        }

        #endregion Operator Overloads
    }
}
