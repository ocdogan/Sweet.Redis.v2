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
using System.Threading;

namespace Sweet.Redis.v2
{
    #region RedisMemDb

    public class RedisMemDb<T> : RedisDisposable, IEnumerable, IEnumerable<T>, ICollection
    {
        #region Field Members

        private readonly object m_SyncRoot = new object();

        protected RedisPartitionedList<T> m_Items;

        #endregion Field Members

        #region .Ctors

        protected RedisMemDb(RedisPartitionedList<T> items)
        {
            if (items != null) items.Sort();
            m_Items = items ?? new RedisPartitionedList<T>(10);
        }

        public RedisMemDb(int capacity = -1)
        {
            m_Items = new RedisPartitionedList<T>(Math.Max(10, capacity));
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
            lock (m_SyncRoot)
            {
                var items = Interlocked.Exchange(ref m_Items, null);
                if (items != null)
                    items.Clear();
            }
        }

        #endregion Destructors

        #region Properties

        public T this[int index]
        {
            get
            {
                ValidateNotDisposed();
                lock (m_SyncRoot)
                {
                    return m_Items[index];
                }
            }
            protected set
            {
                ValidateNotDisposed();
                lock (m_SyncRoot)
                {
                    m_Items[index] = value;
                }
            }
        }

        public int Count
        {
            get
            {
                ValidateNotDisposed();
                lock (m_SyncRoot)
                {
                    return m_Items.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public object SyncRoot
        {
            get { return m_SyncRoot; }
        }

        public virtual uint Version
        {
            get { return 1; }
        }

        #endregion Properties

        #region Methods

        #region Collection Methods

        public IEnumerator<T> GetEnumerator()
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                return m_Items.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                return m_Items.GetEnumerator();
            }
        }

        public virtual void Sort()
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                m_Items.Sort();
            }
        }

        public virtual void Clear()
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                m_Items.Clear();
            }
        }

        public virtual bool Contains(T item)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                return m_Items.Contains(item);
            }
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                m_Items.CopyTo(array, arrayIndex);
            }
        }

        public virtual void CopyTo(Array array, int index)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                ((ICollection)m_Items).CopyTo(array, index);
            }
        }

        public virtual int IndexOf(T item)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                return m_Items.IndexOf(item);
            }
        }

        #region Protected ICollection<T>, IList<T>

        protected virtual void Add(T item)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                m_Items.Put(item);
            }
        }

        protected virtual bool Remove(T item)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                return m_Items.Remove(item);
            }
        }

        protected virtual void Insert(int index, T item)
        {
            ValidateNotDisposed();
            throw new NotImplementedException();
        }

        protected virtual void RemoveAt(int index)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                m_Items.RemoveAt(index);
            }
        }

        #endregion Protected ICollection<T>, IList<T>

        #endregion Collection Methods

        #region Search

        protected int BinarySearch<K>(int index, int length, K value, Func<T, K, int> comparer)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                if (m_Items != null)
                    return m_Items.BinarySearch(index, length, value, comparer);
            }
            return -1;
        }

        #endregion Search

        #endregion Methods
    }

    #endregion RedisMemDb
}
