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
    internal class RedisClusterCommands : RedisCommandSet, IRedisCommandsCluster
    {
        #region .Ctors

        public RedisClusterCommands(RedisAsyncCommandExecuter executer)
            : base(executer)
        { }

        #endregion .Ctors

        #region Methods

        public RedisBool AddSlots(int slot, params int[] slots)
        {
            var parameters =  RedisCommandList.ClusterAddSlots.Join(slot.ToBytes());

            var length = slots.Length;
            if (length > 0)
            {
                for (var i = 0; i < length; i++)
                    parameters = parameters.Join(slots[i].ToBytes());
            }

            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster,  parameters));
        }

        public RedisBool DelSlots(int slot, params int[] slots)
        {
            var parameters = RedisCommandList.ClusterDelSlots.Join(slot.ToBytes());

            var length = slots.Length;
            if (length > 0)
            {
                for (var i = 0; i < length; i++)
                    parameters = parameters.Join(slots[i].ToBytes());
            }

            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, parameters));
        }

        public RedisInteger CountFailureReports(RedisParam nodeId)
        {
            if (nodeId.IsEmpty)
                throw new ArgumentNullException("nodeId");

            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterCountFailureReports, nodeId));
        }

        public RedisInteger CountKeysInSlot(int slot)
        {
            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterCountKeysInSlot, slot));
        }

        public RedisBool Failover()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterFailover));
        }

        public RedisBool FailoverForce()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterFailover, RedisCommandList.ClusterForce));
        }

        public RedisBool FailoverTakeover()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterFailover, RedisCommandList.ClusterTakeover));
        }

        public RedisBool FlushSlots()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterFlushSlots));
        }

        public RedisBool Forget(RedisParam nodeId)
        {
            if (nodeId.IsEmpty)
                throw new ArgumentNullException("nodeId");

            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterForget, nodeId));
        }

        public RedisMultiString GetKeysInSlot(int slot, int count)
        {
            var parameters = RedisCommandList.ClusterGetKeysInSlot
                .Join(slot.ToBytes())
                .Join(count.ToBytes());

            return ExpectMultiDataStrings(new RedisCommand(DbIndex, RedisCommandList.Cluster, parameters));
        }

        public RedisResult<RedisClusterInfo> Info()
        {
            var result = ExpectBulkString(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterInfo));
            if (!ReferenceEquals(result, null))
                return new RedisResult<RedisClusterInfo>(new RedisClusterInfo(result));

            return new RedisResult<RedisClusterInfo>(null);
        }

        public RedisInteger KeySlot(RedisParam key)
        {
            if (key.IsEmpty)
                throw new ArgumentNullException("key");

            return ExpectInteger(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterKeySlot, key));
        }

        public RedisBool Meet(RedisParam ip, int port)
        {
            if (ip.IsEmpty)
                throw new ArgumentNullException("ip");

            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterMeet, ip, port));
        }

        public RedisResult<RedisClusterNodeInfo[]> Nodes()
        {
            var result = ExpectBulkString(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterInfo));
            if (!ReferenceEquals(result, null))
                return new RedisResult<RedisClusterNodeInfo[]>(RedisClusterNodeInfo.Parse(result));

            return new RedisResult<RedisClusterNodeInfo[]>(null);
        }

        public RedisBool Readonly()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterReadonly));
        }

        public RedisBool ReadWrite()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterReadWrite));
        }

        public RedisBool Replicate(RedisParam nodeId)
        {
            if (nodeId.IsEmpty)
                throw new ArgumentNullException("nodeId");

            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterReplicate, nodeId));
        }

        public RedisBool ResetHard()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterReset, RedisCommandList.ClusterHard));
        }

        public RedisBool ResetSoft()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterReset, RedisCommandList.ClusterSoft));
        }

        public RedisBool SaveConfig()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterSaveConfig));
        }

        public RedisBool SetConfigEpoch(int epoch)
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterSetConfigEpoch, epoch));
        }

        public RedisBool SetSlotNode(int slot, RedisParam nodeId)
        {
            if (nodeId.IsEmpty)
                throw new ArgumentNullException("nodeId");

            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterNode, slot, nodeId));
        }

        public RedisBool SetSlotMigrating(int slot, RedisParam nodeId)
        {
            if (nodeId.IsEmpty)
                throw new ArgumentNullException("nodeId");

            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterMigrating, slot, nodeId));
        }

        public RedisBool SetSlotImporting(int slot, RedisParam nodeId)
        {
            if (nodeId.IsEmpty)
                throw new ArgumentNullException("nodeId");

            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterImporting, slot, nodeId));
        }

        public RedisBool SetSlotStable(int slot)
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterStable, slot));
        }

        public RedisResult<RedisClusterNodeInfo[]> Slaves(RedisParam nodeId)
        {
            var result = ExpectBulkString(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterSlaves));
            if (!ReferenceEquals(result, null))
                return new RedisResult<RedisClusterNodeInfo[]>(RedisClusterNodeInfo.Parse(result));

            return new RedisResult<RedisClusterNodeInfo[]>(null);
        }

        public RedisResult<RedisClusterSlotInfo[]> Slots()
        {
            var result = ExpectArray(new RedisCommand(DbIndex, RedisCommandList.Cluster, RedisCommandList.ClusterSlots));
            if (!ReferenceEquals(result, null))
                return new RedisResult<RedisClusterSlotInfo[]>(RedisClusterSlotInfo.Parse(result));

            return new RedisResult<RedisClusterSlotInfo[]>(null);
        }

        #endregion Methods
    }
}
