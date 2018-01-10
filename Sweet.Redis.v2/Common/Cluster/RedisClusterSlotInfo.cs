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
using System.Text;

namespace Sweet.Redis.v2
{
    /*
    *3 // Slot count
    *4 // 1st slot item count
    :5461 // Start slot range
    :10922 // End slot range
    *2 // Master for slot range represented as nested IP/Port array
    $9 // IP value length
    127.0.0.1 // IP value
    :7001 // Port
    *2 // IP value length
    $9 // IP value length
    127.0.0.1
    :7004 // Port
    *4 // 2nd slot item count
    :10923 // Start slot range
    :16383 // End slot range
    *2 // Master for slot range represented as nested IP/Port array
    $9 // IP value length
    127.0.0.1
    :7002
    *2 // Master for slot range represented as nested IP/Port array
    $9 // IP value length
    127.0.0.1 // IP value
    :7005
    *4 // 3rd slot item count
    :0 // Start slot range
    :5460 // End slot range
    *2 // Master for slot range represented as nested IP/Port array
    $9 // IP value length
    127.0.0.1
    :7000 // Port
    *2 // Master for slot range represented as nested IP/Port array
    $9 // IP value length
    127.0.0.1 // IP value
    :7003 // Port
    */
    public class RedisClusterSlotInfo : RedisInfoBase
    {
        #region Field Members

        private RedisEndPoint[] m_Masters;

        #endregion Field Members

        #region .Ctors

        private RedisClusterSlotInfo(RedisArray info)
        {
            ParseInternal(info);
        }

        #endregion .Ctors

        #region Properties

        public long? StartSlotRange { get { return GetInteger("start_slot_range"); } } // 0

        public long? EndSlotRange { get { return GetInteger("end_slot_range"); } } // 5460

        public RedisEndPoint[] Masters { get { return m_Masters; } }

        #endregion Properties

        #region Methods

        private void ParseInternal(RedisArray info)
        {
            if (!ReferenceEquals(info, null))
            {
                var items = info.Value;
                if (items != null)
                {
                    var count = items.Count;
                    if (count > 0)
                    {
                        var rangeStart = items[0] as RedisInteger;
                        if (!ReferenceEquals(rangeStart, null))
                        {
                            base["start_slot_range"] = rangeStart.Value.ToString();

                            if (count > 1)
                            {
                                var rangeEnd = items[1] as RedisInteger;
                                if (!ReferenceEquals(rangeEnd, null))
                                {
                                    base["end_slot_range"] = rangeEnd.Value.ToString();

                                    if (count > 2)
                                    {
                                        var endPoints = new List<RedisEndPoint>(count - 2);

                                        for (var i = 2; i < count; i++)
                                        {
                                            var ep = items[i] as RedisArray;
                                            if (!ReferenceEquals(ep, null))
                                            {
                                                var epList = ep.Value;
                                                if (epList != null)
                                                {
                                                    var length = epList.Count;
                                                    if (length > 1)
                                                    {
                                                        var ip = epList[0] as RedisString;
                                                        if (!ReferenceEquals(ip, null))
                                                        {
                                                            var ipValue = ip.Value;
                                                            if (!ipValue.IsEmpty())
                                                            {
                                                                var port = epList[1] as RedisInteger;
                                                                if (!ReferenceEquals(port, null))
                                                                    endPoints.Add(new RedisEndPoint(ipValue, (int)port.Value));
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        m_Masters = endPoints.ToArray();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static RedisClusterSlotInfo[] Parse(RedisArray parts)
        {
            if (!ReferenceEquals(parts, null))
            {
                var items = parts.Value;
                if (items != null)
                {
                    var count = items.Count;
                    if (count > 0)
                    {
                        var result = new List<RedisClusterSlotInfo>(count);
                        for (var i = 0; i < count; i++)
                        {
                            var item = items[i];
                            if (!ReferenceEquals(item, null))
                            {
                                var info = item as RedisArray;
                                if (!ReferenceEquals(info, null))
                                    result.Add(new RedisClusterSlotInfo(info));
                            }
                        }

                        return result.ToArray();
                    }
                }
            }
            return null;
        }

        #endregion Methods
    }
}
