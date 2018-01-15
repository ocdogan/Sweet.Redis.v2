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
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Sweet.Redis.v2
{
    public static class RedisCommon
    {
        #region Static Members

        public static readonly UTF8Encoding UTF8 = new UTF8Encoding(true);

        private static bool? s_IsWinPlatform;
        private static bool? s_IsLinuxPlatform;

        private static readonly DateTime UnixBaseTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        private static readonly byte Minus = (byte)'-';
        private static readonly byte ZeroBase = (byte)'0';

        private static readonly byte[] ShortMinValue = UTF8.GetBytes("-32768");
        private static readonly byte[] ShortMaxValue = UTF8.GetBytes("32767");

        private static readonly byte[] IntMinValue = UTF8.GetBytes("-2147483648");
        private static readonly byte[] IntMaxValue = UTF8.GetBytes("2147483647");

        private static readonly byte[] LongMinValue = UTF8.GetBytes("-9223372036854775808");
        private static readonly byte[] LongMaxValue = UTF8.GetBytes("9223372036854775807");

        private static readonly byte[] ULongMaxValue = UTF8.GetBytes("18446744073709551615");

        #endregion Static Members

        #region Constants

        private const byte CharMinus = (byte)'-';
        private const byte CharZero = (byte)'0';
        private const byte CharNine = (byte)'9';

        private const int IntStringLen = 11;
        private const int LongStringLen = 20;
        private const int ShortStringLen = 6;

        #endregion Constants

        #region Properties

        public static bool IsLinuxPlatform
        {
            get
            {
                if (!s_IsLinuxPlatform.HasValue)
                {
                    int p = (int)Environment.OSVersion.Platform;
                    s_IsLinuxPlatform = (p == 4) || (p == 6) || (p == 128);
                }
                return s_IsLinuxPlatform.Value;
            }
        }

        public static bool IsWinPlatform
        {
            get
            {
                if (!s_IsWinPlatform.HasValue)
                {
                    var pid = Environment.OSVersion.Platform;
                    switch (pid)
                    {
                        case PlatformID.Win32NT:
                        case PlatformID.Win32S:
                        case PlatformID.Win32Windows:
                        case PlatformID.WinCE:
                            s_IsWinPlatform = true;
                            break;
                        default:
                            s_IsWinPlatform = false;
                            break;
                    }
                }
                return s_IsWinPlatform.Value;
            }
        }

        public static bool OSSupportsIPv4
        {
            get
            {
                return Socket.SupportsIPv4;
            }
        }

        #endregion Properties

        #region Methods

        #region Parse

        #region TryParse Int

        internal static bool TryParse(this string s, out int result)
        {
            result = 0;

            long value;
            if (TryParse(s, out value) &&
                (value <= int.MaxValue && value >= int.MinValue))
            {
                result = unchecked((int)value);
                return true;
            }
            return false;
        }

        internal static bool TryParse(this string s, int start, int length, out int result)
        {
            result = 0;

            long value;
            if (TryParse(s, start, length, out value) &&
                (value <= int.MaxValue && value >= int.MinValue))
            {
                result = unchecked((int)value);
                return true;
            }
            return false;
        }

        internal static bool TryParse(this byte[] bytes, out int result)
        {
            result = 0;

            long value;
            if (TryParse(bytes, out value) &&
                (value <= int.MaxValue && value >= int.MinValue))
            {
                result = unchecked((int)value);
                return true;
            }
            return false;
        }

        internal static bool TryParse(this byte[] bytes, int start, int length, out int result)
        {
            result = 0;

            long value;
            if (TryParse(bytes, start, length, out value) &&
                (value <= int.MaxValue && value >= int.MinValue))
            {
                result = unchecked((int)value);
                return true;
            }
            return false;
        }

        #endregion TryParse Int

        #region TryParse Long

        internal static bool TryParse(this string s, out long result)
        {
            result = 0L;
            if (s != null)
            {
                var end = s.Length;
                if (end > 0)
                {
                    var value = 0L;

                    var sign = 1;
                    var index = 0;

                    if (s[0] == CharMinus)
                    {
                        if (end == 1)
                            return false;
                        index++;
                        sign = -1;
                    }

                    char ch;
                    while (index < end)
                    {
                        ch = s[index++];
                        if ((ch < CharZero) || (ch > CharNine))
                            return false;

                        value = (10 * value) + (ch - CharZero);
                    }

                    result = sign * value;
                    return true;
                }
            }
            return false;
        }

        internal static bool TryParse(this string s, int start, int length, out long result)
        {
            result = 0L;
            if ((s != null) && (start > -1) && (length > 0))
            {
                var end = start + length;
                if (end <= s.Length)
                {
                    var value = 0L;

                    var sign = 1;
                    if (s[start] == CharMinus)
                    {
                        if (length == 1)
                            return false;
                        start++;
                        sign = -1;
                    }

                    char ch;
                    while (start < end)
                    {
                        ch = s[start++];
                        if ((ch < CharZero) || (ch > CharNine))
                            return false;

                        value = (10 * value) + (ch - CharZero);
                    }

                    result = sign * value;
                    return true;
                }
            }
            return false;
        }

        internal static bool TryParse(this byte[] bytes, out long result)
        {
            result = 0L;
            if (bytes != null)
            {
                var end = bytes.Length;
                if (end > 0)
                {
                    var value = 0L;

                    var sign = 1;
                    var index = 0;

                    if (bytes[0] == CharMinus)
                    {
                        if (end == 1)
                            return false;
                        index++;
                        sign = -1;
                    }

                    byte ch;
                    while (index < end)
                    {
                        ch = bytes[index++];
                        if ((ch < CharZero) || (ch > CharNine))
                            return false;

                        value = (10 * value) + (ch - CharZero);
                    }

                    result = sign * value;
                    return true;
                }
            }
            return false;
        }

        internal static bool TryParse(this byte[] bytes, int start, int length, out long result)
        {
            result = 0L;
            if ((bytes != null) && (start > -1) && (length > 0))
            {
                var end = start + length;
                if (end <= bytes.Length)
                {
                    var value = 0L;

                    var sign = 1;
                    if (bytes[start] == CharMinus)
                    {
                        if (length == 1)
                            return false;
                        start++;
                        sign = -1;
                    }

                    byte ch;
                    while (start < end)
                    {
                        ch = bytes[start++];
                        if ((ch < CharZero) || (ch > CharNine))
                            return false;

                        value = (10 * value) + (ch - CharZero);
                    }

                    result = sign * value;
                    return true;
                }
            }
            return false;
        }

        #endregion TryParse Long

        #endregion Parse

        #region General

        internal static byte[][] ToMultiBytes(this RedisArray array)
        {
            var items = array.Value;
            if (items == null)
                return null;

            var count = items.Count;
            if (count == 0)
                return new byte[0][];

            var list = new List<byte[]>(count);
            for (var i = 0; i < count; i++)
            {
                var child = items[i];
                if (child == null)
                    list.Add(null);
                else
                {
                    switch (child.Type)
                    {
                        case RedisResultType.Bytes:
                            list.Add(((RedisBytes)child).Value);
                            break;
                        case RedisResultType.String:
                            list.Add(((RedisString)child).Value.ToBytes());
                            break;
                        default:
                            throw new RedisFatalException("Unexpected multi-string item");
                    }
                }
            }
            return list.ToArray();
        }

        internal static string[] ToMultiString(this RedisArray array)
        {
            var items = array.Value;
            if (items == null)
                return null;

            var count = items.Count;
            if (count == 0)
                return new string[0];

            var list = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                var child = items[i];
                if (child == null)
                    list.Add(null);
                else
                {
                    switch (child.Type)
                    {
                        case RedisResultType.Bytes:
                            list.Add(((RedisBytes)child).Value.ToUTF8String());
                            break;
                        case RedisResultType.String:
                            list.Add(((RedisString)child).Value);
                            break;
                        default:
                            throw new RedisFatalException("Unexpected multi-string item");
                    }
                }
            }
            return list.ToArray();
        }

        internal static RedisEndPoint ToRedisEndPoint(this string host, int port = RedisConstants.DefaultPort)
        {
            host = (host ?? String.Empty).Trim();
            if (!host.IsEmpty())
            {
                port = port < 1 ? RedisConstants.DefaultPort : port;

                var colonPos = host.LastIndexOf(':');
                if (colonPos == -1 || colonPos == host.Length - 1)
                    return new RedisEndPoint(host, port);

                var isIP4 = host.IndexOf('.') > -1;
                if (!isIP4)
                {
                    var colonCount = host.Count((ch) => ch == ':');

                    var isIP6 = (colonCount > 1);
                    if (isIP6 && colonCount == 2)
                        colonPos = -1;
                }

                var name = (host.Substring(0, colonPos) ?? String.Empty).TrimEnd();
                if (name.IsEmpty())
                    name = RedisConstants.LocalHost;

                if (colonPos == host.Length - 1)
                    return new RedisEndPoint(name, port);

                var portStr = (host.Substring(colonPos + 1) ?? String.Empty).TrimStart();
                if (portStr.IsEmpty())
                    return new RedisEndPoint(name, port);

                var portInt = int.Parse(portStr);
                port = portInt < 1 ? port : portInt;

                return new RedisEndPoint(name, port);
            }
            return RedisEndPoint.Empty;
        }

        internal static RedisRole ToRedisRole(this string roleStr)
        {
            if (!roleStr.IsEmpty())
            {
                roleStr = roleStr.ToLowerInvariant();
                switch (roleStr)
                {
                    case "master":
                        return RedisRole.Master;
                    case "slave":
                        return RedisRole.Slave;
                    case "sentinel":
                        return RedisRole.Sentinel;
                    default:
                        break;
                }
            }
            return RedisRole.Undefined;
        }

        internal static SocketError GetSocketErrorCode(this Exception exception)
        {
            while (exception != null)
            {
                var redisError = exception as RedisException;
                if (redisError == null)
                {
                    if (exception is SocketException)
                        return ((SocketException)exception).SocketErrorCode;
                }
                else
                {
                    var errorCode = redisError.ErrorCode;
                    switch (errorCode)
                    {
                        case RedisErrorCode.ConnectionError:
                        case RedisErrorCode.CorruptData:
                        case RedisErrorCode.CorruptResponse:
                        case RedisErrorCode.SocketError:
                            return (SocketError)errorCode;
                    }
                }
                exception = exception.InnerException;
            }
            return SocketError.Success;
        }

        internal static bool IsSocketError(this Exception exception)
        {
            while (exception != null)
            {
                var redisError = exception as RedisException;
                if (redisError == null)
                {
                    if (exception is SocketException)
                        return true;
                }
                else
                {
                    switch (redisError.ErrorCode)
                    {
                        case RedisErrorCode.ConnectionError:
                        case RedisErrorCode.CorruptData:
                        case RedisErrorCode.CorruptResponse:
                        case RedisErrorCode.SocketError:
                            return true;
                    }
                }
                exception = exception.InnerException;
            }
            return false;
        }

        internal static bool IsEmpty(this string obj)
        {
            return (obj == null || obj.Length == 0);
        }

        internal static bool IsEmpty(this Array obj)
        {
            return (obj == null || obj.Length == 0);
        }

        internal static bool IsEmpty(this ICollection obj)
        {
            return (obj == null || obj.Count == 0);
        }

        internal static bool IsEmpty(this RedisEndPoint endPoint)
        {
            return (ReferenceEquals(endPoint, null) || endPoint.IsEmpty);
        }

        internal static bool IsAlive(this IRedisDisposableBase obj)
        {
            return (obj != null) && !obj.Disposed;
        }

        internal static byte[] Clone(this byte[] x, int offset = 0, int length = -1)
        {
            if (x != null)
            {
                var xLength = x.Length;
                if (offset < xLength)
                {
                    if (xLength == 0)
                        return new byte[0];

                    if (offset < 0) offset = 0;

                    if (length < 0) length = xLength;

                    length = Math.Min(length, xLength - offset);
                    if (length > -1)
                    {
                        var result = new byte[length];
                        if (length == 0)
                            return result;

                        Array.Copy(x, offset, result, 0, length);
                        return result;
                    }
                }
            }
            return null;
        }

        internal static string ToUTF8String(this byte[] x, int offset = 0, int length = -1)
        {
            if (x != null)
            {
                var xLength = x.Length;
                if (offset < xLength)
                {
                    if (xLength == 0)
                        return String.Empty;

                    if (offset < 0) offset = 0;

                    if (length < 0) length = xLength;

                    length = Math.Min(length, xLength - offset);
                    if (length > -1)
                    {
                        if (length == 0)
                            return String.Empty;
                        return UTF8.GetString(x, offset, length);
                    }
                }
            }
            return null;
        }

        internal static bool EqualTo(this byte[] x, byte[] y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return ReferenceEquals(y, null);

            if (ReferenceEquals(y, null))
                return false;

            var l1 = x.Length;
            var l2 = y.Length;

            if (l1 != l2)
                return false;

            for (var i = 0; i < l1; i++)
                if (x[i] != y[i])
                    return false;

            return true;
        }

        internal static bool EqualTo(this byte[] data, object obj)
        {
            if (ReferenceEquals(data, null))
                return ReferenceEquals(obj, null);

            if (ReferenceEquals(obj, null))
                return false;

            if (ReferenceEquals(data, obj))
                return true;

            if (obj is byte[])
                return data.EqualTo((byte[])obj);

            if (obj is RedisParam)
                return data.EqualTo(((RedisParam)obj).Data);

            if (obj is string)
                return data.EqualTo(UTF8.GetBytes((string)obj));

            if (obj is long)
                return data.EqualTo(BitConverter.GetBytes((long)obj));

            if (obj is int)
                return data.EqualTo(BitConverter.GetBytes((int)obj));

            if (obj is short)
                return data.EqualTo(BitConverter.GetBytes((short)obj));

            if (obj is double)
                return data.EqualTo(BitConverter.GetBytes((double)obj));

            if (obj is byte)
                return data.EqualTo(new byte[] { (byte)obj });

            if (obj is ulong)
                return data.EqualTo(BitConverter.GetBytes((ulong)obj));

            if (obj is uint)
                return data.EqualTo(BitConverter.GetBytes((uint)obj));

            if (obj is ushort)
                return data.EqualTo(BitConverter.GetBytes((ushort)obj));

            if (obj is DateTime)
                return data.EqualTo(BitConverter.GetBytes(((DateTime)obj).Ticks));

            if (obj is TimeSpan)
                return data.EqualTo(BitConverter.GetBytes(((TimeSpan)obj).Ticks));

            if (obj is char)
                return data.EqualTo(BitConverter.GetBytes((char)obj));

            if (obj is bool)
                return data.EqualTo(BitConverter.GetBytes((bool)obj));

            if (obj is long?)
            {
                var nullable = (long?)obj;
                return nullable.HasValue && data.EqualTo(BitConverter.GetBytes(nullable.Value));
            }

            if (obj is int?)
            {
                var nullable = (int?)obj;
                return nullable.HasValue && data.EqualTo(BitConverter.GetBytes(nullable.Value));
            }

            if (obj is short?)
            {
                var nullable = (short?)obj;
                return nullable.HasValue && data.EqualTo(BitConverter.GetBytes(nullable.Value));
            }

            if (obj is double?)
            {
                var nullable = (long?)obj;
                return nullable.HasValue && data.EqualTo(BitConverter.GetBytes(nullable.Value));
            }

            if (obj is byte?)
            {
                var nullable = (byte?)obj;
                return nullable.HasValue && data.EqualTo(new byte[] { nullable.Value });
            }

            if (obj is ulong?)
            {
                var nullable = (ulong?)obj;
                return nullable.HasValue && data.EqualTo(BitConverter.GetBytes(nullable.Value));
            }

            if (obj is uint?)
            {
                var nullable = (uint?)obj;
                return nullable.HasValue && data.EqualTo(BitConverter.GetBytes(nullable.Value));
            }

            if (obj is ushort?)
            {
                var nullable = (ushort?)obj;
                return nullable.HasValue && data.EqualTo(BitConverter.GetBytes(nullable.Value));
            }

            if (obj is DateTime?)
            {
                var nullable = (DateTime?)obj;
                return nullable.HasValue && data.EqualTo(BitConverter.GetBytes(nullable.Value.Ticks));
            }

            if (obj is TimeSpan?)
            {
                var nullable = (TimeSpan?)obj;
                return nullable.HasValue && data.EqualTo(BitConverter.GetBytes(nullable.Value.Ticks));
            }

            if (obj is char?)
            {
                var nullable = (char?)obj;
                return nullable.HasValue && data.EqualTo(BitConverter.GetBytes(nullable.Value));
            }

            if (obj is bool?)
            {
                var nullable = (bool?)obj;
                return nullable.HasValue && data.EqualTo(BitConverter.GetBytes(nullable.Value));
            }

            return false;
        }

        internal static int IndexOf(this byte[] data, byte b, int startPos = 0, int length = -1)
        {
            if (data != null && length != 0)
            {
                var dataLength = data.Length;

                startPos = Math.Max(0, startPos);
                if (dataLength > 0 && startPos < dataLength)
                {
                    var endPos = dataLength;
                    if (length > 0)
                        endPos = Math.Min(dataLength, startPos + length);

                    for (var i = startPos; i < endPos; i++)
                        if (data[i] == b)
                            return i;
                }
            }
            return -1;
        }

        internal static int ScanCRLF(this byte[] data, int index = 0, int length = -1)
        {
            if (data != null)
            {
                var dataLen = data.Length;
                if (dataLen > 0)
                {
                    length = Math.Max(0, Math.Min(dataLen - index, length));
                    if (length > 0)
                    {
                        var end = Math.Min(dataLen, index + length);
                        for (var i = index; i < end; i++)
                            if (data[i] == '\n' && i >= index && data[i - 1] == '\r')
                                return i;
                    }
                }
            }
            return -1;
        }

        internal static bool Equals<T>(this T[] source, T[] destination, Func<T, T, bool> comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            if (source == null)
                return destination == null;

            if (destination == null)
                return source == null;

            var sourceLen = source.Length;
            if (sourceLen == destination.Length)
            {
                if (sourceLen > 0)
                {
                    for (var i = 0; i < sourceLen; i++)
                    {
                        if (!comparer(source[i], destination[i]))
                            return false;
                    }
                }
                return true;
            }
            return false;
        }

        internal static string NewGuidID()
        {
            return Guid.NewGuid().ToString("N").ToUpper();
        }

        #endregion General

        #region Conversion

        internal static RedisRawObjectType ResponseType(this byte b)
        {
            switch (b)
            {
                case (byte)'+':
                    return RedisRawObjectType.SimpleString;
                case (byte)'-':
                    return RedisRawObjectType.Error;
                case (byte)'$':
                    return RedisRawObjectType.BulkString;
                case (byte)':':
                    return RedisRawObjectType.Integer;
                case (byte)'*':
                    return RedisRawObjectType.Array;
                default:
                    return RedisRawObjectType.Undefined;
            }
        }

        internal static byte ResponseTypeByte(this RedisRawObjectType b)
        {
            switch (b)
            {
                case RedisRawObjectType.SimpleString:
                    return (byte)'+';
                case RedisRawObjectType.Error:
                    return (byte)'-';
                case RedisRawObjectType.BulkString:
                    return (byte)'$';
                case RedisRawObjectType.Integer:
                    return (byte)':';
                case RedisRawObjectType.Array:
                    return (byte)'*';
                default:
                    return (byte)'?';
            }
        }

        internal static double EpochNow()
        {
            return (DateTime.UtcNow - UnixBaseTimeStamp).TotalSeconds;
        }

        internal static double UnixTimeNow()
        {
            return EpochNow();
        }

        internal static double ToUnixTimeStamp(this DateTime date)
        {
            if (date.Kind != DateTimeKind.Utc)
                date = date.ToUniversalTime();
            return (date - UnixBaseTimeStamp).TotalSeconds;
        }

        internal static DateTime FromUnixTimeStamp(this int seconds)
        {
            return FromUnixTimeStamp((long)seconds);
        }

        internal static DateTime FromUnixTimeStamp(this long seconds)
        {
            return UnixBaseTimeStamp.AddSeconds(seconds).ToLocalTime();
        }

        internal static DateTime FromUnixTimeStamp(this double seconds)
        {
            return UnixBaseTimeStamp.AddSeconds(seconds).ToLocalTime();
        }

        internal static DateTime FromUnixTimeStamp(this int seconds, int microSeconds)
        {
            return FromUnixTimeStamp((long)seconds, microSeconds);
        }

        internal static DateTime FromUnixTimeStamp(this long seconds, int microSeconds)
        {
            var date = UnixBaseTimeStamp.AddSeconds(seconds).ToLocalTime();
            return date.AddTicks(microSeconds * 10);
        }

        internal static DateTime FromUnixTimeStamp(this double seconds, int microSeconds)
        {
            var date = UnixBaseTimeStamp.AddSeconds(seconds).ToLocalTime();
            return date.AddTicks(microSeconds * 10);
        }

        internal static int ToInt(this string s, int defaultValue = int.MinValue)
        {
            if (!s.IsEmpty())
            {
                int result;
                if (s.TryParse(out result))
                    return result;
            }
            return defaultValue;
        }

        internal static long ToLong(this string s, long defaultValue = long.MinValue)
        {
            if (!s.IsEmpty())
            {
                long result;
                if (s.TryParse(out result))
                    return result;
            }
            return defaultValue;
        }

        #region ToParams

        internal static RedisParam[] ToParams(this string[] items)
        {
            if (items != null)
            {
                var length = items.Length;
                if (length == 0)
                    return new RedisParam[0];

                var list = new List<RedisParam>(length);
                for (var i = 0; i < length; i++)
                    list.Add(new RedisParam(items[i]));

                return list.ToArray();
            }
            return null;
        }

        internal static RedisParam[] ToParams(this byte[][] items)
        {
            if (items != null)
            {
                var length = items.Length;
                if (length == 0)
                    return new RedisParam[0];

                var list = new List<RedisParam>(length);
                for (var i = 0; i < length; i++)
                    list.Add(new RedisParam(items[i]));

                return list.ToArray();
            }
            return null;
        }

        #endregion ToParams

        #region ToBytes

        internal static byte[] ToBytes(this string obj)
        {
            return (obj != null) ? UTF8.GetBytes(obj) : null;
        }

        internal static byte[] ToBytes(this byte[] obj)
        {
            return obj;
        }

        internal static byte[] ToBytes(this RedisParam obj)
        {
            return obj.Data;
        }

        internal static byte[] ToBytes(this short value)
        {
            var minus = (value < 0);
            if (minus)
            {
                if (value == short.MinValue)
                    return (byte[])ShortMinValue.Clone();

                value = (short)-value;

                if (value < 10)
                    return new byte[] { Minus, (byte)(value + ZeroBase) };

                if (value < 100)
                    return new byte[] { Minus, (byte)((value / 10) + ZeroBase), (byte)((value % 10) + ZeroBase) };

                if (value < 1000)
                    return new byte[] { (byte)((value / 100) + ZeroBase), (byte)(((value / 10) + ZeroBase) % 10), (byte)((value % 10) + ZeroBase) };
            }
            else
            {
                if (value < 10)
                    return new byte[] { (byte)(value + ZeroBase) };

                if (value < 100)
                    return new byte[] { (byte)((value / 10) + ZeroBase), (byte)((value % 10) + ZeroBase) };

                if (value < 1000)
                    return new byte[] { (byte)((value / 100) + ZeroBase), (byte)(((value / 10) + ZeroBase) % 10), (byte)((value % 10) + ZeroBase) };

                if (value == short.MaxValue)
                    return (byte[])ShortMaxValue.Clone();
            }

            var bytes = RedisBytesCache.Acquire(ShortStringLen);

            byte mod;
            var index = IntStringLen;

            do
            {
                mod = (byte)((value % 10) + ZeroBase);
                value /= 10;

                bytes[--index] = mod;
            } while (value > 0);

            if (minus)
                bytes[--index] = Minus;

            var result = (byte[])bytes.Clone(index, IntStringLen - index);
            RedisBytesCache.Release(bytes);

            return result;
        }

        internal static byte[] ToBytes(this int value)
        {
            var minus = (value < 0);
            if (minus)
            {
                if (value == int.MinValue)
                    return (byte[])IntMinValue.Clone();

                value = -value;

                if (value < 10)
                    return new byte[] { Minus, (byte)(value + ZeroBase) };

                if (value < 100)
                    return new byte[] { Minus, (byte)((value / 10) + ZeroBase), (byte)((value % 10) + ZeroBase) };

                if (value < 1000)
                    return new byte[] { (byte)((value / 100) + ZeroBase), (byte)(((value / 10) + ZeroBase) % 10), (byte)((value % 10) + ZeroBase) };
            }
            else
            {
                if (value < 10)
                    return new byte[] { (byte)(value + ZeroBase) };

                if (value < 100)
                    return new byte[] { (byte)((value / 10) + ZeroBase), (byte)((value % 10) + ZeroBase) };

                if (value < 1000)
                    return new byte[] { (byte)((value / 100) + ZeroBase), (byte)(((value / 10) + ZeroBase) % 10), (byte)((value % 10) + ZeroBase) };

                if (value == int.MaxValue)
                    return (byte[])IntMaxValue.Clone();
            }

            var bytes = RedisBytesCache.Acquire(IntStringLen);

            byte mod;
            var index = IntStringLen;

            do
            {
                mod = (byte)((value % 10) + ZeroBase);
                value /= 10;

                bytes[--index] = mod;
            } while (value > 0);

            if (minus)
                bytes[--index] = Minus;

            var result = (byte[])bytes.Clone(index, IntStringLen - index);
            RedisBytesCache.Release(bytes);

            return result;
        }

        internal static byte[] ToBytes(this long value)
        {
            var minus = (value < 0);
            if (minus)
            {
                if (value == long.MinValue)
                    return (byte[])LongMinValue.Clone();

                value = -value;

                if (value < 10L)
                    return new byte[] { Minus, (byte)(value + ZeroBase) };

                if (value < 100L)
                    return new byte[] { Minus, (byte)((value / 10L) + ZeroBase), (byte)((value % 10L) + ZeroBase) };

                if (value < 1000L)
                    return new byte[] { (byte)((value / 100L) + ZeroBase), (byte)(((value / 10L) + ZeroBase) % 10L), (byte)((value % 10L) + ZeroBase) };
            }
            else
            {
                if (value < 10L)
                    return new byte[] { (byte)(value + ZeroBase) };

                if (value < 100L)
                    return new byte[] { (byte)((value / 10L) + ZeroBase), (byte)((value % 10L) + ZeroBase) };

                if (value < 1000L)
                    return new byte[] { (byte)((value / 100L) + ZeroBase), (byte)(((value / 10L) + ZeroBase) % 10L), (byte)((value % 10L) + ZeroBase) };

                if (value == long.MaxValue)
                    return (byte[])LongMaxValue.Clone();
            }

            var bytes = RedisBytesCache.Acquire(LongStringLen);

            byte mod;
            var index = LongStringLen;

            do
            {
                mod = (byte)((value % 10L) + ZeroBase);
                value /= 10L;

                bytes[--index] = mod;
            } while (value > 0);

            if (minus)
                bytes[--index] = Minus;

            var result = (byte[])bytes.Clone(index, IntStringLen - index);
            RedisBytesCache.Release(bytes);

            return result;
        }

        internal static byte[] ToBytes(this ulong value)
        {
            if (value < 10UL)
                return new byte[] { (byte)(value + ZeroBase) };

            if (value < 100UL)
                return new byte[] { (byte)(value / 10UL + ZeroBase), (byte)(value % 10UL + ZeroBase) };

            if (value < 1000UL)
                return new byte[] { (byte)(value / 100UL + ZeroBase), (byte)((value / 10UL + ZeroBase) % 10UL), (byte)(value % 10UL + ZeroBase) };

            if (value == ulong.MaxValue)
                return (byte[])ULongMaxValue.Clone();

            var bytes = RedisBytesCache.Acquire(LongStringLen);

            byte mod;
            var index = LongStringLen;

            do
            {
                mod = (byte)((value % 10L) + ZeroBase);
                value /= 10L;

                bytes[--index] = mod;
            } while (value > 0);

            var result = (byte[])bytes.Clone(index, IntStringLen - index);
            RedisBytesCache.Release(bytes);

            return result;
        }

        internal static byte[] ToBytes(this ushort obj)
        {
            return ToBytes((int)obj);
        }

        internal static byte[] ToBytes(this uint obj)
        {
            return ToBytes((long)obj);
        }

        internal static byte[] ToBytes(this byte obj)
        {
            return new byte[] { obj };
        }

        internal static byte[] ToBytes(this decimal obj)
        {
            return UTF8.GetBytes(obj.ToString(RedisConstants.InvariantCulture));
        }

        internal static byte[] ToBytes(this double obj)
        {
            return UTF8.GetBytes(obj.ToString(RedisConstants.InvariantCulture));
        }

        internal static byte[] ToBytes(this float obj)
        {
            return UTF8.GetBytes(obj.ToString(RedisConstants.InvariantCulture));
        }

        internal static byte[] ToBytes(this DateTime obj)
        {
            return UTF8.GetBytes(obj.Ticks.ToString(RedisConstants.InvariantCulture));
        }

        internal static byte[] ToBytes(this char obj)
        {
            return UTF8.GetBytes(new char[] { obj });
        }

        internal static byte[] ToBytes(this object obj)
        {
            if (obj != null)
            {
                var tc = Type.GetTypeCode(obj.GetType());
                switch (tc)
                {
                    case TypeCode.Object:
                        if (obj is RedisParam)
                            return ((RedisParam)obj).Data;
                        if (obj is byte[])
                            return (byte[])obj;
                        return UTF8.GetBytes(obj.ToString());
                    case TypeCode.String:
                        return UTF8.GetBytes((string)obj);
                    case TypeCode.Int32:
                        return UTF8.GetBytes(((int)obj).ToString(RedisConstants.InvariantCulture));
                    case TypeCode.Int64:
                        return UTF8.GetBytes(((long)obj).ToString(RedisConstants.InvariantCulture));
                    case TypeCode.Decimal:
                        return UTF8.GetBytes(((decimal)obj).ToString(RedisConstants.InvariantCulture));
                    case TypeCode.Double:
                        return UTF8.GetBytes(((double)obj).ToString(RedisConstants.InvariantCulture));
                    case TypeCode.Boolean:
                        return UTF8.GetBytes((bool)obj ? Boolean.TrueString : Boolean.FalseString);
                    case TypeCode.Single:
                        return UTF8.GetBytes(((float)obj).ToString(RedisConstants.InvariantCulture));
                    case TypeCode.Int16:
                        return UTF8.GetBytes(((short)obj).ToString(RedisConstants.InvariantCulture));
                    case TypeCode.UInt32:
                        return UTF8.GetBytes(((uint)obj).ToString(RedisConstants.InvariantCulture));
                    case TypeCode.UInt64:
                        return UTF8.GetBytes(((ulong)obj).ToString(RedisConstants.InvariantCulture));
                    case TypeCode.UInt16:
                        return UTF8.GetBytes(((ushort)obj).ToString(RedisConstants.InvariantCulture));
                    case TypeCode.DateTime:
                        return UTF8.GetBytes(((DateTime)obj).Ticks.ToString(RedisConstants.InvariantCulture));
                    case TypeCode.Char:
                        return UTF8.GetBytes(new char[] { (char)obj });
                    case TypeCode.Byte:
                        return new byte[] { (byte)obj };
                    default:
                        break;
                }
            }
            return null;
        }

        #endregion ToBytes

        #endregion Conversion

        #region Split

        internal static T[] Split<T>(this T[] source, int index, int length)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (length < 0)
                throw new ArgumentException("Length can not be less than zero", "length");

            if (index < 0)
                throw new ArgumentException("Index can not be less than zero", "index");

            if (index > source.Length - 1)
                throw new ArgumentException("Index can not be greater than array length", "index");

            if (index + length > source.Length)
                throw new ArgumentException("Length can not be exceed array length", "length");

            var destination = new T[length];
            Array.Copy(source, index, destination, 0, length);

            return destination;
        }

        internal static T[] Split<T>(this T[] source, int index)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (index < 0)
                throw new ArgumentException("Index can not be less than zero", "index");

            if (index > source.Length - 1)
                throw new ArgumentException("Index can not be greater than array length", "index");

            var length = source.Length - index;

            var destination = new T[length];
            Array.Copy(source, index, destination, 0, length);

            return destination;
        }

        #endregion Split

        #region Merge

        internal static RedisParam[] Merge(this RedisParam[] keys, RedisParam[] values)
        {
            var keysLength = (keys != null) ? keys.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (keysLength != valuesLength)
                throw new ArgumentException("keys length is not equal to values length", "keys");

            if (keysLength < 0)
                return null;

            if (keysLength == 0)
                return new RedisParam[0];

            var result = new RedisParam[2 * keysLength];

            for (int i = 0, index = 0; i < keysLength; i++)
            {
                result[index++] = keys[i];
                result[index++] = values[i];
            }
            return result;
        }

        internal static RedisParam[] Merge(this byte[][] keys, RedisParam[] values)
        {
            var keysLength = (keys != null) ? keys.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (keysLength != valuesLength)
                throw new ArgumentException("keys length is not equal to values length", "keys");

            if (keysLength < 0)
                return null;

            if (keysLength == 0)
                return new RedisParam[0];

            var result = new RedisParam[2 * keysLength];

            for (int i = 0, index = 0; i < keysLength; i++)
            {
                result[index++] = keys[i];
                result[index++] = values[i];
            }
            return result;
        }

        internal static RedisParam[] Merge(this RedisParam[] keys, byte[][] values)
        {
            var keysLength = (keys != null) ? keys.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (keysLength != valuesLength)
                throw new ArgumentException("keys length is not equal to values length", "keys");

            if (keysLength < 0)
                return null;

            if (keysLength == 0)
                return new RedisParam[0];

            var result = new RedisParam[2 * keysLength];

            for (int i = 0, index = 0; i < keysLength; i++)
            {
                result[index++] = keys[i];
                result[index++] = values[i];
            }
            return result;
        }

        internal static RedisParam[] Merge(this byte[][] keys, byte[][] values)
        {
            var keysLength = (keys != null) ? keys.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (keysLength != valuesLength)
                throw new ArgumentException("keys length is not equal to values length", "keys");

            if (keysLength < 0)
                return null;

            if (keysLength == 0)
                return new RedisParam[0];

            var result = new RedisParam[2 * keysLength];

            for (int i = 0, index = 0; i < keysLength; i++)
            {
                result[index++] = keys[i];
                result[index++] = values[i];
            }
            return result;
        }

        internal static RedisParam[] Merge(this byte[][] keys, string[] values)
        {
            var keysLength = (keys != null) ? keys.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (keysLength != valuesLength)
                throw new ArgumentException("keys length is not equal to values length", "keys");

            if (keysLength < 0)
                return null;

            if (keysLength == 0)
                return new RedisParam[0];

            var result = new RedisParam[2 * keysLength];

            for (int i = 0, index = 0; i < keysLength; i++)
            {
                result[index++] = keys[i];

                var s = values[i];
                result[index++] = (s != null) ? UTF8.GetBytes(s) : null;
            }
            return result;
        }

        internal static RedisParam[] Merge(this string[] keys, byte[][] values)
        {
            var keysLength = (keys != null) ? keys.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (keysLength != valuesLength)
                throw new ArgumentException("keys length is not equal to values length", "keys");

            if (keysLength < 0)
                return null;

            if (keysLength == 0)
                return new RedisParam[0];

            var result = new RedisParam[2 * keysLength];

            for (int i = 0, index = 0; i < keysLength; i++)
            {
                var s = keys[i];

                result[index++] = (s != null) ? UTF8.GetBytes(s) : null;
                result[index++] = values[i];
            }
            return result;
        }

        internal static RedisParam[] Merge(this string[] keys, string[] values)
        {
            var keysLength = (keys != null) ? keys.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (keysLength != valuesLength)
                throw new ArgumentException("keys length is not equal to values length", "keys");

            if (keysLength < 0)
                return null;

            if (keysLength == 0)
                return new RedisParam[0];

            var result = new RedisParam[2 * keysLength];

            for (int i = 0, index = 0; i < keysLength; i++)
            {
                var s = keys[i];
                result[index++] = (s != null) ? UTF8.GetBytes(s) : null;

                s = values[i];
                result[index++] = (s != null) ? UTF8.GetBytes(s) : null;
            }
            return result;
        }

        #endregion Merge

        #region Join

        internal static RedisParam[] Join(this RedisParam[] values1, RedisParam[] values2)
        {
            var values1Length = (values1 != null) ? values1.Length : -1;
            var values2Length = (values2 != null) ? values2.Length : -1;

            if (values1Length < 0 && values2Length < 0)
                return null;

            if (values1Length == 0 && values2Length == 0)
                return new RedisParam[0];

            values1Length = Math.Max(0, values1Length);
            values2Length = Math.Max(0, values2Length);

            var resultLen = values1Length + values2Length;
            var result = new RedisParam[resultLen];

            var i = 0;
            for (; i < values1Length; i++)
                result[i] = values1[i];

            for (; i < resultLen; i += 2)
                result[i] = values2[i - values1Length];
            return result;
        }

        internal static RedisParam[] Join(this RedisParam[] values1, byte[][] values2)
        {
            var values1Length = (values1 != null) ? values1.Length : -1;
            var values2Length = (values2 != null) ? values2.Length : -1;

            if (values1Length < 0 && values2Length < 0)
                return null;

            if (values1Length == 0 && values2Length == 0)
                return new RedisParam[0];

            values1Length = Math.Max(0, values1Length);
            values2Length = Math.Max(0, values2Length);

            var resultLen = values1Length + values2Length;
            var result = new RedisParam[resultLen];

            var i = 0;
            for (; i < values1Length; i++)
                result[i] = values1[i];

            for (; i < resultLen; i += 2)
                result[i] = values2[i - values1Length];
            return result;
        }

        internal static RedisParam[] Join(this RedisParam[] values1, byte[] value)
        {
            var values1Length = (values1 != null) ? values1.Length : 0;

            var resultLen = values1Length + 1;
            var result = new RedisParam[resultLen];

            result[resultLen - 1] = value;

            for (var i = 0; i < values1Length; i++)
                result[i] = values1[i];

            return result;
        }

        internal static RedisParam[] Join(this RedisParam[] values1, RedisParam value)
        {
            var values1Length = (values1 != null) ? values1.Length : 0;

            var resultLen = values1Length + 1;
            var result = new RedisParam[resultLen];

            result[resultLen - 1] = value;

            for (var i = 0; i < values1Length; i++)
                result[i] = values1[i];

            return result;
        }

        internal static RedisParam[] Join(this byte[][] values1, RedisParam[] values2)
        {
            var values1Length = (values1 != null) ? values1.Length : -1;
            var values2Length = (values2 != null) ? values2.Length : -1;

            if (values1Length < 0 && values2Length < 0)
                return null;

            if (values1Length == 0 && values2Length == 0)
                return new RedisParam[0];

            values1Length = Math.Max(0, values1Length);
            values2Length = Math.Max(0, values2Length);

            var resultLen = values1Length + values2Length;
            var result = new RedisParam[resultLen];

            var i = 0;
            for (; i < values1Length; i++)
                result[i] = values1[i];

            for (; i < resultLen; i += 2)
                result[i] = values2[i - values1Length];
            return result;
        }


        internal static RedisParam[] Join(this byte[][] values1, byte[][] values2)
        {
            var values1Length = (values1 != null) ? values1.Length : -1;
            var values2Length = (values2 != null) ? values2.Length : -1;

            if (values1Length < 0 && values2Length < 0)
                return null;

            if (values1Length == 0 && values2Length == 0)
                return new RedisParam[0];

            values1Length = Math.Max(0, values1Length);
            values2Length = Math.Max(0, values2Length);

            var resultLen = values1Length + values2Length;
            var result = new RedisParam[resultLen];

            var i = 0;
            for (; i < values1Length; i++)
                result[i] = values1[i];

            for (; i < resultLen; i += 2)
                result[i] = values2[i - values1Length];
            return result;
        }

        internal static RedisParam[] Join(this RedisParam value, byte[][] values)
        {
            var valueLength = !value.IsNull ? value.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (valueLength < 0 && valuesLength < 0)
                return new RedisParam[] { value };

            if (valueLength == 0 && valuesLength == 0)
                return new RedisParam[] { value };

            valueLength = Math.Max(0, valueLength);
            valuesLength = Math.Max(0, valuesLength);

            var resultLength = 1 + valuesLength;
            var result = new RedisParam[resultLength];

            result[0] = value;

            for (var i = 1; i < resultLength; i++)
                result[i] = values[i - 1];
            return result;
        }

        internal static RedisParam[] Join(this byte[] value, byte[][] values)
        {
            var valueLength = (value != null) ? value.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (valueLength < 0 && valuesLength < 0)
                return new RedisParam[] { value };

            if (valueLength == 0 && valuesLength == 0)
                return new RedisParam[] { value };

            valueLength = Math.Max(0, valueLength);
            valuesLength = Math.Max(0, valuesLength);

            var resultLength = 1 + valuesLength;
            var result = new RedisParam[resultLength];

            result[0] = value;

            for (var i = 1; i < resultLength; i++)
                result[i] = values[i - 1];
            return result;
        }

        internal static RedisParam[] Join(this byte[] value, RedisParam[] values)
        {
            var valueLength = (value != null) ? value.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (valueLength < 0 && valuesLength < 0)
                return new RedisParam[] { value };

            if (valueLength == 0 && valuesLength == 0)
                return new RedisParam[] { value };

            valueLength = Math.Max(0, valueLength);
            valuesLength = Math.Max(0, valuesLength);

            var resultLength = 1 + valuesLength;
            var result = new RedisParam[resultLength];

            result[0] = value;

            for (var i = 1; i < resultLength; i++)
                result[i] = values[i - 1];
            return result;
        }

        internal static RedisParam[] Join(this byte[][] values, byte[] value)
        {
            var valueLength = (value != null) ? value.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (valueLength < 0 && valuesLength < 0)
                return new RedisParam[] { value };

            if (valueLength == 0 && valuesLength == 0)
                return new RedisParam[] { value };

            valueLength = Math.Max(0, valueLength);
            valuesLength = Math.Max(0, valuesLength);

            var resultLength = 1 + valuesLength;
            var result = new RedisParam[resultLength];

            result[resultLength - 1] = value;

            for (var i = 0; i < valuesLength; i++)
                result[i] = values[i];
            return result;
        }

        internal static RedisParam[] Join(this RedisParam value, string[] values)
        {
            var valueLength = !value.IsNull ? value.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (valueLength < 0 && valuesLength < 0)
                return new RedisParam[] { value };

            if (valueLength == 0 && valuesLength == 0)
                return new RedisParam[] { value };

            valueLength = Math.Max(0, valueLength);
            valuesLength = Math.Max(0, valuesLength);

            var resultLength = 1 + valuesLength;
            var result = new RedisParam[resultLength];

            result[0] = value;

            for (var i = 1; i < resultLength; i++)
            {
                var s = values[i - 1];
                if (s != null)
                    result[i] = UTF8.GetBytes(s);
            }
            return result;
        }

        internal static RedisParam[] Join(this RedisParam value, RedisParam[] values)
        {
            var valueLength = !value.IsNull ? value.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (valueLength < 0 && valuesLength < 0)
                return new RedisParam[] { value };

            if (valueLength == 0 && valuesLength == 0)
                return new RedisParam[] { value };

            valueLength = Math.Max(0, valueLength);
            valuesLength = Math.Max(0, valuesLength);

            var resultLength = 1 + valuesLength;
            var result = new RedisParam[resultLength];

            result[0] = value;

            for (var i = 1; i < resultLength; i++)
                result[i] = values[i - 1];

            return result;
        }

        internal static RedisParam[] Join(this byte[] value, string[] values)
        {
            var valueLength = (value != null) ? value.Length : -1;
            var valuesLength = (values != null) ? values.Length : -1;

            if (valueLength < 0 && valuesLength < 0)
                return new RedisParam[] { value };

            if (valueLength == 0 && valuesLength == 0)
                return new RedisParam[] { value };

            valueLength = Math.Max(0, valueLength);
            valuesLength = Math.Max(0, valuesLength);

            var resultLength = 1 + valuesLength;
            var result = new RedisParam[resultLength];

            result[0] = value;

            for (var i = 1; i < resultLength; i++)
            {
                var s = values[i - 1];
                if (s != null)
                    result[i] = UTF8.GetBytes(s);
            }
            return result;
        }

        internal static RedisParam[] Join(this RedisParam[] values1, string[] values2)
        {
            var values1Length = (values1 != null) ? values1.Length : -1;
            var values2Length = (values2 != null) ? values2.Length : -1;

            if (values1Length < 0 && values2Length < 0)
                return null;

            if (values1Length == 0 && values2Length == 0)
                return new RedisParam[0];

            values1Length = Math.Max(0, values1Length);
            values2Length = Math.Max(0, values2Length);

            var resultLength = values1Length + values2Length;
            var result = new RedisParam[resultLength];

            if (values1Length > 0)
            {
                for (var i = 0; i < values1Length; i++)
                    result[i] = values1[i];
            }

            for (var i = values1Length; i < resultLength; i++)
            {
                var s = values2[i - values1Length];
                if (s != null)
                    result[i] = UTF8.GetBytes(s);
            }
            return result;
        }

        internal static RedisParam[] Join(this byte[][] values1, string[] values2)
        {
            var values1Length = (values1 != null) ? values1.Length : -1;
            var values2Length = (values2 != null) ? values2.Length : -1;

            if (values1Length < 0 && values2Length < 0)
                return null;

            if (values1Length == 0 && values2Length == 0)
                return new RedisParam[0];

            values1Length = Math.Max(0, values1Length);
            values2Length = Math.Max(0, values2Length);

            var resultLength = values1Length + values2Length;
            var result = new RedisParam[resultLength];

            if (values1Length > 0)
                Array.Copy(result, values1, values1Length);

            for (var i = values1Length; i < resultLength; i++)
            {
                var s = values2[i - values1Length];
                if (s != null)
                    result[i] = UTF8.GetBytes(s);
            }
            return result;
        }

        internal static RedisParam[] Join(this byte[] value1, byte[] value2)
        {
            return new RedisParam[2] { value1, value2 };
        }

        internal static RedisParam[] Join(this RedisParam value1, byte[] value2)
        {
            return new RedisParam[2] { value1, value2 };
        }

        internal static RedisParam[] Join(this RedisParam value1, RedisParam value2)
        {
            return new RedisParam[2] { value1, value2 };
        }

        internal static RedisParam[] Join(this string[] values1, RedisParam[] values2)
        {
            var values1Length = (values1 != null) ? values1.Length : -1;
            var values2Length = (values2 != null) ? values2.Length : -1;

            if (values1Length < 0 && values2Length < 0)
                return null;

            if (values1Length == 0 && values2Length == 0)
                return new RedisParam[0];

            values1Length = Math.Max(0, values1Length);
            values2Length = Math.Max(0, values2Length);

            var resultLength = values1Length + values2Length;
            var result = new RedisParam[resultLength];

            var i = 0;
            for (; i < values1Length; i++)
            {
                var s = values1[i];
                if (s != null)
                    result[i] = UTF8.GetBytes(s);
            }

            for (; i < resultLength; i++)
                result[i] = values2[i - values1Length];
            return result;

        }

        internal static RedisParam[] Join(this string[] values1, string[] values2)
        {
            var values1Length = (values1 != null) ? values1.Length : -1;
            var values2Length = (values2 != null) ? values2.Length : -1;

            if (values1Length < 0 && values2Length < 0)
                return null;

            if (values1Length == 0 && values2Length == 0)
                return new RedisParam[0];

            values1Length = Math.Max(0, values1Length);
            values2Length = Math.Max(0, values2Length);

            var resultLength = values1Length + values2Length;
            var result = new RedisParam[resultLength];

            var i = 0;
            for (; i < values1Length; i++)
            {
                var s = values1[i];
                if (s != null)
                    result[i] = UTF8.GetBytes(s);
            }

            for (; i < resultLength; i++)
            {
                var s = values2[i - values1Length];
                if (s != null)
                    result[i] = UTF8.GetBytes(s);
            }
            return result;

        }

        #endregion Join

        #region Commands

        internal static bool IsDbRequiredCommand(this byte[] cmd)
        {
            if (!cmd.IsEmpty())
                return !RedisConstants.CommandsNotRequireDB.Contains(cmd);
            return false;
        }

        internal static bool IsUpdateCommand(this byte[] cmd)
        {
            if (!cmd.IsEmpty())
                return RedisConstants.CommandsThatUpdate.Contains(cmd);
            return false;
        }

        internal static bool IsAnyRoleCommand(this byte[] cmd)
        {
            if (!cmd.IsEmpty())
                return RedisConstants.CommandsForAnyRole.Contains(cmd);
            return false;
        }

        internal static RedisRole CommandRole(this byte[] cmd)
        {
            if (cmd.IsEmpty())
                return RedisRole.Undefined;
            if (cmd == RedisCommandList.Sentinel)
                return RedisRole.Sentinel;
            if (cmd.IsAnyRoleCommand())
                return RedisRole.Any;
            if (cmd.IsUpdateCommand())
                return RedisRole.Master;
            return RedisRole.Slave;
        }

        #endregion Commands

        #region Socket

        internal static bool IsConnected(this Socket socket, int poll = -1)
        {
            if (socket != null && socket.Connected)
            {
                if (poll > -1)
                    return !(socket.Poll(poll, SelectMode.SelectRead) && (socket.Available == 0));
                return true;
            }
            return false;
        }

        internal static void DisposeSocket(this Socket socket)
        {
            if (socket != null)
            {
                Action<object> action = (state) =>
                {
                    var sck = state as Socket;
                    if (sck != null)
                    {
                        try
                        {
                            sck.Shutdown(SocketShutdown.Both);
                            if (sck.IsConnected(10))
                                sck.Close();
                        }
                        catch (Exception)
                        { }
                        try
                        {
                            sck.Dispose();
                        }
                        catch (Exception)
                        { }
                    }
                };

                try
                {
                    var eventQ = RedisEventQueue.Default;
                    if (!eventQ.IsAlive())
                        action(socket);
                    else
                        eventQ.Enqueu(action, socket);
                }
                catch (Exception)
                { }
            }
        }

        #endregion Socket

        #endregion Methods
    }
}
