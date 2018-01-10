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

namespace Sweet.Redis.v2
{
    /*
    cluster_state:ok
    cluster_slots_assigned:16384
    cluster_slots_ok:16384
    cluster_slots_pfail:0
    cluster_slots_fail:0
    cluster_known_nodes:6
    cluster_size:3
    cluster_current_epoch:6
    cluster_my_epoch:2
    cluster_stats_messages_sent:1117
    cluster_stats_messages_received:1117
    */
    public class RedisClusterInfo : RedisInfoBase
    {
        #region .Ctors

        internal RedisClusterInfo(string[] infoLines = null)
        {
            if (infoLines != null)
            {
                var length = infoLines.Length;
                if (length > 0)
                {
                    for (var i = 0; i < length; i += 2)
                    {
                        var name = infoLines[i];
                        if (name != null)
                            base[name] = i < length - 1 ? infoLines[i + 1] : null;
                    }
                }
            }
        }

        internal RedisClusterInfo(RedisString lines)
        {
            Parse(lines);
        }

        #endregion .Ctors

        #region Properties

        public string State { get { return Get("cluster_state"); } } // ok

        public long? SlotsAssigned { get { return GetInteger("cluster_slots_assigned"); } } // 16384

        public long? SlotsOK { get { return GetInteger("cluster_slots_ok"); } } // 16384

        public long? SlotsPFail { get { return GetInteger("cluster_slots_pfail"); } } // 0

        public long? SlotsFail { get { return GetInteger("cluster_slots_fail"); } } // 0

        public long? KnownNodes { get { return GetInteger("cluster_known_nodes"); } } // 6

        public long? Size { get { return GetInteger("cluster_size"); } } // 3

        public long? CurrentEpoch { get { return GetInteger("cluster_current_epoch"); } } // 6

        public long? MyEpoch { get { return GetInteger("cluster_my_epoch"); } } // 2

        public long? StatsMessagesSent { get { return GetInteger("cluster_stats_messages_sent"); } } // 1117

        public long? StatsMessagesReceived { get { return GetInteger("cluster_stats_messages_received"); } } // 1117
        
        #endregion Properties

        #region Methods

        protected virtual void Parse(RedisString lines)
        {
            if (!ReferenceEquals(lines, null))
            {
                var linesValue = lines.Value;
                if (!linesValue.IsEmpty())
                {
                    var items = linesValue.Split(new[] { RedisConstants.CRLF }, StringSplitOptions.RemoveEmptyEntries);
                    if (items != null)
                    {
                        var count = items.Length;
                        if (count > 0)
                        {
                            for (var i = 0; i < count; i += 2)
                            {
                                var item = items[i];
                                if (item != null)
                                {
                                    var length = item.Length;
                                    if (length > 0)
                                    {
                                        var pos = item.IndexOf(':');
                                        if (pos > -1)
                                        {
                                            var key = (item.Substring(0, pos) ?? String.Empty).Trim();
                                            if (!key.IsEmpty())
                                            {
                                                var value = pos < length - 1 ? (item.Substring(pos + 1, length - pos - 1) ?? String.Empty).Trim() : null;
                                                base[key] = value.IsEmpty() ? null : value;
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

        #endregion Methods
    }
}
