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
using System.Collections;
using System.Collections.Generic;

namespace Sweet.Redis.v2
{
    internal class RedisHashesCommands : RedisCommandSet, IRedisCommandsHashes
    {
        #region .Ctors

        public RedisHashesCommands(RedisAsyncCommandExecuter executer)
            : base(executer)
        { }

        #endregion .Ctors

        #region Methods

        public RedisInteger HDel(RedisParam key, RedisParam field, params RedisParam[] fields)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            ValidateNotDisposed();

            if (fields.Length > 0)
            {
                var parameters = key
                                    .Join(field)
                                    .Join(fields);

                return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.HDel, parameters));
            }
            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.HDel, key, field));
        }

        public RedisBool HExists(RedisParam key, RedisParam field)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            return ExpectGreaterThanZero(new RedisCommand(DbIndex, RedisCommandList.HExists, key, field));
        }

        public RedisBytes HGet(RedisParam key, RedisParam field)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            return ExpectBulkStringBytes(new RedisCommand(DbIndex, RedisCommandList.HGet, key, field));
        }

        public RedisMultiBytes HGetAll(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectMultiDataBytes(new RedisCommand(DbIndex, RedisCommandList.HGetAll, key));
        }

        public RedisMultiString HGetAllString(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectMultiDataStrings(new RedisCommand(DbIndex, RedisCommandList.HGetAll, key));
        }

        public RedisResult<Dictionary<string, string>> HGetAllDictionary(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            var result = ExpectMultiDataStrings(new RedisCommand(DbIndex, RedisCommandList.HGetAll, key));
            if (!ReferenceEquals(result, null))
            {
                var value = result.Value;
                if (value != null)
                {
                    var length = value.Length;
                    if (length == 0)
                        return new RedisResult<Dictionary<string, string>>(new Dictionary<string, string>());

                    var d = new Dictionary<string, string>(length / 2);
                    for (var i = 0; i < length; i += 2)
                        d[value[i]] = value[i + 1];
                
                    return new RedisResult<Dictionary<string, string>>(d);
                }
            }
            return new RedisResult<Dictionary<string, string>>(null);
        }

        public RedisResult<Hashtable> HGetAllHashtable(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            var result = ExpectMultiDataStrings(new RedisCommand(DbIndex, RedisCommandList.HGetAll, key));
            if (!ReferenceEquals(result, null))
            {
                var value = result.Value;
                if (value != null)
                {
                    var length = value.Length;
                    if (length == 0)
                        return new RedisResult<Hashtable>(new Hashtable());

                    var h = new Hashtable(length / 2);
                    for (var i = 0; i < length; i += 2)
                        h[value[i]] = value[i + 1];

                    return new RedisResult<Hashtable>(h);
                }
            }
            return new RedisResult<Hashtable>(null);
        }

        public RedisString HGetString(RedisParam key, RedisParam field)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            return ExpectBulkString(new RedisCommand(DbIndex, RedisCommandList.HGet, key, field.ToBytes()));
        }

        public RedisInteger HIncrBy(RedisParam key, RedisParam field, int increment)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.HIncrBy, key, field, increment.ToBytes()));
        }

        public RedisInteger HIncrBy(RedisParam key, RedisParam field, long increment)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.HIncrBy, key, field, increment.ToBytes()));
        }

        public RedisDouble HIncrByFloat(RedisParam key, RedisParam field, double increment)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            return ExpectDouble(new RedisCommand(DbIndex, RedisCommandList.HIncrByFloat, key, field, increment.ToBytes()));
        }

        public RedisMultiBytes HKeys(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectMultiDataBytes(new RedisCommand(DbIndex, RedisCommandList.HKeys, key));
        }

        public RedisMultiString HKeyStrings(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectMultiDataStrings(new RedisCommand(DbIndex, RedisCommandList.HKeys, key));
        }

        public RedisInteger HLen(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.HLen, key));
        }

        public RedisMultiBytes HMGet(RedisParam key, RedisParam field, params RedisParam[] fields)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            ValidateNotDisposed();

            if (fields != null && fields.Length > 0)
            {
                var parameters = key
                                    .Join(field)
                                    .Join(fields);

                return ExpectMultiDataBytes(new RedisCommand(DbIndex, RedisCommandList.HMGet, parameters));
            }
            return ExpectMultiDataBytes(new RedisCommand(DbIndex, RedisCommandList.HMGet, key, field));
        }

        public RedisMultiString HMGetStrings(RedisParam key, RedisParam field, params RedisParam[] fields)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            ValidateNotDisposed();

            if (fields.Length > 0)
            {
                var parameters = key
                                    .Join(field)
                                    .Join(fields);

                return ExpectMultiDataStrings(new RedisCommand(DbIndex, RedisCommandList.HMGet, parameters));
            }
            return ExpectMultiDataStrings(new RedisCommand(DbIndex, RedisCommandList.HMGet, key, field.ToBytes()));
        }

        public RedisBool HMSet(RedisParam key, RedisParam field, RedisParam value, RedisParam[] fields = null, RedisParam[] values = null)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            ValidateNotDisposed();

            if (value.Length > RedisConstants.MaxValueLength)
                throw new ArgumentException("value is limited to 1GB", "value");

            if (fields.Length > 0)
            {
                if (values == null || values.Length != fields.Length)
                    throw new ArgumentException("Field and values length does not match", "field");

                var parameters = key
                                    .Join(field)
                                    .Join(value)
                                    .Join(fields.Merge(values));

                return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.HMSet, parameters));
            }
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.HMSet, key, field, value));
        }

        public RedisBool HMSet(RedisParam key, Hashtable values)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (values.IsEmpty())
                throw new ArgumentNullException("values");

            ValidateNotDisposed();

            var parameters = new RedisParam[1 + (2 * values.Count)];
            parameters[0] = key;

            var i = 1;
            foreach (DictionaryEntry de in values)
            {
                parameters[i++] = de.Key.ToBytes();
                parameters[i++] = de.Value.ToBytes();
            }
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.HMSet, parameters));
        }

        public RedisBool HMSet(RedisParam key, IDictionary<string, string> values)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (values == null || values.Count == 0)
                throw new ArgumentNullException("values");

            ValidateNotDisposed();

            var parameters = new RedisParam[1 + (2 * values.Count)];
            parameters[0] = key;

            var i = 1;
            foreach (var kvp in values)
            {
                parameters[i++] = kvp.Key.ToBytes();
                parameters[i++] = kvp.Value.ToBytes();
            }
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.HMSet, parameters));
        }

        public RedisBool HMSet(RedisParam key, IDictionary<RedisParam, RedisParam> values)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (values == null || values.Count == 0)
                throw new ArgumentNullException("values");

            ValidateNotDisposed();

            var parameters = new RedisParam[1 + (2 * values.Count)];
            parameters[0] = key;

            var i = 1;
            foreach (var kvp in values)
            {
                parameters[i++] = kvp.Key;
                parameters[i++] = kvp.Value;
            }
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.HMSet, parameters));
        }

        public RedisScanBytes HScan(RedisParam key, ulong cursor = 0uL, int count = 10, RedisParam? match = null)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            ValidateNotDisposed();

            var parameters = new RedisParam[] { key, cursor.ToBytes() };

            if (match.HasValue)
            {
                var value = match.Value;
                if (!value.IsEmpty)
                {
                    parameters = parameters.Join(RedisCommandList.Match);
                    parameters = parameters.Join(value);
                }
            }

            if (count > 0)
            {
                parameters = parameters.Join(RedisCommandList.Count);
                parameters = parameters.Join(count.ToBytes());
            }

            return RedisCommandUtils.ToScanBytes(ExpectArray(new RedisCommand(DbIndex, RedisCommandList.HScan, parameters)));
        }

        public RedisScanStrings HScanString(RedisParam key, ulong cursor = 0uL, int count = 10, RedisParam? match = null)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            ValidateNotDisposed();

            var parameters = new RedisParam[] { key, cursor.ToBytes() };

            if (match.HasValue)
            {
                var value = match.Value;
                if (!value.IsEmpty)
                {
                    parameters = parameters.Join(RedisCommandList.Match);
                    parameters = parameters.Join(value);
                }
            }

            if (count > 0)
            {
                parameters = parameters.Join(RedisCommandList.Count);
                parameters = parameters.Join(count.ToBytes());
            }

            return RedisCommandUtils.ToScanStrings(ExpectArray(new RedisCommand(DbIndex, RedisCommandList.HScan, parameters)));
        }

        public RedisBool HSet(RedisParam key, RedisParam field, RedisParam value)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            if (value.Length > RedisConstants.MaxValueLength)
                throw new ArgumentException("value is limited to 1GB", "value");

            return ExpectGreaterThanZero(new RedisCommand(DbIndex, RedisCommandList.HSet, key, field, value));
        }

        public RedisBool HSetNx(RedisParam key, RedisParam field, RedisParam value)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            ValidateNotDisposed();

            if (value.Length > RedisConstants.MaxValueLength)
                throw new ArgumentException("value is limited to 1GB", "value");

            return ExpectGreaterThanZero(new RedisCommand(DbIndex, RedisCommandList.HSetNx, key, field, value));
        }

        public RedisInteger HStrLen(RedisParam key, RedisParam field)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (field.IsEmpty)
                throw new ArgumentNullException("field");

            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.HStrLen, key, field));
        }

        public RedisMultiBytes HVals(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectMultiDataBytes(new RedisCommand(DbIndex, RedisCommandList.HVals, key));
        }

        public RedisMultiString HValStrings(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectMultiDataStrings(new RedisCommand(DbIndex, RedisCommandList.HVals, key));
        }

        #endregion Methods
    }
}
