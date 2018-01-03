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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sweet.Redis.v2
{
    public class RedisCommand : IRedisCommand
    {
        #region Field Members

        private RedisRole? m_Role;

        private int m_DbIndex;
        private RedisParam m_Command;
        private RedisParam[] m_Arguments;
        private RedisCommandPriority m_Priority;
        private RedisCommandType m_CommandType;

        private int m_IsUpdater = -1;

        #endregion Field Members

        #region .Ctors

        public RedisCommand(RedisParam command, params RedisParam[] args)
        {
            if (command.IsEmpty)
                throw new ArgumentNullException("command");

            m_Arguments = args;
            m_Command = command;
        }

        protected internal RedisCommand(int dbIndex, RedisParam command, params RedisParam[] args)
        {
            if (command.IsEmpty)
                throw new ArgumentNullException("command");

            m_Arguments = args;
            m_Command = command;
            m_DbIndex = dbIndex;
        }

        protected internal RedisCommand(int dbIndex, RedisParam command,
            RedisCommandType commandType, params RedisParam[] args)
        {
            if (command.IsEmpty)
                throw new ArgumentNullException("command");

            m_Arguments = args;
            m_Command = command;
            m_CommandType = commandType;
            m_DbIndex = dbIndex;
        }

        #endregion .Ctors

        #region Properties

        public RedisParam[] Arguments { get { return m_Arguments; } }

        public RedisParam Command { get { return m_Command; } }

        public RedisCommandType CommandType
        {
            get { return m_CommandType; }
            internal set { m_CommandType = value; }
        }

        public int DbIndex { get { return m_DbIndex; } }

        public RedisCommandPriority Priority 
        { 
            get { return m_Priority; }
            set { m_Priority = value; }
        }

        internal bool IsHeartBeat { get; set; }

        public bool IsUpdater
        {
            get
            {
                Interlocked.CompareExchange(ref m_IsUpdater, 
                    m_Command.Data.IsUpdateCommand() ? 1 : 0, -1);
                return m_IsUpdater == 1;
            }
        }

        public RedisResult Result { get; set; }

        public RedisRole Role
        {
            get
            {
                if (!m_Role.HasValue)
                    m_Role = m_Command.Data.CommandRole();
                return m_Role.Value;
            }
        }

        #endregion Properties

        #region Methods

        #region Overriden Methods

        public override string ToString()
        {
            var sBuilder = new StringBuilder();

            sBuilder.Append("[DbIndex=");
            sBuilder.Append(DbIndex);
            sBuilder.Append(", Command=");
            sBuilder.Append(m_Command.Data != null ? m_Command.Data.ToUTF8String() : "(nil)");
            sBuilder.Append(", Arguments=");

            var args = Arguments;
            if (args == null)
                sBuilder.Append("(nil)]");
            else
            {
                var length = args.Length;
                if (length == 0)
                    sBuilder.Append("(empty)]");
                else
                {
                    var itemLen = 0;
                    for (var i = 0; i < length; i++)
                    {
                        var item = args[i];
                        if (i > 0)
                            sBuilder.Append(", ");

                        if (item.IsNull)
                        {
                            itemLen += 5;
                            sBuilder.Append("(nil)");
                        }
                        else if (item.IsEmpty)
                        {
                            itemLen += 7;
                            sBuilder.Append("(empty)");
                        }
                        else
                        {
                            var data = item.Data.ToUTF8String();

                            var len = 1000 - itemLen;
                            if (len >= data.Length)
                                sBuilder.Append(data);
                            else
                            {
                                if (len > 0)
                                    sBuilder.Append(data.Substring(len));
                                sBuilder.Append("...");
                            }

                            itemLen += data.Length;
                        }

                        if (itemLen >= 1000)
                            break;
                    }

                    sBuilder.Append(']');
                }
            }
            return sBuilder.ToString();
        }

        #endregion Overriden Methods

        #endregion Methods
    }
}
