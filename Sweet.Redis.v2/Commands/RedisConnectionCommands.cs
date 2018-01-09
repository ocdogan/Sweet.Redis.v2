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
    internal class RedisConnectionCommands : RedisCommandSet, IRedisCommandsConnection
    {
        #region .Ctors

        public RedisConnectionCommands(RedisAsyncCommandExecuter executer)
            : base(executer)
        { }

        #endregion .Ctors

        #region Methods

        public RedisBool Auth(RedisParam password)
        {
            if (password.IsEmpty)
                throw new ArgumentNullException("password");

            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Auth, password));
        }

        public RedisString Echo(RedisParam msg)
        {
            if (msg.IsNull)
                throw new ArgumentNullException("msg");

            return ExpectBulkString(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Echo, msg));
        }

        public RedisBool Ping()
        {
            return ExpectSimpleString(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Ping), RedisConstants.PONG);
        }

        public RedisString Ping(RedisParam msg)
        {
            if (msg.IsEmpty)
                return ExpectSimpleString(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Ping));
            return ExpectBulkString(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Ping, msg));
        }

        public RedisBool Quit()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Quit));
        }

        #endregion Methods
    }
}
