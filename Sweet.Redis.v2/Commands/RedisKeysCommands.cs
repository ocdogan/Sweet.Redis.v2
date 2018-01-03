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
    internal class RedisKeysCommands : RedisCommandSet, IRedisKeysCommands
    {
        #region .Ctors

        public RedisKeysCommands(RedisAsyncCommandExecuter executer)
            : base(executer)
        { }

        #endregion .Ctors

        #region Methods

        public RedisInteger Del(RedisParam key, params RedisParam[] keys)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            ValidateNotDisposed();

            if (keys.Length > 0)
            {
                var parameters = key.Join(keys);
                return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Del, parameters));
            }
            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Del, keys));
        }

        public RedisBytes Dump(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectBulkStringBytes(new RedisCommand(DbIndex, RedisCommandList.Dump, key));
        }

        public RedisBool Exists(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectGreaterThanZero(new RedisCommand(DbIndex, RedisCommandList.Exists, key));
        }

        public RedisBool Expire(RedisParam key, int seconds)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectGreaterThanZero(new RedisCommand(DbIndex, RedisCommandList.Expire, key, seconds.ToBytes()));
        }

        public RedisBool ExpireAt(RedisParam key, int timestamp)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectGreaterThanZero(new RedisCommand(DbIndex, RedisCommandList.ExpireAt, key, timestamp.ToBytes()));
        }

        public RedisMultiString Keys(RedisParam pattern)
        {
            if (pattern.IsNull)
                throw new ArgumentNullException("pattern");

            return ExpectMultiDataStrings(new RedisCommand(DbIndex, RedisCommandList.Keys, pattern));
        }

        public RedisBool Migrate(RedisParam host, int port, RedisParam key, int destinationDb, long timeoutMs, bool copy = false,
            bool replace = false, params RedisParam[] keys)
        {
            if (host.IsNull)
                throw new ArgumentNullException("host");

            if (key.IsEmpty)
                throw new ArgumentNullException("key");

            ValidateNotDisposed();

            var parameters = host.ToBytes()
                                 .Join(port.ToBytes())
                                 .Join(!key.IsNull ? key.Data : RedisCommandList.EmptyString)
                                 .Join(destinationDb.ToBytes())
                                 .Join(timeoutMs.ToBytes());

            if (copy)
                parameters = parameters.Join(RedisCommandList.Copy);

            if (replace)
                parameters = parameters.Join(RedisCommandList.Replace);

            if (keys.Length > 0)
                parameters = parameters
                                 .Join(RedisCommandList.Keys)
                                 .Join(keys);

            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Migrate, parameters));
        }

        public RedisBool Move(RedisParam key, int db)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectGreaterThanZero(new RedisCommand(DbIndex, RedisCommandList.Move, key, db.ToBytes()));
        }

        public RedisInteger ObjectRefCount(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Object, RedisCommandList.RefCount, key));
        }

        public RedisBytes ObjectEncoding(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectBulkStringBytes(new RedisCommand(DbIndex, RedisCommandList.Object, RedisCommandList.Encoding, key));
        }

        public RedisString ObjectEncodingString(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectBulkString(new RedisCommand(DbIndex, RedisCommandList.Object, RedisCommandList.Encoding, key));
        }

        public RedisInteger ObjectIdleTime(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Object, RedisCommandList.IdleTime, key));
        }

        public RedisBool Persist(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectGreaterThanZero(new RedisCommand(DbIndex, RedisCommandList.Persist, key));
        }

        public RedisBool PExpire(RedisParam key, long milliseconds)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectGreaterThanZero(new RedisCommand(DbIndex, RedisCommandList.PExpire, key, milliseconds.ToBytes()));
        }

        public RedisBool PExpireAt(RedisParam key, long millisecondsTimestamp)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectGreaterThanZero(new RedisCommand(DbIndex, RedisCommandList.PExpireAt, key, millisecondsTimestamp.ToBytes()));
        }

        public RedisInteger PTtl(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.PTtl, key));
        }

        public RedisString RandomKey()
        {
            return ExpectBulkString(new RedisCommand(DbIndex, RedisCommandList.RandomKey));
        }

        public RedisBool Rename(RedisParam key, RedisParam newKey)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (newKey.IsNull)
                throw new ArgumentNullException("newKey");

            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Rename, key, newKey));
        }

        public RedisBool RenameNx(RedisParam key, RedisParam newKey)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            if (newKey.IsNull)
                throw new ArgumentNullException("newKey");

            return ExpectGreaterThanZero(new RedisCommand(DbIndex, RedisCommandList.RenameNx, key, newKey));
        }

        public RedisBool Restore(RedisParam key, long ttl, RedisParam value)
        {
            ValidateNotDisposed();
            ValidateKeyAndValue(key, value);

            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Rename, key, ttl.ToBytes(), value));
        }

        public RedisScanBytes Scan(ulong cursor = 0uL, int count = 10, RedisParam? match = null)
        {
            ValidateNotDisposed();

            var parameters = new RedisParam[] { cursor.ToBytes() };

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

            return RedisCommandUtils.ToScanBytes(ExpectArray(new RedisCommand(DbIndex, RedisCommandList.Scan, parameters)));
        }

        public RedisScanStrings ScanString(ulong cursor = 0uL, int count = 10, RedisParam? match = null)
        {
            ValidateNotDisposed();

            var parameters = new RedisParam[] { cursor.ToBytes() };

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

            return RedisCommandUtils.ToScanStrings(ExpectArray(new RedisCommand(DbIndex, RedisCommandList.Scan, parameters)));
        }

        public RedisMultiBytes Sort(RedisParam key, bool descending, bool alpha = false,
                      int start = -1, int end = -1, RedisParam? by = null, RedisParam? get = null)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            ValidateNotDisposed();

            var parameters = new RedisParam[] { key };

            if (descending)
                parameters = parameters.Join(RedisCommandList.Descending);

            if (alpha)
                parameters = parameters.Join(RedisCommandList.Alpha);

            if (start > -1 && end > -1)
                parameters = parameters
                    .Join(RedisCommandList.Limit)
                    .Join(start.ToBytes())
                    .Join(end.ToBytes());

            if (by.HasValue && !by.Value.IsEmpty)
                parameters = parameters
                    .Join(RedisCommandList.By)
                    .Join(by);

            if (get.HasValue && !get.Value.IsEmpty)
                parameters = parameters
                    .Join(RedisCommandList.Get)
                    .Join(get);

            return ExpectMultiDataBytes(new RedisCommand(DbIndex, RedisCommandList.Sort, parameters));
        }

        public RedisInteger Touch(RedisParam key, params RedisParam[] keys)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            ValidateNotDisposed();

            if (keys.Length > 0)
            {
                var parameters = key.Join(keys);
                return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Touch, parameters));
            }
            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Touch, keys));
        }

        public RedisInteger Ttl(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Ttl, key));
        }

        public RedisString Type(RedisParam key)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            return ExpectSimpleString(new RedisCommand(DbIndex, RedisCommandList.Type, key));
        }

        public RedisInteger Unlink(RedisParam key, params RedisParam[] keys)
        {
            if (key.IsNull)
                throw new ArgumentNullException("key");

            ValidateNotDisposed();

            if (keys.Length > 0)
            {
                var parameters = key.Join(keys);
                return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Unlink, parameters));
            }
            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Unlink, keys));
        }

        public RedisInteger Wait(int numberOfSlaves, int timeout)
        {
            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Wait, numberOfSlaves.ToBytes(), timeout.ToBytes()));
        }

        #endregion Methods
    }
}
