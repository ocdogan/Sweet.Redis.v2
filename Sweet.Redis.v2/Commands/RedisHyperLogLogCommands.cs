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

namespace Sweet.Redis.v2
{
    internal class RedisHyperLogLogCommands : RedisCommandSet, IRedisCommandsHyperLogLog
    {
        #region .Ctors

        public RedisHyperLogLogCommands(RedisAsyncCommandExecuter executer)
            : base(executer)
        { }

        #endregion .Ctors

        #region Methods

        public RedisBool PfAdd(RedisParam key, RedisParam element, params RedisParam[] elements)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (element.IsNull)
                throw new ArgumentNullException("element");

            ValidateNotDisposed();

            var length = elements.Length;
            if (length > 0)
            {
                var parameters = key.Join(element).Join(elements);
                return ExpectOne(new RedisCommand(DbIndex, RedisCommandList.PfAdd, parameters));
            }
            return ExpectOne(new RedisCommand(DbIndex, RedisCommandList.PfAdd, key, element));
        }

        public RedisInteger PfCount(RedisParam key, params RedisParam[] keys)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            ValidateNotDisposed();

            var length = keys.Length;
            if (length > 0)
            {
                var parameters = key.Join(keys);
                return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.PfAdd, parameters));
            }
            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.PfAdd, key));
        }

        public RedisBool PfMerge(RedisParam destKey, RedisParam sourceKey, params RedisParam[] sourceKeys)
        {
            if (destKey.IsNull)
                throw new ArgumentNullException("destKey");

            if (sourceKey.IsNull)
                throw new ArgumentNullException("sourceKey");

            ValidateNotDisposed();

            var length = sourceKeys.Length;
            if (length > 0)
            {
                var parameters = destKey.Join(sourceKey).Join(sourceKeys);
                return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Quit, parameters));
            }
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Quit, destKey, sourceKey));
        }

        #endregion Methods
    }
}
