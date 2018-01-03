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
using System.Threading;

namespace Sweet.Redis.v2
{
    internal class RedisSentinelClient : RedisAsyncClient, IRedisSentinelClient, IRedisDisposable
    {
        #region Field Members

        private IRedisSentinelCommands m_Commands;
        private RedisAsyncCommandExecuter m_Executer;

        #endregion Field Members

        #region .Ctors

        public RedisSentinelClient(RedisConnectionSettings settings)
            : base(settings)
        {
            m_Executer = new RedisAsyncCommandExecuter(this, RedisConstants.UninitializedDbIndex, settings.ThrowOnError);
        }

        #endregion .Ctors

        #region Properties

        public IRedisSentinelCommands Commands
        {
            get
            {
                ValidateNotDisposed();
                if (m_Commands == null)
                    m_Commands = new RedisSentinelCommands(m_Executer);
                return m_Commands;
            }
        }

        public override RedisRole Role
        {
            get { return RedisRole.Sentinel; }
        }

        #endregion Properties
    }
}
