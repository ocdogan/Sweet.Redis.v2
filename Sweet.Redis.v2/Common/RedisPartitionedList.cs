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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sweet.Redis.v2
{
    #region RedisPartitionedList

    public class RedisPartitionedList<T> : RedisDisposable, IEnumerable, IEnumerable<T>
    {
        #region IEnumerator<T>

        [Serializable]
        public struct PartitionedListEnumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private RedisPartitionedList<T> m_List;
            private List<Bucket> m_Buckets;
            private int m_BucketCount;
            private int m_CurrBucket;
            private int m_CurrItem;
            private int m_Version;
            private T m_Current;

            public T Current
            {
                get { return m_Current; }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (m_CurrItem < 0 || m_CurrBucket < 0 || m_CurrBucket > m_BucketCount - 1)
                        throw new Exception("Enum operation can't happen");
                    return Current;
                }
            }

            internal PartitionedListEnumerator(RedisPartitionedList<T> list)
            {
                m_List = list;
                m_Buckets = m_List.m_Buckets;

                m_CurrItem = 0;
                m_CurrBucket = -1;
                m_BucketCount = m_Buckets.Count;

                m_Version = m_List.m_Version;
                m_Current = default(T);
            }

            public void Dispose()
            {
            }

            private Bucket GetCurrentBucket()
            {
                if (m_CurrItem < 0)
                    throw new Exception("Enum operation can't happen");

                if (m_Version != m_List.m_Version)
                    throw new Exception("Enum failed version");

                if (m_CurrBucket < 0) m_CurrBucket = 0;

                while (m_CurrBucket < m_Buckets.Count)
                {
                    var bucket = m_Buckets[m_CurrBucket];
                    if (m_CurrItem < bucket.Count)
                        return bucket;

                    m_CurrItem = 0;
                    m_CurrBucket++;
                }
                return null;
            }

            public bool MoveNext()
            {
                if (m_Version != m_List.m_Version)
                    throw new Exception("Enum failed version");

                var bucket = GetCurrentBucket();
                if (bucket != null)
                {
                    m_Current = bucket[m_CurrItem];
                    m_CurrItem++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (m_Version != m_List.m_Version)
                    throw new Exception("Enum failed version");

                m_CurrItem = -1;
                m_CurrBucket = m_BucketCount;

                m_Current = default(T);
                return false;
            }

            void IEnumerator.Reset()
            {
                if (m_Version != m_List.m_Version)
                    throw new Exception("Enum failed version");

                m_CurrItem = 0;
                m_CurrBucket = -1;
                m_Current = default(T);
            }
        }

        #endregion IEnumerator<T>

        #region Bucket

        private class Bucket : List<T>, IDisposable
        {
            #region Field Members

            private int m_Disposed;
            private RedisPartitionedList<T> m_Parent;
            private readonly object m_SyncRoot = new object();

            #endregion Field Members

            #region .Ctors

            public Bucket(RedisPartitionedList<T> parent, int capacity = -1)
                : base(capacity < 0 ? 0 : capacity)
            {
                m_Parent = parent;
            }

            #endregion .Ctors

            #region Destructors

            ~Bucket()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
            }

            protected virtual void OnDispose()
            {
                lock (m_SyncRoot)
                {
                    m_Parent = null;
                    Clear();
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                var wasDisposed = Interlocked.Exchange(ref m_Disposed, 1) == 1;
                if (wasDisposed)
                    return;

                if (disposing)
                    GC.SuppressFinalize(this);

                OnDispose();
            }

            #endregion Destructors

            #region Properties

            public T Minimum
            {
                get
                {
                    var count = Count;
                    if (count > 0)
                    {
                        var first = base[0];
                        if (count > 1)
                        {
                            if (m_Parent == null || !m_Parent.Sorted)
                            {
                                first = this.Min<T>();
                            }
                            else
                            {
                                var last = base[count - 1];
                                if (Comparer<T>.Default.Compare(last, first) < 0)
                                    return last;
                            }
                        }
                        return first;
                    }
                    return default(T);
                }
            }

            public T Maximum
            {
                get
                {
                    var count = Count;
                    if (count > 0)
                    {
                        var last = base[count - 1];
                        if (count > 1)
                        {
                            if (m_Parent == null || !m_Parent.Sorted)
                            {
                                last = this.Max<T>();
                            }
                            else
                            {
                                var first = base[0];
                                if (Comparer<T>.Default.Compare(first, last) > 0)
                                    return first;
                            }
                        }
                        return last;
                    }
                    return default(T);
                }
            }

            #endregion Properties
        }

        #endregion Bucket

        #region Constants

        private const int LargeObjectHeapSize = 85000;
        private const int MaxArraySize = LargeObjectHeapSize - 1000;

        #endregion Constants

        #region Field Members

        private int m_Count;
        private int m_BucketSize;
        private int m_Version;
        private bool m_Sorted;
        private int m_UncompleteCount;
        private List<Bucket> m_Buckets = new List<Bucket>();

        private readonly object m_SyncRoot = new object();

        #endregion Field Members

        #region .Ctors

        public RedisPartitionedList(int capacity = 0)
        {
            var itemSize = Marshal.SizeOf(typeof(IntPtr));
            if (typeof(T).IsValueType)
                itemSize = Marshal.SizeOf(typeof(T));

            m_BucketSize = MaxArraySize / itemSize;

            if (capacity > 0)
                m_Buckets.Add(new Bucket(this, Math.Min(capacity, m_BucketSize)));
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
            lock (m_SyncRoot)
            {
                var buckets = Interlocked.Exchange(ref m_Buckets, null);
                foreach (var bucket in buckets)
                    bucket.Dispose();

                buckets.Clear();

                m_Count = 0;
                m_UncompleteCount = 0;
                m_Sorted = false;
            }
        }

        #endregion Destructors

        #region Properties

        public int Count
        {
            get { return m_Count; }
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

        public bool Sorted
        {
            get { return m_Sorted; }
        }

        public virtual T this[int index]
        {
            get
            {
                if (index < 0 || index > m_Count - 1)
                    throw new ArgumentOutOfRangeException("index");

                ValidateNotDisposed();
                lock (m_SyncRoot)
                {
                    if (m_UncompleteCount == 0)
                    {
                        var bucketIndex = index / m_BucketSize;
                        index %= m_BucketSize;

                        return m_Buckets[bucketIndex][index];
                    }

                    foreach (var bucket in m_Buckets)
                    {
                        if (index < bucket.Count)
                            return bucket[index];

                        index -= bucket.Count;
                        if (index < 0)
                            throw new ArgumentOutOfRangeException("index");
                    }
                }
                return default(T);
            }
            set
            {
                if (index < 0 || index > m_Count - 1)
                    throw new ArgumentOutOfRangeException("index");

                ValidateNotDisposed();
                lock (m_SyncRoot)
                {
                    m_Version++;

                    if (m_UncompleteCount == 0)
                    {
                        var bucketIndex = index / m_BucketSize;
                        index %= m_BucketSize;

                        m_Buckets[bucketIndex][index] = value;
                        return;
                    }

                    foreach (var bucket in m_Buckets)
                    {
                        if (index < bucket.Count)
                        {
                            bucket[index] = value;
                            return;
                        }

                        index -= bucket.Count;
                        if (index < 0)
                            throw new ArgumentOutOfRangeException("index");
                    }
                    m_Sorted = false;
                }
            }
        }

        #endregion Properties

        #region Methods

        #region ICollection

        public virtual void Put(T item)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                m_Version++;

                Bucket currBucket = null;
                var addedToUncompleted = false;

                var bucketCount = m_Buckets.Count;
                if (bucketCount > 0)
                {
                    if (m_UncompleteCount == 0)
                    {
                        currBucket = m_Buckets[bucketCount - 1];
                        if (currBucket.Count > m_BucketSize - 1)
                            currBucket = null;
                    }
                    else
                    {
                        for (var i = bucketCount - 1; i > -1; i--)
                        {
                            var bucket = m_Buckets[i];
                            if (bucket.Count < m_BucketSize)
                            {
                                currBucket = bucket;
                                addedToUncompleted = (i < bucketCount - 1);
                                break;
                            }
                        }
                    }
                }

                if (currBucket == null)
                {
                    currBucket = new Bucket(this, m_BucketSize);
                    m_Buckets.Add(currBucket);
                }

                currBucket.Add(item);
                m_Count++;

                if (addedToUncompleted)
                    m_UncompleteCount--;
                m_Sorted = false;
            }
        }

        public virtual void Clear()
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                m_Version++;

                var buckets = Interlocked.Exchange(ref m_Buckets, new List<Bucket>());
                foreach (var bucket in buckets)
                    bucket.Dispose();

                buckets.Clear();

                m_Count = 0;
                m_UncompleteCount = 0;
                m_Sorted = false;
            }
        }

        public virtual bool Contains(T item)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                foreach (var bucket in m_Buckets)
                    if (bucket.Contains(item))
                        return true;
            }
            return false;
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                foreach (var partition in m_Buckets)
                {
                    partition.CopyTo(array, arrayIndex);
                    arrayIndex += partition.Count;
                }
            }
        }

        public virtual bool Remove(T item)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                m_Version++;

                Bucket bucket;
                var bucketCount = m_Buckets.Count;

                for (var i = 0; i < bucketCount; i++)
                {
                    bucket = m_Buckets[i];
                    if (bucket.Remove(item))
                    {
                        m_Count--;

                        if (bucket.Count == 0)
                        {
                            m_Buckets.RemoveAt(i);
                            bucket.Dispose();

                            if (i > 0 && i == bucketCount - 1)
                            {
                                bucket = m_Buckets[i - 1];
                                if (bucket.Count < m_BucketSize)
                                    m_UncompleteCount--;
                            }
                        }
                        else if (i < bucketCount - 1)
                            m_UncompleteCount++;

                        return true;
                    }
                }
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            ValidateNotDisposed();
            return new PartitionedListEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            ValidateNotDisposed();
            return new PartitionedListEnumerator(this);
        }

        #endregion ICollection

        #region IList

        public virtual int IndexOf(T item)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                var start = 0;
                foreach (var bucket in m_Buckets)
                {
                    var index = bucket.IndexOf(item);
                    if (index > -1)
                        return start + index;

                    start += bucket.Count;
                }
                return -1;
            }
        }

        public virtual void RemoveAt(int index)
        {
            if (index < 0 || index > m_Count - 1)
                throw new ArgumentOutOfRangeException("index");

            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                m_Version++;

                Bucket bucket;
                var bucketCount = m_Buckets.Count;

                for (var i = 0; i < bucketCount; i++)
                {
                    bucket = m_Buckets[i];

                    if (index < bucket.Count)
                    {
                        bucket.RemoveAt(index);
                        m_Count--;

                        if (bucket.Count == 0)
                        {
                            m_Buckets.RemoveAt(i);
                            bucket.Dispose();

                            if (i > 0 && i == bucketCount - 1)
                            {
                                bucket = m_Buckets[i - 1];
                                if (bucket.Count < m_BucketSize)
                                    m_UncompleteCount--;
                            }
                        }
                        else if (i < bucketCount - 1)
                            m_UncompleteCount++;
                        return;
                    }

                    index -= bucket.Count;
                    if (index < 0)
                        throw new ArgumentOutOfRangeException("index");
                }
            }
        }

        #endregion IList

        #region Sort

        private void Swap(int i, int j)
        {
            if (i != j)
            {
                T t = this[i];
                this[i] = this[j];
                this[j] = t;
            }
        }

        private void SwapIfGreater(IComparer<T> comparer, int a, int b)
        {
            if (a != b && (comparer.Compare(this[a], this[b]) > 0))
            {
                T key = this[a];
                this[a] = this[b];
                this[b] = key;
            }
        }

        private void DownHeap(int i, int n, int lo, IComparer<T> comparer)
        {
            Contract.Requires(comparer != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(lo < m_Count);

            T d = this[lo + i - 1];
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && comparer.Compare(this[lo + child - 1], this[lo + child]) < 0)
                    child++;

                if (!(comparer.Compare(d, this[lo + child - 1]) < 0))
                    break;

                this[lo + i - 1] = this[lo + child - 1];
                i = child;
            }
            this[lo + i - 1] = d;
        }

        private void Heapsort(int lo, int hi, IComparer<T> comparer)
        {
            Contract.Requires(comparer != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi > lo);
            Contract.Requires(hi < m_Count);

            var n = hi - lo + 1;
            for (int i = n / 2; i >= 1; i = i - 1)
            {
                DownHeap(i, n, lo, comparer);
            }
            for (int i = n; i > 1; i = i - 1)
            {
                Swap(lo, lo + i - 1);
                DownHeap(1, i - 1, lo, comparer);
            }
        }

        private void DepthLimitedQuickSort(int left, int right, IComparer<T> comparer, int depthLimit)
        {
            do
            {
                if (depthLimit == 0)
                {
                    Heapsort(left, right, comparer);
                    return;
                }

                int i = left;
                int j = right;

                int middle = i + ((j - i) >> 1);
                SwapIfGreater(comparer, i, middle);  // swap the low with the mid point
                SwapIfGreater(comparer, i, j);       // swap the low with the high
                SwapIfGreater(comparer, middle, j);  // swap the middle with the high

                T x = this[middle];
                do
                {
                    while (comparer.Compare(this[i], x) < 0) i++;
                    while (comparer.Compare(x, this[j]) < 0) j--;

                    if (i > j) break;
                    if (i < j)
                    {
                        T key = this[i];
                        this[i] = this[j];
                        this[j] = key;
                    }
                    i++;
                    j--;
                } while (i <= j);

                depthLimit--;

                if (j - left <= right - i)
                {
                    if (left < j) DepthLimitedQuickSort(left, j, comparer, depthLimit);
                    left = i;
                }
                else
                {
                    if (i < right) DepthLimitedQuickSort(i, right, comparer, depthLimit);
                    right = j;
                }
            } while (left < right);
        }

        public void Sort(IComparer<T> comparer = null)
        {
            Sort(0, m_Count, comparer);
        }

        public void Sort(int index, int length, IComparer<T> comparer = null)
        {
            ValidateNotDisposed();
            lock (m_SyncRoot)
            {
                try
                {
                    if (comparer == null)
                        comparer = Comparer<T>.Default;

                    DepthLimitedQuickSort(index, length + index - 1, comparer, 32);
                    m_Sorted = true;
                }
                catch (IndexOutOfRangeException)
                {
                    throw new Exception("Bad comparer");
                }
                catch (Exception innerException)
                {
                    throw new InvalidOperationException("Invalid operation. Comparer failed", innerException);
                }
            }
        }

        #endregion Sort

        #region Search

        public int BinarySearch<K>(int index, int length, K value, Func<T, K, int> comparer)
        {
            var lo = index;
            var hi = index + length - 1;

            int order, current;
            while (lo <= hi)
            {
                current = lo + ((hi - lo) >> 1);
                order = comparer(this[current], value);

                if (order == 0)
                    return current;

                if (order < 0)
                {
                    lo = current + 1;
                }
                else
                {
                    hi = current - 1;
                }
            }
            return ~lo;
        }

        #endregion Search

        #endregion Methods
    }

    #endregion RedisPartitionedList
}
