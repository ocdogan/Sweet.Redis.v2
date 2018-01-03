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
    public static class RedisCommandUtils
    {
        public static RedisScanBytes ToScanBytes(RedisArray array)
        {
            if (array == null)
                return new RedisScanBytes(null);

            var items = array.Value;
            if (items == null)
                return new RedisScanBytes(null);

            var count = items.Count;
            if (count == 0)
                return new RedisScanBytes(null);

            var item = items[0];
            if (item == null)
                return new RedisScanBytes(null);

            if (item.Type != RedisResultType.Bytes)
                throw new RedisException("Invalid scan cursor type");

            var str = ((RedisBytes)item).Value.ToUTF8String();
            if (str.IsEmpty())
                throw new RedisException("Invalid scan cursor type");

            var cursor = ulong.Parse(str);

            var result = (RedisByteArray[])null;
            if (items.Count > 1)
            {
                item = items[1];
                if (item != null)
                {
                    if (item.Type != RedisResultType.Array)
                        throw new RedisException("Invalid scan result type");

                    var subItems = ((RedisArray)item).Value;
                    if (subItems != null)
                    {
                        var subCount = subItems.Count;
                        if (subCount > 0)
                        {
                            var list = new List<RedisByteArray>(subCount);
                            for (var i = 0; i < count; i++)
                            {
                                var subItem = subItems[i];
                                if (subItem == null || subItem.Type != RedisResultType.Bytes)
                                    throw new RedisException("Invalid scan result item type");

                                list.Add(((RedisBytes)subItem).Value);
                            }

                            result = list.ToArray();
                        }
                    }
                }
            }

            return new RedisScanBytes(new RedisScanBytesData(cursor, result));
        }

        public static RedisScanStrings ToScanStrings(RedisArray array)
        {
            if (array == null)
                return new RedisScanStrings(null);

            var items = array.Value;
            if (items == null)
                return new RedisScanStrings(null);

            var count = items.Count;
            if (count == 0)
                return new RedisScanStrings(null);

            var item = items[0];
            if (item == null)
                return new RedisScanStrings(null);

            if (item.Type != RedisResultType.Bytes)
                throw new RedisException("Invalid scan cursor type");

            var data = ((RedisBytes)item).Value.ToUTF8String();
            if (data.IsEmpty())
                throw new RedisException("Invalid scan cursor type");

            var cursor = ulong.Parse(data);

            var result = (string[])null;
            if (items.Count > 1)
            {
                item = items[1];
                if (item != null)
                {
                    if (item.Type != RedisResultType.Array)
                        throw new RedisException("Invalid scan result type");

                    var subItems = ((RedisArray)item).Value;
                    if (subItems != null)
                    {
                        var subCount = subItems.Count;
                        if (subCount > 0)
                        {
                            var list = new List<string>(subCount);
                            for (var i = 0; i < count; i++)
                            {
                                var subItem = subItems[i];
                                if (subItem == null || subItem.Type != RedisResultType.Bytes)
                                    throw new RedisException("Invalid scan result item type");

                                list.Add(((RedisBytes)subItem).Value.ToUTF8String());
                            }

                            result = list.ToArray();
                        }
                    }
                }
            }

            return new RedisScanStrings(new RedisScanStringsData(cursor, result));
        }

        public static RedisResult<RedisGeoPosition[]> ToGeoPosition(RedisArray array)
        {
            if (array == null)
                return new RedisResult<RedisGeoPosition[]>(new RedisGeoPosition[0]);

            var items = array.Value;
            if (items == null)
                return new RedisResult<RedisGeoPosition[]>(new RedisGeoPosition[0]);

            var count = items.Count;

            var result = new RedisGeoPosition[count];
            if (count > 0)
                for (var i = 0; i < count; i++)
                    result[i] = ToGeoPositionItem((RedisArray)items[i]);

            return new RedisResult<RedisGeoPosition[]>(result);
        }

        public static RedisResult<RedisGeoRadiusResult[]> ToGeoRadiusArray(RedisArray array)
        {
            if (array == null)
                return new RedisResult<RedisGeoRadiusResult[]>(new RedisGeoRadiusResult[0]);

            var items = array.Value;
            if (items == null)
                return new RedisResult<RedisGeoRadiusResult[]>(new RedisGeoRadiusResult[0]);

            var count = items.Count;
            if (count == 0)
                return new RedisResult<RedisGeoRadiusResult[]>(new RedisGeoRadiusResult[0]);

            var list = new List<RedisGeoRadiusResult>(count);
            for (var i = 0; i < count; i++)
                list.Add(ToGeoRadiusResult(items[i]));

            return new RedisResult<RedisGeoRadiusResult[]>(list.ToArray());
        }

        public static RedisGeoRadiusResult ToGeoRadiusResult(RedisResult obj)
        {
            if (obj != null)
            {
                if (obj.Type == RedisResultType.Bytes)
                {
                    var member = ((RedisBytes)obj).Value;
                    if (member != null)
                        return new RedisGeoRadiusResult(member.ToUTF8String());
                }
                if (obj.Type == RedisResultType.String)
                {
                    var member = ((RedisString)obj).Value;
                    if (member != null)
                        return new RedisGeoRadiusResult(member);
                }
                else if (obj.Type == RedisResultType.Array)
                {
                    var items = ((RedisArray)obj).Value;
                    if (items == null)
                        return null;

                    var count = items.Count;
                    if (count < 1)
                        return null;

                    object data;
                    string member = null;
                    double? distance = null;
                    long? hash = null;
                    RedisGeoPosition? coord = null;

                    var item = items[0];
                    if (item != null)
                    {
                        if (item.Type == RedisResultType.Bytes)
                            member = ((RedisBytes)item).Value.ToUTF8String();
                        else if (item.Type == RedisResultType.String)
                            member = ((RedisString)item).Value;
                    }

                    for (var i = 1; i < count; i++)
                    {
                        var child = items[i];
                        if (child != null)
                        {
                            if (child.Type == RedisResultType.Array)
                                coord = ToGeoPositionItem((RedisArray)child);
                            else
                            {
                                if (child.Type == RedisResultType.Integer)
                                {
                                    data = ((RedisInteger)child).Value;
                                    if (data is long)
                                        hash = (long)data;
                                }
                                else
                                {
                                    var str = (string)null;
                                    if (child.Type == RedisResultType.Bytes)
                                        str = ((RedisBytes)child).Value.ToUTF8String();
                                    else if (child.Type == RedisResultType.String)
                                        str = ((RedisString)child).Value;

                                    if (str != null)
                                    {
                                        var d = 0d;
                                        if (double.TryParse(str, out d))
                                            distance = d;
                                    }
                                }
                            }
                        }
                    }

                    return new RedisGeoRadiusResult(member, coord, distance, hash);
                }
            }
            return null;
        }

        private static RedisGeoPosition ToGeoPositionItem(RedisArray array)
        {
            if (array != null)
            {
                var items = array.Value;
                if (items != null && items.Count >= 2)
                {
                    var item = items[0] as RedisBytes;
                    if (!ReferenceEquals(item, null))
                    {
                        var data = item.Value;
                        if (!data.IsEmpty())
                        {
                            double longitude;
                            if (double.TryParse(data.ToUTF8String(), out longitude))
                            {
                                item = items[1] as RedisBytes;
                                if (!ReferenceEquals(item, null))
                                {
                                    data = item.Value;
                                    if (!data.IsEmpty())
                                    {
                                        double latitude;
                                        if (double.TryParse(data.ToUTF8String(), out latitude))
                                            return new RedisGeoPosition(longitude, latitude);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return RedisGeoPosition.Empty;
        }
    }
}
