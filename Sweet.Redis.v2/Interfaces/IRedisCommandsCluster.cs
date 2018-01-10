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

namespace Sweet.Redis.v2
{
    /*
    CLUSTER ADDSLOTS slot [slot ...]
    summary: Assign new hash slots to receiving node
    since: 3.0.0

    CLUSTER COUNT-FAILURE-REPORTS node-id
    summary: Return the number of failure reports active for a given node
    since: 3.0.0

    CLUSTER COUNTKEYSINSLOT slot
    summary: Return the number of local keys in the specified hash slot
    since: 3.0.0

    CLUSTER DELSLOTS slot [slot ...]
    summary: Set hash slots as unbound in receiving node
    since: 3.0.0

    CLUSTER FAILOVER [FORCE|TAKEOVER]
    summary: Forces a slave to perform a manual failover of its master.
    since: 3.0.0

    CLUSTER FORGET node-id
    summary: Remove a node from the nodes table
    since: 3.0.0

    CLUSTER GETKEYSINSLOT slot count
    summary: Return local key names in the specified hash slot
    since: 3.0.0

    CLUSTER INFO -
    summary: Provides info about Redis Cluster node state
    since: 3.0.0

    CLUSTER KEYSLOT key
    summary: Returns the hash slot of the specified key
    since: 3.0.0

    CLUSTER MEET ip port
    summary: Force a node  to handshake with another node
    since: 3.0.0

    CLUSTER NODES -
    summary: Get Cluster config for the node
    since: 3.0.0

    CLUSTER REPLICATE node-id
    summary: Reconfigure a node as a slave of the specified master node
    since: 3.0.0

    CLUSTER RESET [HARD|SOFT]
    summary: Reset a Redis Cluster node
    since: 3.0.0

    CLUSTER SAVECONFIG -
    summary: Forces the node to save  state on disk
    since: 3.0.0

    CLUSTER SET-CONFIG-EPOCH config-epoch
    summary: Set the configuration epoch in a new node
    since: 3.0.0

    CLUSTER SETSLOT slot IMPORTING|MIGRATING|STABLE|NODE [node-id]
    summary: Bind a hash slot to a specific node
    since: 3.0.0

    CLUSTER SLAVES node-id
    summary: List slave nodes of the specified master node
    since: 3.0.0

    CLUSTER SLOTS -
    summary: Get array of Cluster slot to node mappings
    since: 3.0.0

    READONLY -
    summary: Enables read queries for a connection to a  slave node
    since: 3.0.0

    READWRITE -
    summary: Disables read queries for a connection to a  slave node
    since: 3.0.0
    */
    public interface IRedisCommandsCluster
    {
        RedisBool AddSlots(int slot, params int[] slots);
        RedisBool DelSlots(int slot, params int[] slots);

        RedisInteger CountFailureReports(RedisParam nodeId);
        RedisInteger CountKeysInSlot(int slot);

        RedisBool Failover();
        RedisBool FailoverForce();
        RedisBool FailoverTakeover();

        RedisBool FlushSlots();

        RedisBool Forget(RedisParam nodeId);

        RedisMultiString GetKeysInSlot(int slot, int count);

        RedisResult<RedisClusterInfo> Info();

        RedisInteger KeySlot(RedisParam key);

        RedisBool Meet(RedisParam ip, int port);

        RedisResult<RedisClusterNodeInfo[]> Nodes();

        RedisBool Readonly();

        RedisBool ReadWrite();

        RedisBool Replicate(RedisParam nodeId);

        RedisBool ResetHard();
        RedisBool ResetSoft();

        RedisBool SaveConfig();

        RedisBool SetConfigEpoch(int epoch);

        RedisBool SetSlotNode(int slot, RedisParam nodeId);
        RedisBool SetSlotMigrating(int slot, RedisParam nodeId);
        RedisBool SetSlotImporting(int slot, RedisParam nodeId);
        RedisBool SetSlotStable(int slot);

        RedisResult<RedisClusterNodeInfo[]> Slaves(RedisParam nodeId);

        RedisResult<RedisClusterSlotInfo[]> Slots();
    }
}
