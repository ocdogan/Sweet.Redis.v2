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

using System;
using System.Collections.Generic;
using System.Threading;

namespace Sweet.Redis.v2
{
    /*
    # Sentinel
    sentinel_masters:1
    sentinel_tilt:0
    sentinel_running_scripts:0
    sentinel_scripts_queue_length:0
    sentinel_simulate_failure_flags:0
    master0:name=mymaster,status=ok,address=127.0.0.1:6379,slaves=2,sentinels=1
    */
    public class RedisServerInfoSentinelSection : RedisServerInfoSection
    {
        #region Field Members

        private RedisServerMasterInfo[] m_Masters;
        private List<RedisServerMasterInfo> m_MastersList = new List<RedisServerMasterInfo>();

        #endregion Field Members

        #region .Ctors

        internal RedisServerInfoSentinelSection(string sectionName)
            : base(sectionName)
        { }

        #endregion .Ctors

        #region Properties

        public RedisServerMasterInfo[] Masters // name=mymaster,status=ok,address=127.0.0.1:6379,slaves=2,sentinels=1
        {
            get
            {
                if (m_Masters == null)
                {
                    var list = Interlocked.Exchange(ref m_MastersList, null);
                    m_Masters = list != null ? list.ToArray() : new RedisServerMasterInfo[0];
                }
                return m_Masters;
            }
        }

        public long? SentinelMasters { get { return GetInteger("sentinel_masters"); } } // 1

        public long? SentinelTilt { get { return GetInteger("sentinel_tilt"); } } // 0

        public long? SentinelRunningScripts { get { return GetInteger("sentinel_running_scripts"); } } // 0

        public long? SentinelScriptsQueueLength { get { return GetInteger("sentinel_scripts_queue_length"); } } // 0

        public long? SentinelSimulateFailureFlags { get { return GetInteger("sentinel_simulate_failure_flags"); } } // 0

        #endregion Properties

        #region Methods

        protected override string OnSetValue(string name, string value)
        {
            if (!name.IsEmpty())
            {
                var masterLength = "master".Length;
                if ((name.Length > masterLength) && name.StartsWith("master", StringComparison.OrdinalIgnoreCase))
                {
                    var indexStr = name.Substring(masterLength);
                    if (!indexStr.IsEmpty())
                    {
                        int index;
                        if (indexStr.TryParse(out index))
                            m_MastersList.Add(new RedisServerMasterInfo(index, value));
                    }
                }
            }
            return base.OnSetValue(name, value);
        }

        #endregion Methods
    }
}
