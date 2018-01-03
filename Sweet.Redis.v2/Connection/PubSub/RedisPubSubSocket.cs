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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Sweet.Redis.v2
{
    public class RedisPubSubSocket : RedisAsyncContinuousSocket
    {
        #region Field Members

        private Action<RedisPubSubMessage> m_Callback;

        #endregion Field Members

        #region .Ctors

        public RedisPubSubSocket(IPEndPoint endPoint,
            Action<RedisPubSubMessage> callback,
            int receiveTimeout = RedisConstants.DefaultReceiveTimeout,
            int sendTimeout = RedisConstants.DefaultSendTimeout,
            int capacity = 0,
            int bulkSendFactor = 0)
            : base(endPoint, receiveTimeout, sendTimeout, capacity, bulkSendFactor)
        {
            m_Callback = callback;
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            Interlocked.Exchange(ref m_Callback, null);
            base.OnDispose(disposing);
        }

        #endregion Destructors

        #region Methods

        protected static RedisPubSubMessage ToPubSubMessage(RedisArray response)
        {
            if (!ReferenceEquals(response, null))
            {
                var items = response.Value;
                if (items != null && items.Count >= 3)
                {
                    var index = 0;
                    var typeItem = items[index++] as RedisBytes;

                    if (!ReferenceEquals(typeItem, null))
                    {
                        var data = typeItem.Value;
                        if (data != null)
                        {
                            var typeStr = data.ToUTF8String();
                            if (typeStr != null)
                            {
                                var type = ToPubSubMessageType(typeStr);
                                if (type != RedisPubSubMessageType.Undefined)
                                {
                                    if (type == RedisPubSubMessageType.PMessage && items.Count < 4)
                                        return RedisPubSubMessage.Empty;

                                    var channelItem = items[index++] as RedisBytes;
                                    if (!ReferenceEquals(channelItem, null))
                                    {
                                        data = channelItem.Value;
                                        if (data != null)
                                        {
                                            var channel = data.ToUTF8String();
                                            if (!channel.IsEmpty())
                                            {
                                                var pattern = String.Empty;
                                                switch (type)
                                                {
                                                    case RedisPubSubMessageType.PMessage:
                                                        {
                                                            var patternItem = items[index++] as RedisBytes;
                                                            if (!ReferenceEquals(patternItem, null))
                                                            {
                                                                data = patternItem.Value;
                                                                if (data != null)
                                                                    pattern = data.ToUTF8String();
                                                            }

                                                            var tmp = channel;
                                                            channel = pattern;
                                                            pattern = tmp;
                                                        }
                                                        break;
                                                    case RedisPubSubMessageType.PSubscribe:
                                                    case RedisPubSubMessageType.PUnsubscribe:
                                                        {
                                                            pattern = channel;
                                                            channel = String.Empty;
                                                        }
                                                        break;
                                                    default:
                                                        break;
                                                }

                                                var dataItem = items[index++];
                                                if (dataItem != null)
                                                {
                                                    switch (dataItem.Type)
                                                    {
                                                        case RedisResultType.Bytes:
                                                        case RedisResultType.Integer:
                                                            return new RedisPubSubMessage(type, typeStr, channel, pattern, dataItem);
                                                        default:
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return RedisPubSubMessage.Empty;
        }

        private static RedisPubSubMessageType ToPubSubMessageType(string typeStr)
        {
            var type = RedisPubSubMessageType.Undefined;
            switch ((typeStr ?? String.Empty).ToLowerInvariant())
            {
                case "message":
                    type = RedisPubSubMessageType.Message;
                    break;
                case "pmessage":
                    type = RedisPubSubMessageType.PMessage;
                    break;
                case "subscribe":
                    type = RedisPubSubMessageType.Subscribe;
                    break;
                case "psubscribe":
                    type = RedisPubSubMessageType.PSubscribe;
                    break;
                case "unsubscribe":
                    type = RedisPubSubMessageType.Unsubscribe;
                    break;
                case "punsubscribe":
                    type = RedisPubSubMessageType.PUnsubscribe;
                    break;
                default:
                    break;
            }
            return type;
        }

        protected override void DoAfterCompleteContext(RedisBufferContext context, RedisAsyncTask asyncTask)
        {
            if (context.ResultType == RedisRawObjectType.Array)
            {
                var result = context.Result as RedisArray;

                if (!ReferenceEquals(result, null))
                {
                    var message = ToPubSubMessage(result);

                    if (message != null && !message.IsEmpty)
                    {
                        if (asyncTask != null &&
                            !(message.Type == RedisPubSubMessageType.Message ||
                             message.Type == RedisPubSubMessageType.PMessage))
                            asyncTask.TrySetCompleted(context.Result);

                        var callback = m_Callback;
                        if (callback != null)
                            callback(message);
                        return;
                    }
                }
            }

            if (asyncTask != null)
                asyncTask.TrySetCompleted(context.Result);
        }

        #endregion Methods
    }
}
