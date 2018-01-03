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
using System.Threading;

namespace Sweet.Redis.v2
{
    public class RedisAsyncQueue<T>
    {
        #region Field Members

        private int m_Count;
        private readonly object m_Lock = new object();

        private readonly Queue<T> m_DefaultQ = new Queue<T>();
        private readonly Queue<T> m_HighPriorityQ = new Queue<T>();

        #endregion Field Members

        #region Properties

        public int Count
        {
            get { return m_Count; }
        }

        public bool IsEmpty
        {
            get { return m_Count == 0; }
        }

        public object SyncLock
        {
            get { return m_Lock; }
        }

        #endregion Properties

        #region Methods

        public void Enqueue(T item, bool highPriority)
        {
            lock (m_Lock)
            {
                (highPriority ? m_HighPriorityQ : m_DefaultQ).Enqueue(item);                
                m_Count++;
            }
        }

        public bool TryDequeue(out T item)
        {
            if (m_Count > 0)
            {
                lock (m_Lock)
                {
                    if (m_HighPriorityQ.Count > 0)
                    {
                        item = m_HighPriorityQ.Dequeue();
                        m_Count--;
                        return true;
                    }

                    if (m_DefaultQ.Count > 0)
                    {
                        item = m_DefaultQ.Dequeue();
                        m_Count--;
                        return true;
                    }
                }
            }

            item = default(T);
            return false;
        }

        #endregion Methods
    }
}
