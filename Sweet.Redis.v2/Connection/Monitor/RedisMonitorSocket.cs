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
using System.Threading;

namespace Sweet.Redis.v2
{
    public class RedisMonitorSocket : RedisAsyncContinuousSocket
    {
        #region MessageParseState

        private enum MessageParseState
        {
            Undefined,
            Invalid,
            Time,
            ClientInfoStart,
            ClientInfo,
            MessageStart,
            Message
        }

        #endregion MessageParseState

        #region Field Members

        private Action<RedisMonitorMessage> m_Callback;

        #endregion Field Members

        #region .Ctors

        public RedisMonitorSocket(IPEndPoint endPoint,
            Action<RedisMonitorMessage> callback,
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

        protected static RedisMonitorMessage ToMonitorMessage(RedisString str)
        {
            if (!ReferenceEquals(str, null))
            {
                var data = str.Value;
                if (data != null)
                {
                    var dataLength = data.Length;
                    if (dataLength > 0)
                    {
                        var startPos = 0;
                        var partStartPos = -1;
                        var currentState = MessageParseState.Undefined;

                        var timeDotPos = -1;
                        var time = DateTime.MinValue;

                        var command = (string)null;
                        var msgData = (string)null;
                        var clientInfo = (string)null;

                        for (var i = 0; i < dataLength; i++)
                        {
                            if (i - startPos > RedisConstants.KByte)
                                break;

                            var ch = data[i];
                            switch (currentState)
                            {
                                case MessageParseState.Undefined:
                                    {
                                        if (char.IsWhiteSpace(ch))
                                            continue;
                                        startPos = i;
                                        partStartPos = i;
                                        currentState = MessageParseState.Time;
                                    }
                                    break;
                                case MessageParseState.Time:
                                    {
                                        if (char.IsNumber(ch))
                                        {
                                            if (i - partStartPos > 25)
                                                currentState = MessageParseState.Invalid;
                                            continue;
                                        }

                                        if (ch == '.')
                                            timeDotPos = i;
                                        else if (ch != ' ')
                                            currentState = MessageParseState.Invalid;
                                        else
                                        {
                                            var length = i - partStartPos;
                                            if (length < 1)
                                                currentState = MessageParseState.Invalid;
                                            else
                                            {
                                                var timeStr = data.Substring(partStartPos, length);

                                                if (timeDotPos < 0)
                                                    time = timeStr.ToInt().FromUnixTimeStamp();
                                                else
                                                {
                                                    time = ((timeDotPos == 0) ? "0" : timeStr.Substring(0, timeDotPos)).ToInt()
                                                        .FromUnixTimeStamp(timeStr.Substring(timeDotPos + 1, timeStr.Length - timeDotPos - 1).ToInt(0));
                                                }

                                                partStartPos = i + 1;
                                                currentState = MessageParseState.ClientInfoStart;
                                            }
                                        }
                                    }
                                    break;
                                case MessageParseState.ClientInfoStart:
                                    {
                                        if (char.IsWhiteSpace(ch))
                                        {
                                            if (i - partStartPos > 100)
                                                currentState = MessageParseState.Invalid;
                                            continue;
                                        }

                                        if (ch != '[')
                                            currentState = MessageParseState.Invalid;
                                        else
                                        {
                                            partStartPos = i + 1;
                                            currentState = MessageParseState.ClientInfo;
                                        }
                                    }
                                    break;
                                case MessageParseState.ClientInfo:
                                    {
                                        var length = i - partStartPos;
                                        if (ch == ']')
                                        {
                                            if (length < 0)
                                                currentState = MessageParseState.Invalid;
                                            else
                                            {
                                                clientInfo = data.Substring(partStartPos, length);

                                                partStartPos = i + 1;
                                                currentState = MessageParseState.MessageStart;
                                            }
                                            continue;
                                        }

                                        if (length > 50)
                                            currentState = MessageParseState.Invalid;
                                    }
                                    break;
                                case MessageParseState.MessageStart:
                                    {
                                        if (char.IsWhiteSpace(ch))
                                        {
                                            if (i - partStartPos > 100)
                                                currentState = MessageParseState.Invalid;
                                            continue;
                                        }

                                        partStartPos = i;
                                        currentState = MessageParseState.Message;
                                    }
                                    break;
                                case MessageParseState.Message:
                                    {
                                        var length = dataLength - partStartPos;
                                        if (length > 0)
                                        {
                                            command = data.Substring(partStartPos, length);

                                            var dataPos = command.IndexOf("\" ", 0, Math.Min(25, length), StringComparison.Ordinal);
                                            if (dataPos > -1)
                                            {
                                                msgData = command.Substring(dataPos + 2);
                                                command = command.Substring(0, dataPos + 1);
                                            }
                                        }
                                        return new RedisMonitorMessage(time, clientInfo, command, msgData);
                                    }
                                default:
                                    return RedisMonitorMessage.Empty;
                            }
                        }

                        if (currentState == MessageParseState.Message ||
                            currentState == MessageParseState.MessageStart)
                            return new RedisMonitorMessage(time, clientInfo, command, msgData);
                    }
                }
            }
            return RedisMonitorMessage.Empty;
        }

        protected override void DoAfterCompleteContext(RedisBufferContext context, RedisAsyncTask asyncTask)
        {
            if (context.ResultType == RedisRawObjectType.SimpleString)
            {
                var result = context.Result as RedisString;

                if (!ReferenceEquals(result, null) &&
                    result != RedisConstants.OK)
                {
                    var message = ToMonitorMessage(result);
                    if (message != null && !message.IsEmpty)
                    {
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
