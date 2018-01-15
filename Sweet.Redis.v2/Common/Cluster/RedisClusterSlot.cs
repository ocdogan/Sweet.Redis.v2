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

namespace Sweet.Redis.v2
{
    public class RedisClusterSlot : IComparable, IComparable<RedisClusterSlot>
    {
        #region Field Members

        private int m_Start = -1;
        private int m_End = -1;

        #endregion Field Members

        #region .Ctors

        public RedisClusterSlot(int start, int end)
        {
            m_Start = Math.Min(Math.Max(-1, start), RedisConstants.ClusterSlotMod - 1);
            m_End = Math.Max(m_Start, Math.Min(Math.Max(-1, end), RedisConstants.ClusterSlotMod - 1));
        }

        #endregion .Ctors

        #region Properties

        public int Start
        {
            get { return m_Start; }
        }

        public int End
        {
            get { return m_End; }
        }

        #endregion Properties

        #region Methods

        private int CompareWith(RedisClusterSlot other)
        {
            var comparison = m_Start.CompareTo(other.m_Start);
            if (comparison == 0)
                return m_End.CompareTo(other.m_End);
            return comparison;
        }

        public int CompareTo(object obj)
        {
            if (obj is RedisClusterSlot)
                return CompareWith((RedisClusterSlot)obj);
            return -1;
        }

        public int CompareTo(RedisClusterSlot other)
        {
            return CompareWith(other);
        }

        public override string ToString()
        {
            return String.Format("[Slot Range: {0} - {1}]", m_Start, m_End);
        }

        #endregion Methods
    }
}
