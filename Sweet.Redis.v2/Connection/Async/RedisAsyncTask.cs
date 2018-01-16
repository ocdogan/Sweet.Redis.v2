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
using System.Threading;
using System.Threading.Tasks;

namespace Sweet.Redis.v2
{
    public class RedisAsyncTask
    {
        #region Static Members

        private static readonly RedisAsyncTaskStatus CompletedSomeHow =
            RedisAsyncTaskStatus.Completed | RedisAsyncTaskStatus.Canceled | RedisAsyncTaskStatus.Failed;

        #endregion Static Members

        #region Field Members

        private RedisCommand m_Command;

        private int m_Waiting;
        private bool m_IsAsync;
        private RedisResult m_Result;
        private Exception m_Exception;
        private TaskStatus? m_AsyncStatus;
        private RedisAsyncTaskStatus m_Status;
        private TaskCompletionSource<RedisResult> m_Completion;

        private readonly object m_WaitLock = new object();

        #endregion Field Members

        #region .Ctors

        public RedisAsyncTask(RedisCommand command, TaskCompletionSource<RedisResult> completion = null)
        {
            m_Command = command;
            m_Completion = completion;
            m_IsAsync = (completion != null);
        }

        #endregion .Ctors

        #region Properties

        public RedisCommand Command
        {
            get { return m_Command; }
        }

        public Exception Exception
        {
            get
            {
                if (m_IsAsync)
                    return m_Completion.Task.Exception;
                return m_Exception;
            }
        }

        public bool IsAsync
        {
            get { return m_IsAsync; }
        }

        public bool IsCanceled
        {
            get
            {
                if ((m_Status & RedisAsyncTaskStatus.Canceled) != RedisAsyncTaskStatus.Undefined)
                    return true;
                return IsTaskCompletionSourceCanceled();
            }
        }

        public bool IsCompleted
        {
            get
            {
                if ((m_Status & CompletedSomeHow) != RedisAsyncTaskStatus.Undefined)
                    return true;
                return IsTaskCompletionSourceCompleted();
            }
        }

        public bool IsFaulted
        {
            get
            {
                if ((m_Status & RedisAsyncTaskStatus.Failed) != RedisAsyncTaskStatus.Undefined)
                    return true;
                return IsTaskCompletionSourceFaulted();
            }
        }

        public RedisResult Result
        {
            get
            {
                if (m_IsAsync)
                    return m_Completion.Task.Result;
                return m_Result;
            }
        }

        public RedisAsyncTaskStatus Status
        {
            get { return m_Status; }
        }

        #endregion Properties

        #region Methods

        private bool IsTaskCompletionSourceCanceled()
        {
            if (m_IsAsync)
            {
                var status = TaskStatus.Created;
                if (!m_AsyncStatus.HasValue)
                {
                    var task = m_Completion.Task;
                    status = (m_AsyncStatus = (task != null ? task.Status : TaskStatus.Canceled)).Value;
                }

                return status == TaskStatus.Canceled;
            }
            return false;
        }

        private bool IsTaskCompletionSourceCompleted()
        {
            if (m_IsAsync)
            {
                var status = TaskStatus.Created;
                if (!m_AsyncStatus.HasValue)
                {
                    var task = m_Completion.Task;
                    status = (m_AsyncStatus = (task != null ? task.Status : TaskStatus.Canceled)).Value;
                }

                return (status == TaskStatus.RanToCompletion) ||
                    (status == TaskStatus.Canceled) ||
                    (status == TaskStatus.Faulted);
            }
            return false;
        }

        private bool IsTaskCompletionSourceFaulted()
        {
            if (m_IsAsync)
            {
                var status = TaskStatus.Created;
                if (!m_AsyncStatus.HasValue)
                {
                    var task = m_Completion.Task;
                    status = (m_AsyncStatus = (task != null ? task.Status : TaskStatus.Canceled)).Value;
                }

                return status == TaskStatus.Faulted;
            }
            return false;
        }

        public void TrySetCanceled()
        {
            try
            {
                if (m_IsAsync)
                {
                    var tcs = m_Completion;
                    var task = tcs.Task;

                    if (!IsTaskCompletionSourceCompleted())
                        tcs.TrySetCanceled();
                }
            }
            finally
            {
                m_Status |= RedisAsyncTaskStatus.Canceled;
                Pulse();
            }
        }

        public void TrySetException(Exception exception)
        {
            try
            {
                if (m_IsAsync)
                    m_Completion.TrySetException(exception);
                else m_Exception = exception;
            }
            finally
            {
                m_Status |= RedisAsyncTaskStatus.Failed;
                Pulse();
            }
        }

        public void TrySetCompleted(RedisResult data)
        {
            try
            {
                if (m_IsAsync)
                    m_Completion.TrySetResult(data);
                else m_Result = data;
            }
            finally
            {
                m_Status |= RedisAsyncTaskStatus.Completed;
                Pulse();
            }
        }

        public void Pulse()
        {
            if (m_Waiting != 0)
            {
                lock (m_WaitLock)
                {
                    Monitor.PulseAll(m_WaitLock);
                }
            }
        }

        public bool Wait(int millisecondsTimeout)
        {
            if (Interlocked.CompareExchange(ref m_Waiting, 1, 0) == 0)
            {
                try
                {
                    if (!IsCompleted)
                    {
                        lock (m_WaitLock)
                        {
                            Monitor.Wait(m_WaitLock, Math.Max(0, millisecondsTimeout));
                            return !IsCompleted;
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref m_Waiting, 0);
                }
                return false;
            }
            return !IsCompleted;
        }

        #endregion Methods
    }
}
