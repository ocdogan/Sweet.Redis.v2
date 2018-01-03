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

namespace Sweet.Redis.v2
{
    /*
    APPEND key value
    summary: Append a value to a key
    since: 2.0.0

    BITCOUNT key [start end]
    summary: Count set bits in a RedisParam
    since: 2.6.0

    BITFIELD key [GET type offset] [SET type offset value] [INCRBY type offset increment] [OVERFLOW WRAP|SAT|FAIL]
    summary: Perform arbitrary bitfield integer operations on strings
    since: 3.2.0

    BITOP operation destkey key [key ...]
    summary: Perform bitwise operations between strings
    since: 2.6.0

    BITPOS key bit [start] [end]
    summary: Find first bit set or clear in a RedisParam
    since: 2.8.7

    DECR key
    summary: Decrement the integer value of a key by one
    since: 1.0.0

    DECRBY key decrement
    summary: Decrement the integer value of a key by the given number
    since: 1.0.0

    GET key
    summary: Get the value of a key
    since: 1.0.0

    GETBIT key offset
    summary: Returns the bit value at offset in the RedisParam value stored at key
    since: 2.2.0

    GETRANGE key start end
    summary: Get a substring of the RedisParam stored at a key
    since: 2.4.0

    GETSET key value
    summary: Set the RedisParam value of a key and return its old value
    since: 1.0.0

    INCR key
    summary: Increment the integer value of a key by one
    since: 1.0.0

    INCRBY key increment
    summary: Increment the integer value of a key by the given amount
    since: 1.0.0

    INCRBYFLOAT key increment
    summary: Increment the float value of a key by the given amount
    since: 2.6.0

    MGET key [key ...]
    summary: Get the values of all the given keys
    since: 1.0.0

    MSET key value [key value ...]
    summary: Set multiple keys to multiple values
    since: 1.0.1

    MSETNX key value [key value ...]
    summary: Set multiple keys to multiple values, only if none of the keys exist
    since: 1.0.1

    PSETEX key milliseconds value
    summary: Set the value and expiration in milliseconds of a key
    since: 2.6.0

    SET key value [EX seconds] [PX milliseconds] [NX|XX]
    summary: Set the RedisParam value of a key
    since: 1.0.0

    SETBIT key offset value
    summary: Sets or clears the bit at offset in the RedisParam value stored at key
    since: 2.2.0

    SETEX key seconds value
    summary: Set the value and expiration of a key
    since: 2.0.0

    SETNX key value
    summary: Set the value of a key, only if the key does not exist
    since: 1.0.0

    SETRANGE key offset value
    summary: Overwrite part of a RedisParam at key starting at the specified offset
    since: 2.2.0

    STRLEN key
    summary: Get the length of the value stored in a key
    since: 2.2.0
     */
    public interface IRedisStringsCommands
    {
        RedisInteger Append(RedisParam key, RedisParam value);

        RedisInteger BitCount(RedisParam key);
        RedisInteger BitCount(RedisParam key, int start, int end);

        RedisInteger Decr(RedisParam key);
        RedisInteger DecrBy(RedisParam key, int count);
        RedisInteger DecrBy(RedisParam key, long count);

        RedisBytes Get(RedisParam key);
        RedisString GetString(RedisParam key);
        RedisInteger GetBit(RedisParam key, int offset);
        RedisBytes GetRange(RedisParam key, int start, int end);
        RedisString GetRangeString(RedisParam key, int start, int end);
        RedisBytes GetSet(RedisParam key, RedisParam value);
        RedisString GetSetString(RedisParam key, RedisParam value);

        RedisInteger Incr(RedisParam key);
        RedisInteger IncrBy(RedisParam key, int count);
        RedisInteger IncrBy(RedisParam key, long count);
        RedisDouble IncrByFloat(RedisParam key, double increment);

        RedisMultiBytes MGet(params RedisParam[] keys);
        RedisMultiString MGetString(params RedisParam[] keys);

        RedisBool MSet(RedisParam[] keys, RedisParam[] values);
        RedisBool MSetNx(RedisParam[] keys, RedisParam[] values);
        RedisBool PSetEx(RedisParam key, long milliseconds, RedisParam value);

        RedisBool Set(RedisParam key, RedisParam value);
        RedisBool Set(RedisParam key, RedisParam value, int expirySeconds, long expiryMilliseconds = RedisConstants.Zero);
        RedisInteger SetBit(RedisParam key, int offset, int value);
        RedisBool SetEx(RedisParam key, int seconds, RedisParam value);
        RedisBool SetNx(RedisParam key, RedisParam value);
        RedisInteger SetRange(RedisParam key, int offset, RedisParam value);

        RedisInteger StrLen(RedisParam key);
    }
}
