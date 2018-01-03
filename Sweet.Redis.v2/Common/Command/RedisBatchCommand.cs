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

using System.Collections.Concurrent;
using System.Threading;

namespace Sweet.Redis.v2
{
    public class RedisBatchCommand : RedisCommand, IRedisBatchCommand
    {
        #region Field Members

        private ConcurrentBag<RedisCommand> m_Siblings = new ConcurrentBag<RedisCommand>();

        #endregion Field Members

        #region .Ctors

        public RedisBatchCommand(RedisParam command, params RedisParam[] args)
            : base(command, args)
        { }

        protected internal RedisBatchCommand(int dbIndex, RedisParam command, params RedisParam[] args)
            : base(dbIndex, command, args)
        { }

        protected internal RedisBatchCommand(int dbIndex, RedisParam command,
            RedisCommandType commandType, params RedisParam[] args)
            : base(dbIndex, command, commandType, args)
        { }

        protected internal RedisBatchCommand(RedisCommand command)
            : base(command.DbIndex, command.Command, command.CommandType, command.Arguments)
        { }

        #endregion .Ctors

        #region Properties

        public ConcurrentBag<RedisCommand> Siblings 
        { 
            get { return m_Siblings; } 
        }

        #endregion Properties
    }
}
