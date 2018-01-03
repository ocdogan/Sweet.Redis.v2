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
using System.Threading;

namespace Sweet.Redis.v2
{
    internal class RedisCommandSet : RedisInternalDisposable
    {
        #region Field Members

        private RedisAsyncCommandExecuter m_Executer;
        private int m_DbIndex = RedisConstants.UninitializedDbIndex;

        #endregion Field Members

        #region .Ctors

        public RedisCommandSet(RedisAsyncCommandExecuter executer)
        {
            m_Executer = executer;
            m_DbIndex = m_Executer.DbIndex;
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
            Interlocked.Exchange(ref m_Executer, null);
        }

        #endregion Destructors

        #region Properties

        public int DbIndex
        {
            get { return m_DbIndex; }
        }

        public RedisAsyncCommandExecuter Executer
        {
            get { return m_Executer; }
        }

        #endregion Properties

        #region Methods

        #region Validation Methods

        protected static void ValidateKeyAndValue(string key, byte[] value, string keyName = null, string valueName = null)
        {
            if (key == null)
                throw new RedisFatalException(new ArgumentNullException(keyName.IsEmpty() ? "key" : keyName), RedisErrorCode.MissingParameter);

            if (value == null)
                throw new RedisFatalException(new ArgumentNullException(valueName.IsEmpty() ? "value" : valueName), RedisErrorCode.MissingParameter);

            if (value.Length > RedisConstants.MaxValueLength)
                throw new RedisFatalException(new ArgumentException("Redis values are limited to 1GB", valueName.IsEmpty() ? "value" : valueName), RedisErrorCode.MissingParameter);
        }

        protected static void ValidateKeyAndValue(RedisParam key, byte[] value, string keyName = null, string valueName = null)
        {
            if (key.IsEmpty)
                throw new RedisFatalException(new ArgumentNullException(keyName.IsEmpty() ? "key" : keyName), RedisErrorCode.MissingParameter);

            if (value == null)
                throw new RedisFatalException(new ArgumentNullException(valueName.IsEmpty() ? "value" : valueName), RedisErrorCode.MissingParameter);

            if (value.Length > RedisConstants.MaxValueLength)
                throw new RedisFatalException(new ArgumentException("Redis values are limited to 1GB", valueName.IsEmpty() ? "value" : valueName), RedisErrorCode.MissingParameter);
        }

        protected static void ValidateKeyAndValue(RedisParam key, RedisParam value, string keyName = null, string valueName = null)
        {
            if (key.IsEmpty)
                throw new RedisFatalException(new ArgumentNullException(keyName.IsEmpty() ? "key" : keyName), RedisErrorCode.MissingParameter);

            if (value.IsNull)
                throw new RedisFatalException(new ArgumentNullException(valueName.IsEmpty() ? "value" : valueName), RedisErrorCode.MissingParameter);

            if (value.Data.Length > RedisConstants.MaxValueLength)
                throw new RedisFatalException(new ArgumentException("Redis values are limited to 1GB", valueName.IsEmpty() ? "value" : valueName), RedisErrorCode.MissingParameter);
        }

        #endregion Validation Methods

        #region RedisAsyncClient Execution Methods

        protected RedisArray ExpectArray(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectArray(command);
        }

        protected RedisMultiBytes ExpectMultiDataBytes(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectMultiDataBytes(command);
        }

        protected RedisMultiString ExpectMultiDataStrings(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectMultiDataStrings(command);
        }

        protected RedisString ExpectBulkString(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectBulkString(command);
        }

        protected RedisBytes ExpectBulkStringBytes(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectBulkStringBytes(command);
        }

        protected RedisDouble ExpectDouble(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectDouble(command);
        }

        protected RedisBool ExpectGreaterThanZero(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectGreaterThanZero(command);
        }

        protected RedisInteger ExpectInteger(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectInteger(command);
        }

        protected RedisVoid ExpectNothing(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectNothing(command);
        }

        protected RedisNullableDouble ExpectNullableDouble(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectNullableDouble(command);
        }

        protected RedisNullableInteger ExpectNullableInteger(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectNullableInteger(command);
        }

        protected RedisBool ExpectOK(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectOK(command);
        }

        protected RedisBool ExpectOne(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectOne(command);
        }

        protected RedisString ExpectSimpleString(RedisCommand command)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectSimpleString(command);
        }

        protected RedisBool ExpectSimpleString(RedisCommand command, string expectedResult)
        {
            ValidateNotDisposed();
            return m_Executer.ExpectSimpleString(command, expectedResult);
        }

        #endregion RedisAsyncClient Execution Methods

        #endregion Methods
    }
}
