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
using System.Collections.Generic;

namespace Sweet.Redis.v2
{
    public class RedisSynchronizedQueue<T>
    {
        #region Constants

        private const int DefaultCapacity = 16;

        #endregion Constants

        #region Field Members

        private readonly Queue<T> m_Queue;
        private readonly object m_Lock = new object();

        private int m_Count;

        #endregion Field Members

        #region .Ctors

        public RedisSynchronizedQueue(int capacity = -1)
        {
            m_Queue = new Queue<T>(Math.Max(capacity, DefaultCapacity));
        }

        #endregion .Ctors

        #region Properties

        public int Count
        {
            get
            {
                return m_Count;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return m_Count == 0;
            }
        }

        #endregion Properties

        #region Methods

        public T Dequeue()
        {
            if (m_Count > 0)
            {
                lock (m_Lock)
                {
                    if (m_Count > 0)
                    {
                        var result = m_Queue.Dequeue();
                        m_Count--;

                        return result;
                    }
                }
            }
            return default(T);
        }

        public bool TryDequeue(out T result)
        {
            if (m_Count == 0)
            {
                result = default(T);
                return false;
            }

            lock (m_Lock)
            {
                result = default(T);
                if (m_Count > 0)
                {
                    result = m_Queue.Dequeue();
                    m_Count--;
                    return true;
                }
            }
            return false;
        }

        public void Enqueue(T item)
        {
            lock (m_Lock)
            {
                m_Queue.Enqueue(item);
                m_Count++;
            }
        }

        public T[] ToArray()
        {
            lock (m_Lock)
            {
                return m_Queue.ToArray();
            }
        }

        #endregion Methods
    }
}
