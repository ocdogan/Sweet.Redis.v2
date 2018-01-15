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
    internal class RedisAsyncCommandExecuter : RedisDisposable, IRedisIdentifiedObject
    {
        #region Field Members

        private int m_DbIndex;
        private RedisAsyncClient m_AsyncClient;
        private long m_Id = RedisIDGenerator<RedisAsyncCommandExecuter>.NextId();

        #endregion Field Members

        #region .Ctors

        public RedisAsyncCommandExecuter(RedisAsyncClient asyncClient, int dbIndex)
        {
            m_AsyncClient = asyncClient;
            m_DbIndex = Math.Min(Math.Max(dbIndex, RedisConstants.UninitializedDbIndex), RedisConstants.MaxDbIndex);
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
            Interlocked.Exchange(ref m_AsyncClient, null);
        }

        #endregion Destructors

        #region Properties

        protected internal RedisAsyncClient AsyncClient
        {
            get { return m_AsyncClient; }
        }

        public virtual int DbIndex
        {
            get { return m_DbIndex; }
        }

        public long Id
        {
            get { return m_Id; }
        }

        public virtual RedisRole Role
        {
            get { return RedisRole.Undefined; }
        }

        #endregion Properties

        #region Methods

        #region Sync Execution Methods

        public RedisArray ExpectArray(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisArray)Expect(new RedisAsyncRequest(command, RedisCommandExpect.Array));
        }

        public RedisMultiBytes ExpectMultiDataBytes(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisMultiBytes)Expect(new RedisAsyncRequest(command, RedisCommandExpect.MultiDataBytes));
        }

        public RedisMultiString ExpectMultiDataStrings(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisMultiString)Expect(new RedisAsyncRequest(command, RedisCommandExpect.MultiDataStrings));
        }

        public RedisString ExpectBulkString(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisString)Expect(new RedisAsyncRequest(command, RedisCommandExpect.BulkString));
        }

        public RedisBytes ExpectBulkStringBytes(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisBytes)Expect(new RedisAsyncRequest(command, RedisCommandExpect.BulkStringBytes));
        }

        public RedisDouble ExpectDouble(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisDouble)Expect(new RedisAsyncRequest(command, RedisCommandExpect.Double));
        }

        public RedisBool ExpectGreaterThanZero(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisBool)Expect(new RedisAsyncRequest(command, RedisCommandExpect.GreaterThanZero));
        }

        public RedisInteger ExpectInteger(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisInteger)Expect(new RedisAsyncRequest(command, RedisCommandExpect.Integer));
        }

        public RedisVoid ExpectNothing(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisVoid)Expect(new RedisAsyncRequest(command, RedisCommandExpect.Void));
        }

        public RedisNullableDouble ExpectNullableDouble(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisNullableDouble)Expect(new RedisAsyncRequest(command, RedisCommandExpect.NullableDouble));
        }

        public RedisNullableInteger ExpectNullableInteger(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisNullableInteger)Expect(new RedisAsyncRequest(command, RedisCommandExpect.NullableInteger));
        }

        public RedisBool ExpectOK(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisBool)Expect(new RedisAsyncRequest(command, RedisCommandExpect.OK));
        }

        public RedisBool ExpectOne(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisBool)Expect(new RedisAsyncRequest(command, RedisCommandExpect.One));
        }

        public RedisString ExpectSimpleString(RedisCommand command)
        {
            ValidateNotDisposed();
            return (RedisString)Expect(new RedisAsyncRequest(command, RedisCommandExpect.SimpleString));
        }

        public RedisBool ExpectSimpleString(RedisCommand command, string expectedResult)
        {
            ValidateNotDisposed();
            return (RedisBool)Expect(new RedisAsyncRequest(command, RedisCommandExpect.SimpleStringResult, expectedResult));
        }

        protected virtual RedisResult Expect(RedisAsyncRequest request)
        {
            Process(request);
            return request.Response;
        }

        protected void Process(RedisAsyncRequest[] requests)
        {
            if (requests != null)
            {
                var count = requests.Length;
                if (count > 0)
                {
                    var commands = requests.Select(r => r.Command).ToArray();
                    var results = m_AsyncClient.Expect(commands);

                    for (var i = 0; i < count; i++)
                    {
                        var request = requests[i];
                        if (request.Expectation == RedisCommandExpect.Void)
                            request.Response = new RedisVoid(0);
                        else
                            request.Response = ToExpectation(results[i],
                                                             request.Expectation, request.ExpectedResult);
                    }
                }
            }
        }
        
        protected void Process(RedisAsyncRequest request)
        {
            if (request.Expectation == RedisCommandExpect.Void)
            {
                var command = request.Command;
                if (command.CommandType != RedisCommandType.SendNotReceive)
                    command.CommandType = RedisCommandType.SendNotReceive;

                m_AsyncClient.Expect(command);
                request.Response = new RedisVoid(0);
                return;
            }

            request.Response = ToExpectation(m_AsyncClient.Expect(request.Command),
                                             request.Expectation, request.ExpectedResult);
        }

        protected RedisResult ToExpectation(RedisResult response, RedisCommandExpect expectation, string expectedResult = null)
        {
            switch (expectation)
            {
                case RedisCommandExpect.BulkStringBytes:
                    {
                        var result = response as RedisBytes;
                        if (ReferenceEquals(result, null))
                            throw new RedisException("Unexpected type", RedisErrorCode.CorruptResponse);
                        return result;
                    }
                case RedisCommandExpect.Array:
                    {
                        if (!(response is RedisArray))
                            throw new RedisException("Unexpected type", RedisErrorCode.CorruptResponse);
                        return response;
                    }
                case RedisCommandExpect.BulkString:
                    {
                        var result = response as RedisBytes;
                        if (ReferenceEquals(result, null))
                            throw new RedisException("Unexpected type", RedisErrorCode.CorruptResponse);

                        var bytes = result.Value;
                        if (bytes == null)
                            return new RedisString(null);
                        return new RedisString(bytes.ToUTF8String());
                    }
                case RedisCommandExpect.OK:
                    {
                        var result = response as RedisString;
                        if (ReferenceEquals(result, null))
                            throw new RedisFatalException("Unexpected value returned");
                        return new RedisBool(result.Value == RedisConstants.OK);
                    }
                case RedisCommandExpect.SimpleString:
                    {
                        if (!(response is RedisString))
                            throw new RedisFatalException("Unexpected value returned");
                        return response;
                    }
                case RedisCommandExpect.SimpleStringResult:
                    {
                        var result = response as RedisString;
                        if (ReferenceEquals(result, null))
                            throw new RedisFatalException("Unexpected value returned");
                        return new RedisBool(result.Value == expectedResult);
                    }
                case RedisCommandExpect.Integer:
                    {
                        if (!(response is RedisInteger))
                            throw new RedisException("Not an integer result", RedisErrorCode.CorruptResponse);
                        return response;
                    }
                case RedisCommandExpect.MultiDataBytes:
                    {
                        var array = response as RedisArray;
                        if (ReferenceEquals(array, null))
                            return new RedisMultiBytes(null);
                        return new RedisMultiBytes(array.ToMultiBytes());
                    }
                case RedisCommandExpect.MultiDataStrings:
                    {
                        var array = response as RedisArray;
                        if (ReferenceEquals(array, null))
                            return new RedisMultiString(null);
                        return new RedisMultiString(array.ToMultiString());
                    }
                case RedisCommandExpect.Double:
                    {
                        if (!ReferenceEquals(response, null))
                        {
                            if (response is RedisInteger)
                                return new RedisDouble(((RedisInteger)response).Value);

                            if (response is RedisString)
                            {
                                var str = ((RedisString)response).Value;
                                if (!String.IsNullOrEmpty(str))
                                {
                                    double d;
                                    if (double.TryParse(str, out d))
                                        return new RedisDouble(d);
                                }
                            }
                            else if (response is RedisBytes)
                            {
                                var bytes = ((RedisBytes)response).Value;
                                if (bytes != null && bytes.Length > 0)
                                {
                                    double d;
                                    if (double.TryParse(bytes.ToUTF8String(), out d))
                                        return new RedisDouble(d);
                                }
                            }
                        }
                        throw new RedisException("Not a double result", RedisErrorCode.CorruptResponse);
                    }
                case RedisCommandExpect.NullableInteger:
                    {
                        if (response is RedisInteger)
                            return new RedisNullableInteger(((RedisInteger)response).Value);

                        if (response is RedisString)
                        {
                            var str = ((RedisString)response).Value;
                            if (String.IsNullOrEmpty(str))
                                return new RedisNullableInteger(null);

                            if (str == RedisConstants.NilStr)
                                return new RedisNullableInteger(null);

                            long l;
                            if (str.TryParse(out l))
                                return new RedisNullableInteger(l);
                        }
                        else if (response is RedisBytes)
                        {
                            var bytes = ((RedisBytes)response).Value;
                            if (bytes == null || bytes.Length == 0)
                                return new RedisNullableInteger(null);

                            if (bytes.EqualTo(RedisConstants.Nil))
                                return new RedisNullableInteger(null);

                            long l;
                            if (bytes.TryParse(out l))
                                return new RedisNullableInteger(l);
                        }

                        throw new RedisException("Not an integer result", RedisErrorCode.CorruptResponse);
                    }
                case RedisCommandExpect.NullableDouble:
                    {
                        if (ReferenceEquals(response, null))
                            return new RedisNullableDouble(null);

                        if (response is RedisInteger)
                            return new RedisNullableDouble(((RedisInteger)response).Value);

                        if (response is RedisString)
                        {
                            var str = ((RedisString)response).Value;
                            if (String.IsNullOrEmpty(str))
                                return new RedisNullableDouble(null);

                            if (str == RedisConstants.NilStr)
                                return new RedisNullableDouble(null);

                            double d;
                            if (double.TryParse(str, out d))
                                return new RedisNullableDouble(d);
                        }
                        else if (response is RedisBytes)
                        {
                            var bytes = ((RedisBytes)response).Value;
                            if (bytes == null || bytes.Length == 0)
                                return new RedisNullableDouble(null);

                            if (bytes.EqualTo(RedisConstants.Nil))
                                return new RedisNullableDouble(null);

                            double d;
                            if (double.TryParse(bytes.ToUTF8String(), out d))
                                return new RedisNullableDouble(d);
                        }

                        throw new RedisException("Not a double result", RedisErrorCode.CorruptResponse);
                    }
                case RedisCommandExpect.GreaterThanZero:
                    {
                        var result = response as RedisInteger;
                        if (ReferenceEquals(result, null))
                            throw new RedisException("Not an integer result", RedisErrorCode.CorruptResponse);
                        return new RedisBool(result.Value > RedisConstants.Zero);
                    }
                case RedisCommandExpect.One:
                    {
                        var result = response as RedisInteger;
                        if (ReferenceEquals(result, null))
                            throw new RedisFatalException("Unexpected value returned");
                        return new RedisBool(result.Value == RedisConstants.One);
                    }
                case RedisCommandExpect.Void:
                    return new RedisVoid(0);
            }

            throw new RedisException("Unexpected type", RedisErrorCode.CorruptResponse);
        }

        protected RedisResult CreateEmptyResult(RedisCommandExpect expect)
        {
            switch (expect)
            {
                case RedisCommandExpect.Array:
                    return new RedisArray();
                case RedisCommandExpect.BulkString:
                case RedisCommandExpect.SimpleString:
                    return new RedisString();
                case RedisCommandExpect.BulkStringBytes:
                    return new RedisBytes();
                case RedisCommandExpect.OK:
                case RedisCommandExpect.GreaterThanZero:
                case RedisCommandExpect.One:
                    return new RedisBool();
                case RedisCommandExpect.MultiDataBytes:
                    return new RedisMultiBytes();
                case RedisCommandExpect.MultiDataStrings:
                    return new RedisMultiString();
                case RedisCommandExpect.Integer:
                    return new RedisInteger();
                case RedisCommandExpect.Double:
                    return new RedisDouble();
                case RedisCommandExpect.NullableInteger:
                    return new RedisNullableInteger();
                case RedisCommandExpect.NullableDouble:
                    return new RedisNullableDouble();
                case RedisCommandExpect.Void:
                    return new RedisVoid();
            }

            throw new RedisException("Unexpected type", RedisErrorCode.CorruptResponse);
        }

        #endregion Sync Execution Methods

        #endregion Methods
    }
}
