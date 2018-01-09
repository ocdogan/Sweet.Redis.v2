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
    internal class RedisSentinelCommands : RedisCommandSet, IRedisCommandsSentinel
    {
        #region .Ctors

        public RedisSentinelCommands(RedisAsyncCommandExecuter executer)
            : base(executer)
        { }

        #endregion .Ctors

        #region Methods

        public RedisString CheckQuorum(string masterName)
        {
            if (masterName.IsEmpty())
                throw new ArgumentNullException("masterName");

            return ExpectSimpleString(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.SentinelCheckQuorum));
        }

        public RedisBool Failover(string masterName)
        {
            if (masterName.IsEmpty())
                throw new ArgumentNullException("masterName");

            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.SentinelFailover, masterName.ToBytes()));
        }

        public RedisBool FlushConfig()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.SentinelFlushConfig));
        }

        public RedisResult<RedisEndPointInfo> GetMasterAddrByName(string masterName)
        {
            if (masterName.IsEmpty())
                throw new ArgumentNullException("masterName");

            var array = ExpectArray(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.SentinelGetMasterAddrByName, masterName.ToBytes()));
            if (!ReferenceEquals(array, null))
            {
                var items = array.Value;
                if (items != null)
                {
                    var count = items.Count;
                    if (count > 0)
                    {
                        var ipResult = items[0];
                        if (!ReferenceEquals(ipResult, null) && ipResult.Type == RedisResultType.Bytes)
                        {
                            var ipAddress = ((RedisBytes)ipResult).Value.ToUTF8String();
                            if (!ipAddress.IsEmpty())
                            {
                                int port = 0;
                                if (count > 1)
                                {
                                    var portResult = items[1];
                                    if (!ReferenceEquals(portResult, null) && portResult.Type == RedisResultType.Bytes)
                                    {
                                        var data = ((RedisBytes)portResult).Value.ToUTF8String();
                                        if (!data.IsEmpty())
                                        {
                                            long l;
                                            if (data.TryParse(out l))
                                                port = (int)l;
                                        }
                                    }
                                }

                                return new RedisResult<RedisEndPointInfo>(new RedisEndPointInfo(ipAddress, port));
                            }
                        }
                    }
                }
            }
            return new RedisResult<RedisEndPointInfo>(null);
        }

        public RedisResult<RedisServerInfo> Info(RedisParam? section = null)
        {
            string lines;
            if (!section.HasValue || section.Value.IsNull)
                lines = ExpectBulkString(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Info));
            else
                lines = ExpectBulkString(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Info, section.Value));

            var info = RedisServerInfo.Parse(lines);
            return new RedisResult<RedisServerInfo>(info);
        }

        public RedisResult<RedisIsMasterDownInfo> IsMasterDownByAddr(string ipAddress, int port, string runId)
        {
            if (ipAddress.IsEmpty())
                throw new ArgumentNullException("ipAddress");

            if (runId.IsEmpty())
                throw new ArgumentNullException("runId");

            var array = ExpectArray(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.SentinelIsMasterDownByAddr,
                                  ipAddress.ToBytes(), port.ToBytes(),
                                  ((long)RedisCommon.EpochNow()).ToBytes(), runId.ToBytes()));
            if (!ReferenceEquals(array, null))
            {
                var info = RedisIsMasterDownInfo.Parse(array);
                return new RedisResult<RedisIsMasterDownInfo>(info);
            }
            return new RedisResult<RedisIsMasterDownInfo>(RedisIsMasterDownInfo.Empty);
        }

        public RedisResult<RedisSentinelMasterInfo> Master(string masterName)
        {
            if (masterName.IsEmpty())
                throw new ArgumentNullException("masterName");

            var lines = ExpectMultiDataStrings(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.SentinelMaster, masterName.ToBytes()));
            if (!ReferenceEquals(lines, null))
            {
                var info = new RedisSentinelMasterInfo(lines);
                return new RedisResult<RedisSentinelMasterInfo>(info);
            }
            return new RedisResult<RedisSentinelMasterInfo>(null);
        }

        public RedisResult<RedisSentinelMasterInfo[]> Masters()
        {
            var array = ExpectArray(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.SentinelMasters));
            if (!ReferenceEquals(array, null))
            {
                var items = array.Value;
                if (items != null)
                {
                    var count = items.Count;
                    if (count > 0)
                    {
                        var list = new List<RedisSentinelMasterInfo>(count);
                        for (var i = 0; i < count; i++)
                        {
                            var item = items[i];
                            if (!ReferenceEquals(item, null) && item.Type == RedisResultType.Array)
                            {
                                var info = new RedisSentinelMasterInfo(((RedisArray)item).ToMultiString());
                                list.Add(info);
                            }
                        }

                        if (list.Count > 0)
                            return new RedisResult<RedisSentinelMasterInfo[]>(list.ToArray());
                    }
                }
            }
            return new RedisResult<RedisSentinelMasterInfo[]>(null);
        }

        public RedisBool Monitor(string masterName, string ipAddress, int port, int quorum)
        {
            if (masterName.IsEmpty())
                throw new ArgumentNullException("masterName");

            if (ipAddress.IsEmpty())
                throw new ArgumentNullException("ipAddress");

            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.SentinelMonitor,
                                      masterName.ToBytes(), ipAddress.ToBytes(), port.ToBytes(), quorum.ToBytes()));
        }

        public RedisBool Ping()
        {
            var pong = ExpectSimpleString(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Ping));
            return pong == RedisConstants.PONG;
        }

        public RedisBool Remove(string masterName)
        {
            if (masterName.IsEmpty())
                throw new ArgumentNullException("masterName");

            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.SentinelRemove, masterName.ToBytes()));
        }

        public RedisInteger Reset(RedisParam pattern)
        {
            if (pattern.IsNull)
                throw new ArgumentNullException("pattern");

            return ExpectInteger(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.SentinelReset, pattern));
        }

        public RedisResult<RedisRoleInfo> Role()
        {
            var array = ExpectArray(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Role));
            if (!ReferenceEquals(array, null))
            {
                var result = RedisRoleInfo.Parse(array);
                return new RedisResult<RedisRoleInfo>(result);
            }
            return new RedisResult<RedisRoleInfo>(null);
        }

        public RedisResult<RedisSentinelNodeInfo[]> Sentinels(string masterName)
        {
            if (masterName.IsEmpty())
                throw new ArgumentNullException("masterName");

            var array = ExpectArray(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.Sentinels, masterName.ToBytes()));
            if (!ReferenceEquals(array, null))
            {
                var items = RedisSentinelNodeInfo.ParseInfoResponse(array);
                if (!items.IsEmpty())
                    return new RedisResult<RedisSentinelNodeInfo[]>(items);
            }
            return new RedisResult<RedisSentinelNodeInfo[]>(null);
        }

        public RedisBool Set(string masterName, RedisParam parameter, RedisParam value)
        {
            if (masterName.IsEmpty())
                throw new ArgumentNullException("masterName");

            if (parameter.IsNull)
                throw new ArgumentNullException("parameter");

            if (value.IsNull)
                throw new ArgumentNullException("value");

            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.SentinelSet, parameter, value));
        }

        public RedisResult<RedisSentinelSlaveInfo[]> Slaves(string masterName)
        {
            if (masterName.IsEmpty())
                throw new ArgumentNullException("masterName");

            var array = ExpectArray(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Sentinel, RedisCommandList.SentinelSlaves, masterName.ToBytes()));
            if (!ReferenceEquals(array, null))
            {
                var items = array.Value;
                if (items != null)
                {
                    var count = items.Count;
                    if (count > 0)
                    {
                        var list = new List<RedisSentinelSlaveInfo>(count);
                        for (var i = 0; i < count; i++)
                        {
                            var item = items[i];
                            if (!ReferenceEquals(item, null) && item.Type == RedisResultType.Array)
                            {
                                var info = new RedisSentinelSlaveInfo(((RedisArray)item).ToMultiString());
                                list.Add(info);
                            }
                        }

                        if (list.Count > 0)
                            return new RedisResult<RedisSentinelSlaveInfo[]>(list.ToArray());
                    }
                }
            }
            return new RedisResult<RedisSentinelSlaveInfo[]>(null);
        }

        #endregion Methods
    }
}
