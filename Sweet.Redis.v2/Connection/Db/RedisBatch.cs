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
    internal class RedisBatch : RedisDb, IRedisBatch
    {
        #region RedisBatchRequest

        protected class RedisBatchRequest
        {
            #region .Ctors

            public RedisBatchRequest(RedisAsyncRequest request, RedisResult result)
            {
                Result = result;
                Request = request;
            }

            #endregion .Ctors

            #region Properties

            public RedisAsyncRequest Request { get; private set; }

            public RedisResult Result { get; private set; }

            #endregion Properties
        }

        #endregion RedisBatchRequest

        #region Constants

        private const int DefaultCapacity = 16;

        #endregion Constants

        #region Field Members

        protected int m_State;
        protected List<RedisBatchRequest> m_Requests = new List<RedisBatchRequest>(DefaultCapacity);

        #endregion Field Members

        #region .Ctors

        public RedisBatch(RedisAsyncClient asyncClient, int dbIndex)
            : base(asyncClient, dbIndex)
        { }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
            Discard(Interlocked.Exchange(ref m_Requests, null));
        }

        #endregion Destructors

        #region Properties

        public RedisBatchState Status
        {
            get { return (RedisBatchState)m_State; }
        }

        #endregion Properties

        #region Methods

        protected override RedisResult Expect(RedisAsyncRequest request)
        {
            SetWaitingCommit();

            var result = CreateEmptyResult(request.Expectation);
            m_Requests.Add(new RedisBatchRequest(request, result));

            return result;
        }

        protected void SetWaitingCommit()
        {
            var currentState = (RedisBatchState)Interlocked.CompareExchange(ref m_State, (int)RedisBatchState.WaitingCommit,
                (int)RedisBatchState.Ready);

            if (currentState == RedisBatchState.Executing)
                throw new RedisFatalException("Can not expect any command while executing", RedisErrorCode.ExecutionError);
        }

        protected virtual bool Flush()
        {
            ValidateNotDisposed();

            if (Interlocked.CompareExchange(ref m_State, (int)RedisBatchState.Executing, (int)RedisBatchState.WaitingCommit) == 
                (int)RedisBatchState.WaitingCommit)
            {
                var success = false;
                try
                {
                    var requests = Interlocked.Exchange(ref m_Requests, new List<RedisBatchRequest>(DefaultCapacity));
                    if (requests == null)
                        return false;

                    var requestCount = requests.Count;
                    if (requestCount == 0)
                        return false;

                    try
                    {
                        OnFlush(requests, out success);

                        if (!success || m_State != (int)RedisBatchState.Executing)
                        {
                            success = false;
                            Discard(requests);
                            return false;
                        }

                        return true;
                    }
                    catch (Exception e)
                    {
                        SetException(requests, e);
                        throw;
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref m_State, success ?
                                    (int)RedisBatchState.Ready :
                                    (int)RedisBatchState.Failed);
                }
            }
            return false;
        }

        protected virtual void OnFlush(IList<RedisBatchRequest> batchRequests, out bool success)
        {
            success = true;
        }

        protected virtual bool Rollback()
        {
            ValidateNotDisposed();

            if (Interlocked.CompareExchange(ref m_State, (int)RedisBatchState.Executing, (int)RedisBatchState.WaitingCommit) == 
                (int)RedisBatchState.WaitingCommit)
            {
                var requests = Interlocked.Exchange(ref m_Requests, new List<RedisBatchRequest>(DefaultCapacity));
                try
                {
                    Cancel(requests);
                }
                catch (Exception e)
                {
                    Interlocked.Exchange(ref m_State, (int)RedisBatchState.Failed);
                    SetException(requests, e);
                }
                finally
                {
                    Interlocked.CompareExchange(ref m_State, (int)RedisBatchState.Ready, (int)RedisBatchState.WaitingCommit);
                }
                return true;
            }
            return false;
        }

        protected virtual void Discard(IList<RedisBatchRequest> requests, Exception exception = null)
        {
            if (requests != null)
            {
                var requestCount = requests.Count;
                if (requestCount > 0)
                {
                    var cancel = (exception == null);
                    for (var i = 0; i < requestCount; i++)
                    {
                        try
                        {
                            var request = requests[i];
                            if (request != null)
                            {
                                if (cancel)
                                    request.Result.TryCancel();
                                else
                                    request.Result.TrySetException(exception);
                            }
                        }
                        catch (Exception)
                        { }
                    }
                }
            }
        }

        protected static void Cancel(IList<RedisBatchRequest> requests, int start = 0)
        {
            if (requests != null)
            {
                var count = requests.Count;
                if (count > 0)
                {
                    for (var i = Math.Max(0, start); i < count; i++)
                    {
                        try
                        {
                            var request = requests[i];
                            if (request != null)
                                request.Result.TryCancel();
                        }
                        catch (Exception)
                        { }
                    }
                }
            }
        }
        protected static void SetException(IList<RedisBatchRequest> requests, Exception exception, int start = 0)
        {
            if (exception != null && requests != null)
            {
                var count = requests.Count;
                if (count > 0)
                {
                    for (var i = Math.Max(0, start); i < count; i++)
                    {
                        try
                        {
                            var request = requests[i];
                            if (request != null)
                                request.Result.TrySetException(exception);
                        }
                        catch (Exception)
                        { }
                    }
                }
            }
        }

        #endregion Methods
    }
}
