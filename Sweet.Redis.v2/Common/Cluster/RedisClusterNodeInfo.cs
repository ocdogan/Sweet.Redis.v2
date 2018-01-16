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
    <id> <ip:port> <flags> <master> <ping-sent> <pong-recv> <config-epoch> <link-state> <slot> <slot> ... <slot>
    
    780813af558af81518e58e495d63b6e248e80adf 127.0.0.1:7000 master - 0 1515566855321 1 connected 0-5460
    b1dc6b4fd60ffcff552d669b3294f6991ccb2b68 127.0.0.1:7005 slave 17f2a81c7fc22283ef93bf118cedb193a1122399 0 1515566855821 6 connected
    17f2a81c7fc22283ef93bf118cedb193a1122399 127.0.0.1:7002 master - 0 1515566854820 3 connected 10923-16383
    eaf2fc952dad107ffa28c0f436f73c3d52112276 127.0.0.1:7004 slave ea828c6074663c8bd4e705d3e3024d9d1721ef3b 0 1515566856323 5 connected
    ea828c6074663c8bd4e705d3e3024d9d1721ef3b 127.0.0.1:7001 myself,master - 0 0 2 connected 5461-10922
    5ffbeb7f7872923ee0ed0913aedfe806265f5090 127.0.0.1:7003 slave 780813af558af81518e58e495d63b6e248e80adf 0 1515566856825 4 connected
    */
    public class RedisClusterNodeInfo : RedisInfoBase
    {
        #region .Ctors

        private RedisClusterNodeInfo(string[] parts)
        {
            Parse(parts);
        }

        #endregion .Ctors

        #region Properties

        public string Id { get { return Get("id"); } } // 780813af558af81518e58e495d63b6e248e80adf

        public string IpPort { get { return Get("ip_port"); } } // 127.0.0.1:7000

        public string Flags { get { return Get("flags"); } } // master|slave|myself,master

        public string Master { get { return Get("master"); } } // -|17f2a81c7fc22283ef93bf118cedb193a1122399

        public long? PingSent { get { return GetInteger("ping-sent"); } } // 0

        public long? PongReceived { get { return GetInteger("pong-recv"); } } // 0|1515566855321

        public long? ConfigEpoch { get { return GetInteger("config-epoch"); } } // 1

        public string LinkState { get { return Get("link-state"); } } // connected

        public string Slots { get { return Get("slots"); } } // nil|0-5460,5461,5462-8000

        #endregion Properties

        #region Methods

        private void Parse(string[] parts)
        {
            if (parts != null)
            {
                var length = parts.Length;
                if (length > 0)
                {
                    var value = parts[0];
                    base["id"] = value.IsEmpty() ? null : value;

                    if (length > 1)
                    {
                        value = parts[1];
                        base["ip_port"] = value.IsEmpty() ? null : value;

                        if (length > 2)
                        {
                            value = parts[2];
                            base["flags"] = value.IsEmpty() ? null : value;

                            if (length > 3)
                            {
                                value = parts[3];
                                base["master"] = value.IsEmpty() ? null : value;

                                if (length > 4)
                                {
                                    value = parts[4];
                                    base["ping-sent"] = value.IsEmpty() ? null : value;

                                    if (length > 5)
                                    {
                                        value = parts[5];
                                        base["pong-recv"] = value.IsEmpty() ? null : value;

                                        if (length > 6)
                                        {
                                            value = parts[6];
                                            base["config-epoch"] = value.IsEmpty() ? null : value;
                                        }

                                        if (length > 7)
                                        {
                                            value = parts[7];
                                            base["link-state"] = value.IsEmpty() ? null : value;

                                            if (length > 8)
                                            {
                                                if (length == 9)
                                                    base["slots"] = parts[8];
                                                else
                                                {
                                                    var slots = new StringBuilder(10 * (length - 8));
                                                    for (var i = 8; i < length; i++)
                                                    {
                                                        if (i > 8)
                                                            slots.Append(',');
                                                        slots.Append(parts[i]);
                                                    }

                                                    base["slots"] = slots.ToString();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static RedisClusterNodeInfo[] Parse(RedisString lines)
        {
            if (!ReferenceEquals(lines, null))
            {
                var linesValue = lines.Value;
                if (!linesValue.IsEmpty())
                {
                    var items = linesValue.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (items != null)
                    {
                        var count = items.Length;
                        if (count > 0)
                        {
                            var result = new List<RedisClusterNodeInfo>(count);

                            for (var i = 0; i < count; i++)
                            {
                                var item = items[i];
                                if (!item.IsEmpty())
                                {
                                    var parts = item.Split(new[] { ' ' }, StringSplitOptions.None);
                                    if (!parts.IsEmpty())
                                        result.Add(new RedisClusterNodeInfo(parts));
                                }
                            }

                            return result.ToArray();
                        }
                    }
                }
            }
            return null;
        }

        #endregion Methods
    }
}
