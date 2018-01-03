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
    internal class RedisPipeline : RedisBatch, IRedisPipeline
    {
        #region .Ctors

        public RedisPipeline(RedisAsyncClient asyncClient, int dbIndex, bool throwOnError = true)
            : base(asyncClient, dbIndex, throwOnError)
        { }

        #endregion .Ctors

        #region Methods

        protected override void OnFlush(IList<RedisBatchRequest> batchRequests, out bool success)
        {
            success = false;
            try
            {
                var requestCount = batchRequests.Count;

                var requests = batchRequests.Select(br => br.Request).ToArray();
                Process(requests);

                for (var i = 0; i < requestCount; i++)
                {
                    var batchResult = batchRequests[i].Result;
                    try
                    {
                        batchResult.TrySetResult(requests[i].Response.RawData);
                    }
                    catch (Exception e)
                    {
                        batchResult.TrySetException(e);
                        for (var j = i + 1; j < requestCount; j++)
                            batchRequests[i].Result.TryCancel();

                        Interlocked.Exchange(ref m_State, (int)RedisBatchState.Failed);
                        return;
                    }
                }

                success = true;
            }
            catch (Exception e)
            {
                Discard(batchRequests, e);
                throw;
            }
        }

        public bool Execute()
        {
            return Flush();
        }

        public bool Cancel()
        {
            return Rollback();
        }

        #endregion Methods
    }
}
