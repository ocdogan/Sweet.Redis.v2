﻿#region License
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

namespace Sweet.Redis.v2
{
    public interface IRedisPubSubChannel : IRedisDisposableBase
    {
        EndPoint EndPoint { get; }
        DateTime LastMessageSeenTime { get; }

        bool Ping();
        void Quit();
        void ResubscribeAll();
        void UnsubscribeAll();

        void PSubscribe(Action<RedisPubSubMessage> callback, RedisParam pattern, params RedisParam[] patterns);
        void PUnsubscribe(params RedisParam[] patterns);
        void Subscribe(Action<RedisPubSubMessage> callback, RedisParam channel, params RedisParam[] channels);
        void UnregisterPSubscription(RedisParam channel, Action<RedisPubSubMessage> callback);
        void UnregisterPSubscription(Action<RedisPubSubMessage> callback);
        void UnregisterSubscription(RedisParam channel, Action<RedisPubSubMessage> callback);
        void UnregisterSubscription(Action<RedisPubSubMessage> callback);
        void Unsubscribe(params string[] channels);
    }
}
