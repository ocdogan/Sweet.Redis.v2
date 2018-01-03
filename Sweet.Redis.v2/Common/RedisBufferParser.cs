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

namespace Sweet.Redis.v2
{
    public class RedisBufferParser
    {
        #region Constants

        private const int CRLFLength = 2;

        private const byte CR = (byte)'\r';
        private const byte LF = (byte)'\n';

        private const byte ArraySign = (byte)'*';
        private const byte ErrorSign = (byte)'-';
        private const byte NumberSign = (byte)':';
        private const byte SimpleStringSign = (byte)'+';
        private const byte BulkStringSign = (byte)'$';

        #endregion Constants

        #region Methods

        public void TryParse(RedisBufferContext context)
        {
            var buffer = context.Buffer;
            if (buffer != null)
            {
                var bufferLen = buffer.Length;

                var contextLen = context.Length;
                var contextOffset = context.Offset;

                if (bufferLen == 0 || contextLen <= 0 ||
                    contextOffset < 0 || contextOffset >= bufferLen)
                    return;

                context.Length = Math.Min(contextLen, bufferLen - contextOffset);

                var sign = buffer[contextOffset];
                switch (sign)
                {
                    case BulkStringSign:
                        {
                            context.Offset++;
                            context.Length--;

                            context.ResultType = RedisRawObjectType.BulkString;

                            byte[] bytes;
                            if (!TryParseLine(context, out bytes))
                            {
                                context.Offset--;
                                context.Length++;
                            }
                            else
                            {
                                long dataSize;
                                if (!bytes.TryParse(out dataSize))
                                    throw new RedisException("Invalid bulk string size", RedisErrorCode.CorruptResponse);

                                if (dataSize > RedisConstants.MinusOne)
                                {
                                    var iSize = checked((int)dataSize);

                                    if (context.Length >= iSize + CRLFLength)
                                    {
                                        var crPos = context.Offset + iSize;
                                        if (!(buffer[crPos] == CR && buffer[crPos + 1] == LF))
                                            throw new RedisException("Invalid bulk string data termination", RedisErrorCode.CorruptResponse);

                                        context.Completed = true;
                                        if (iSize == 0)
                                            context.Result = new RedisBytes(new byte[0]);
                                        else
                                        {
                                            var result = new byte[iSize];
                                            Array.Copy(buffer, context.Offset, result, 0, iSize);

                                            context.Result = new RedisBytes(result);
                                        }

                                        context.Offset += iSize + CRLFLength;
                                        context.Length -= iSize + CRLFLength;
                                    }
                                }
                                else
                                {
                                    if (dataSize < RedisConstants.MinusOne)
                                        throw new RedisException("Invalid bulk string size", RedisErrorCode.CorruptResponse);

                                    context.Completed = true;
                                    context.Result = new RedisBytes(null);
                                }
                            }
                        }
                        break;
                    case ArraySign:
                        {
                            context.Offset++;
                            context.Length--;

                            context.ResultType = RedisRawObjectType.Array;

                            byte[] bytes;
                            if (!TryParseLine(context, out bytes))
                            {
                                context.Offset--;
                                context.Length++;
                            }
                            else
                            {
                                long itemCount;
                                if (!bytes.TryParse(out itemCount))
                                    throw new RedisException("Invalid bulk string size", RedisErrorCode.CorruptResponse);

                                if (itemCount > RedisConstants.Zero)
                                {
                                    var iCount = checked((int)itemCount);

                                    var array = new RedisArray(iCount);
                                    var items = array.RawData as IList<RedisResult>;

                                    for (var i = 0; i < iCount; i++)
                                    {
                                        var innerContext = 
                                            new RedisBufferContext
                                            {
                                                Buffer = buffer,
                                                Length = context.Length,
                                                Offset = context.Offset,
                                            };

                                        TryParse(innerContext);
                                        if (!innerContext.Completed)
                                            return;

                                        context.Offset = innerContext.Offset;
                                        context.Length = innerContext.Length;

                                        items.Add(innerContext.Result);
                                    }

                                    array.TrySetCompleted();

                                    context.Result = array;
                                    context.Completed = true;
                                }
                                else if (itemCount == RedisConstants.Zero)
                                {
                                    context.Result = new RedisArray(new List<RedisResult>(), 0);
                                    context.Completed = true;
                                }
                                else
                                {
                                    context.Result = new RedisArray(null, -1);
                                    context.Completed = true;
                                }
                            }
                        }
                        break;
                    case SimpleStringSign:
                        {
                            context.Offset++;
                            context.Length--;

                            context.ResultType = RedisRawObjectType.SimpleString;

                            byte[] bytes;
                            if (!TryParseLine(context, out bytes))
                            {
                                context.Offset--;
                                context.Length++;
                            }
                            else
                            {
                                context.Result = new RedisString((bytes == null) ? null : RedisCommon.ToUTF8String(bytes));
                                context.Completed = true;
                            }
                        }
                        break;
                    case ErrorSign:
                        {
                            context.Offset++;
                            context.Length--;

                            context.ResultType = RedisRawObjectType.Error;

                            byte[] bytes;
                            if (!TryParseLine(context, out bytes))
                            {
                                context.Offset--;
                                context.Length++;
                            }
                            else
                            {
                                context.Result = new RedisError((bytes == null) ? null : RedisCommon.ToUTF8String(bytes));
                                context.Completed = true;
                            }
                        }
                        break;
                    case NumberSign:
                        {
                            context.Offset++;
                            context.Length--;

                            context.ResultType = RedisRawObjectType.Integer;

                            byte[] bytes;
                            if (!TryParseLine(context, out bytes))
                            {
                                context.Offset--;
                                context.Length++;
                            }
                            else
                            {
                                long number;
                                if (!bytes.TryParse(out number))
                                    throw new RedisException("Invalid integer value", RedisErrorCode.CorruptResponse);

                                context.Result = new RedisInteger(number);
                                context.Completed = true;
                            }
                        }
                        break;
                    default:
                        throw new RedisException("Undefined redis response type", RedisErrorCode.CorruptResponse);
                }
            }
        }

        private bool TryParseLine(RedisBufferContext context, out byte[] result)
        {
            result = null;
            var contextLength = context.Length;
            if (contextLength > 0)
            {
                var buffer = context.Buffer;

                var lineStart = context.Offset;
                var contextOffset = context.Offset;

                var crState = false;
                while (contextLength-- > 0)
                {
                    if (crState)
                    {
                        if (buffer[contextOffset++] != LF)
                            crState = false;
                        else
                        {
                            context.Offset = contextOffset;
                            context.Length = contextLength;

                            var dataLength = (contextOffset - CRLFLength) - lineStart;
                            if (dataLength > 0)
                            {
                                result = new byte[dataLength];
                                Array.Copy(buffer, lineStart, result, 0, dataLength);
                            }
                            else result = new byte[0];

                            return true;
                        }
                    }
                    else if (buffer[contextOffset++] == CR)
                        crState = true;
                }
            }
            return false;
        }

        #endregion Methods
    }
}
