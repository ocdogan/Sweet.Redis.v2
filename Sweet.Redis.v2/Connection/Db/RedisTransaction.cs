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
using System.Linq;
using System.Threading;

namespace Sweet.Redis.v2
{
    internal class RedisTransaction : RedisBatch, IRedisTransaction
    {
        #region Field Members

        private RedisSynchronizedQueue<RedisParam> m_WatchQ;

        #endregion Field Members

        #region .Ctors

        public RedisTransaction(RedisAsyncClient asyncClient, int dbIndex, bool throwOnError = true)
            : base(asyncClient, dbIndex, throwOnError)
        { }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
            Interlocked.Exchange(ref m_WatchQ, null);
        }

        #endregion Destructors

        #region Methods

        protected override void OnFlush(IList<RedisBatchRequest> batchRequests, out bool success)
        {
            success = false;

            var watches = Interlocked.Exchange(ref m_WatchQ, null);
            if (watches != null && watches.Count > 0)
            {
                var watch = new RedisAsyncRequest(
                    new RedisCommand(DbIndex, RedisCommandList.Watch,
                                     RedisCommandType.SendAndReceive, watches.ToArray()), RedisCommandExpect.OK);

                Process(watch);
            }

            Process(batchRequests);
            try
            {
                var exec = new RedisAsyncRequest(new RedisCommand(DbIndex, RedisCommandList.Exec), RedisCommandExpect.Array);
                Process(exec);

                success = true;

                var response = (RedisArray)exec.Response;
                if (response == null)
                    Discard(batchRequests);
                else
                {
                    var items = response.Value;
                    if (items == null || response.Length <= 0)
                        Discard(batchRequests);
                    else
                    {
                        var requestCount = batchRequests.Count;

                        var count = Math.Min(requestCount, items.Count);
                        for (var i = 0; i < count; i++)
                        {
                            var batch = batchRequests[i];
                            var batchResult = batch.Result;

                            if (!ReferenceEquals(batchResult, null))
                            {
                                try
                                {
                                    var request = batch.Request;
                                    var expectation = ToExpectation(items[i], request.Expectation, request.ExpectedResult);

                                    batchResult.TrySetResult(expectation.RawData);
                                }
                                catch (Exception e)
                                {
                                    batchResult.TrySetException(e);
                                }
                            }
                        }

                        if (count < requestCount)
                        {
                            for (var i = count; i < requestCount; i++)
                            {
                                var result = batchRequests[i].Result;
                                if (!ReferenceEquals(result, null))
                                    result.TryCancel();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Discard(batchRequests, e);
                throw;
            }
        }

        private void Process(IList<RedisBatchRequest> batchRequests)
        {
            try
            {
                var requests = batchRequests.Select(br => new RedisAsyncRequest(br.Request.Command, RedisCommandExpect.OK)).ToList();

                // MULTI command
                requests.Insert(0, new RedisAsyncRequest(new RedisBatchCommand(new RedisCommand(DbIndex, RedisCommandList.Multi)), RedisCommandExpect.OK));
                Process(requests.ToArray());
            }
            catch (Exception e)
            {
                Discard(batchRequests, e);
                throw;
            }
        }

        public bool Commit()
        {
            return Flush();
        }

        public bool Discard()
        {
            return Rollback();
        }

        public bool Watch(RedisParam key, params RedisParam[] keys)
        {
            ValidateNotDisposed();

            if (m_State == (int)RedisBatchState.Executing)
                throw new RedisException("Transaction is being executed", RedisErrorCode.ExecutionError);

            var queue = m_WatchQ;
            if (queue == null)
                queue = (m_WatchQ = new RedisSynchronizedQueue<RedisParam>());

            if (!key.IsEmpty)
                queue.Enqueue(key);

            var length = keys.Length;
            if (length > 0)
            {
                foreach (var k in keys)
                {
                    if (!k.IsEmpty)
                        queue.Enqueue(k);
                }
            }
            return true;
        }

        public bool Unwatch()
        {
            ValidateNotDisposed();

            Interlocked.Exchange(ref m_WatchQ, null);
            return true;
        }

        #endregion Methods
    }
}
