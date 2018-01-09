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
using System.Linq;
using System.Net;
using System.Threading;

namespace Sweet.Redis.v2
{
    internal class RedisEndPointResolver : RedisDisposable, IRedisNamedObject
    {
        #region NodeRoleAndSiblings

        protected class NodeRoleAndSiblings
        {
            #region .Ctors

            public NodeRoleAndSiblings(RedisRole role, RedisEndPoint[] siblings)
            {
                Role = role;
                Siblings = siblings;
            }

            #endregion .Ctors

            #region Properties

            public RedisRole Role { get; private set; }

            public RedisEndPoint[] Siblings { get; private set; }

            #endregion Properties
        }

        #endregion NodeRoleAndSiblings

        #region GroupedClients

        protected class GroupedClients
        {
            #region .Ctors

            public GroupedClients(RedisAsyncClient[] masters, RedisAsyncClient[] slaves, RedisAsyncClient[] sentinels)
            {
                Masters = masters;
                Slaves = slaves;
                Sentinels = sentinels;
            }

            #endregion .Ctors

            #region Properties

            public RedisAsyncClient[] Masters { get; private set; }

            public RedisAsyncClient[] Slaves { get; private set; }

            public RedisAsyncClient[] Sentinels { get; private set; }

            #endregion Properties
        }

        #endregion GroupedClients

        #region Field Members

        private string m_Name;
        private RedisConnectionSettings m_Settings;

        #endregion Field Members

        #region .Ctors

        public RedisEndPointResolver(string name, RedisConnectionSettings settings)
        {
            if (settings == null)
                throw new RedisFatalException(new ArgumentNullException("settings"), RedisErrorCode.MissingParameter);

            m_Name = name;
            m_Settings = settings;
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
            Interlocked.Exchange(ref m_Settings, null);
        }

        #endregion Destructors

        #region Properties

        public string Name { get { return m_Name; } }

        public RedisConnectionSettings Settings { get { return m_Settings; } }

        #endregion Properties

        #region Methods

        protected GroupedClients CreateGroupSockets()
        {
            if (!Disposed)
            {
                var settings = m_Settings;
                var ipEPList = RedisEndPoint.ToIPEndPoints(settings.EndPoints);

                if (ipEPList != null && ipEPList.Count > 0)
                {
                    var ipEPSettings = ipEPList
                            .Select(ep => (RedisConnectionSettings)settings.Clone(ep.Address.ToString(), ep.Port))
                            .ToArray();

                    if (!ipEPSettings.IsEmpty())
                    {
                        var discoveredEndPoints = new HashSet<IPEndPoint>();
                        var emptyTuple = new Tuple<RedisRole, RedisAsyncClient>[0];

                        var groupsTuples = ipEPSettings
                            .SelectMany(setting => CreateNodes(discoveredEndPoints, setting) ?? emptyTuple)
                            .Where(node => node != null)
                            .GroupBy(
                                tuple => tuple.Item1,
                                tuple => tuple.Item2,
                                (role, group) => new Tuple<RedisRole, RedisAsyncClient[]>(role, group.ToArray()))
                            .ToList();

                        if (!groupsTuples.IsEmpty())
                        {
                            // 0: Masters, 1: Slaves, 2: Sentinels
                            const int MastersPos = 0, SlavesPos = 1, SentinelsPos = 2;

                            var result = new RedisAsyncClient[3][];
                            foreach (var tuple in groupsTuples)
                            {
                                switch (tuple.Item1)
                                {
                                    case RedisRole.Master:
                                        result[MastersPos] = tuple.Item2;
                                        break;
                                    case RedisRole.Slave:
                                        result[SlavesPos] = tuple.Item2;
                                        break;
                                    case RedisRole.Sentinel:
                                        result[SentinelsPos] = tuple.Item2;
                                        break;
                                    default:
                                        break;
                                }
                            }

                            return new GroupedClients(result[MastersPos], result[SlavesPos], result[SentinelsPos]);
                        }
                    }
                }
            }
            return null;
        }

        protected virtual RedisAsyncClient NewClient(RedisConnectionSettings settings)
        {
            return new RedisAsyncClient(settings);
        }

        protected Tuple<RedisRole, RedisEndPoint[], RedisAsyncClient> DiscoverNode(RedisConnectionSettings settings)
        {
            var client = NewClient(settings);

            var nodeInfo = GetNodeInfo(settings.MasterName, client);
            if (!(nodeInfo == null || nodeInfo.Role == RedisRole.Undefined))
            {
                var role = nodeInfo.Role;
                var siblingEndPoints = nodeInfo.Siblings;

                return new Tuple<RedisRole, RedisEndPoint[], RedisAsyncClient>(role, siblingEndPoints, client);
            }
            return null;
        }

        private Tuple<RedisRole, RedisAsyncClient>[] CreateNodes(HashSet<IPEndPoint> discoveredEndPoints, RedisConnectionSettings settings)
        {
            try
            {
                if (Disposed)
                    return null;

                var endPoints = settings.EndPoints;
                if (endPoints.IsEmpty())
                    return null;

                var ipAddresses = endPoints[0].ResolveHost();
                if (ipAddresses.IsEmpty())
                    return null;

                var nodeEndPoint = new IPEndPoint(ipAddresses[0], endPoints[0].Port);
                if (discoveredEndPoints.Contains(nodeEndPoint))
                    return null;

                discoveredEndPoints.Add(nodeEndPoint);

                var nodeInfo = DiscoverNode(settings);

                if (nodeInfo != null)
                {
                    var role = nodeInfo.Item1;
                    var siblingEndPoints = nodeInfo.Item2;
                    var client = nodeInfo.Item3;

                    var list = new List<Tuple<RedisRole, RedisAsyncClient>>();
                    if (client != null)
                        list.Add(new Tuple<RedisRole, RedisAsyncClient>(role, client));

                    if (role != RedisRole.Undefined && !siblingEndPoints.IsEmpty())
                    {
                        foreach (var siblingEndPoint in siblingEndPoints)
                        {
                            try
                            {
                                if (!Disposed && siblingEndPoint != null && !siblingEndPoint.Host.IsEmpty())
                                {
                                    var siblingSettings = (RedisConnectionSettings)settings.Clone(siblingEndPoint.Host, siblingEndPoint.Port);

                                    var otherNodes = CreateNodes(discoveredEndPoints, siblingSettings);
                                    if (otherNodes != null)
                                        list.AddRange(otherNodes);
                                }
                            }
                            catch (Exception)
                            { }
                        }
                    }

                    return list.ToArray();
                }
            }
            catch (Exception)
            { }
            return null;
        }

        protected NodeRoleAndSiblings GetNodeInfo(string masterName, RedisAsyncClient client)
        {
            if (client != null)
            {
                try
                {
                    var serverInfo = client.GetServerInfo();
                    if (serverInfo != null)
                    {
                        var role = DiscoverRoleOfNode(serverInfo);

                        switch (role)
                        {
                            case RedisRole.Slave:
                                return GetMasterOfSlave(serverInfo) ?? new NodeRoleAndSiblings(role, null);
                            case RedisRole.Master:
                                return GetSlavesOfMaster(serverInfo) ?? new NodeRoleAndSiblings(role, null);
                            case RedisRole.Sentinel:
                                {
                                    var masters = GetMastersOfSentinel(masterName, serverInfo);
                                    if (!Disposed)
                                    {
                                        var otherSentinels = GetSiblingSentinelsOfSentinel(masterName, client);
                                        if (!Disposed)
                                        {
                                            if (otherSentinels == null || otherSentinels.Siblings == null)
                                                return masters ?? new NodeRoleAndSiblings(role, null);

                                            if (masters == null || masters.Siblings == null)
                                                return otherSentinels ?? new NodeRoleAndSiblings(role, null);

                                            var siblings = new HashSet<RedisEndPoint>(masters.Siblings);
                                            siblings.UnionWith(otherSentinels.Siblings);

                                            return new NodeRoleAndSiblings(role, siblings.ToArray());
                                        }
                                    }
                                    break;
                                }
                            default:
                                break;
                        }
                    }
                }
                catch (Exception)
                { }
            }
            return null;
        }

        private RedisRole DiscoverRoleOfNode(RedisServerInfo serverInfo)
        {
            if (serverInfo != null)
            {
                var serverSection = serverInfo.Server;
                if (serverSection != null)
                {
                    var redisMode = (serverSection.RedisMode ?? String.Empty).Trim().ToLowerInvariant();
                    if (redisMode == "sentinel")
                        return RedisRole.Sentinel;

                    var replicationSection = serverInfo.Replication;
                    if (replicationSection != null)
                        return replicationSection.Role.ToRedisRole();
                }
            }
            return RedisRole.Undefined;
        }

        private NodeRoleAndSiblings GetMasterOfSlave(RedisServerInfo serverInfo)
        {
            if (serverInfo != null)
            {
                var replicationSection = serverInfo.Replication;
                if (replicationSection != null)
                {
                    try
                    {
                        if (replicationSection.MasterPort.HasValue &&
                            !replicationSection.MasterHost.IsEmpty())
                        {
                            var endPoint = new RedisEndPoint(replicationSection.MasterHost,
                                                             (int)replicationSection.MasterPort.Value);
                            return new NodeRoleAndSiblings(RedisRole.Slave, new[] { endPoint });

                        }
                    }
                    catch (Exception)
                    { }
                }
            }
            return null;
        }

        private NodeRoleAndSiblings GetSlavesOfMaster(RedisServerInfo serverInfo)
        {
            if (serverInfo != null)
            {
                var replicationSection = serverInfo.Replication;
                if (replicationSection != null)
                {
                    var slaves = replicationSection.Slaves;
                    if (slaves != null)
                    {
                        var slaveEndPoints = new List<RedisEndPoint>();

                        foreach (var slave in slaves)
                        {
                            try
                            {
                                if (slave.Port.HasValue && !slave.IPAddress.IsEmpty())
                                {
                                    var endPoint = new RedisEndPoint(slave.IPAddress, slave.Port.Value);
                                    slaveEndPoints.Add(endPoint);
                                }
                            }
                            catch (Exception)
                            { }
                        }

                        return new NodeRoleAndSiblings(RedisRole.Master, slaveEndPoints.ToArray());
                    }
                }
            }
            return null;
        }

        private NodeRoleAndSiblings GetMastersOfSentinel(string masterName, RedisServerInfo serverInfo)
        {
            if (serverInfo != null)
            {
                var sentinelSection = serverInfo.Sentinel;
                if (sentinelSection != null)
                {
                    var masters = sentinelSection.Masters;
                    if (masters != null)
                    {
                        var mastersLength = masters.Length;
                        if (mastersLength > 0)
                        {
                            if (masterName.IsEmpty())
                            {
                                if (mastersLength == 1)
                                {
                                    var master = masters[0];
                                    try
                                    {
                                        if (master.Port.HasValue && !master.IPAddress.IsEmpty())
                                        {
                                            var endPoint = new RedisEndPoint(master.IPAddress, master.Port.Value);
                                            return new NodeRoleAndSiblings(RedisRole.Sentinel, new[] { endPoint });
                                        }
                                    }
                                    catch (Exception)
                                    { }
                                }
                                return null;
                            }

                            var masterEndPoints = new List<RedisEndPoint>();

                            foreach (var master in masters)
                            {
                                try
                                {
                                    if (master.Name == masterName &&
                                        master.Port.HasValue && !master.IPAddress.IsEmpty())
                                    {
                                        var endPoint = new RedisEndPoint(master.IPAddress, master.Port.Value);
                                        masterEndPoints.Add(endPoint);
                                    }
                                }
                                catch (Exception)
                                { }
                            }

                            return new NodeRoleAndSiblings(RedisRole.Sentinel, masterEndPoints.ToArray());
                        }
                    }
                }
            }
            return null;
        }

        private NodeRoleAndSiblings GetSiblingSentinelsOfSentinel(string masterName, RedisAsyncClient client)
        {
            if (masterName.IsEmpty())
                throw new ArgumentNullException("masterName");

            try
            {
                var response = client.Expect(new RedisCommand(-1, RedisCommandList.Sentinel, RedisCommandType.SendAndReceive,
                                RedisCommandList.Sentinels, masterName.ToBytes()));

                if (!ReferenceEquals(response, null) && response.Type == RedisResultType.Array)
                {
                    var result = response as RedisArray;
                    if (!ReferenceEquals(result, null))
                    {
                        var sentinelInfos = RedisSentinelNodeInfo.ParseInfoResponse(result);
                        if (sentinelInfos != null)
                        {
                            var length = sentinelInfos.Length;
                            if (length > 0)
                            {
                                var siblingEndPoints = new List<RedisEndPoint>(length);
                                for (var i = 0; i < length; i++)
                                {
                                    var sentinelInfo = sentinelInfos[i];

                                    if (!Disposed && sentinelInfo != null && sentinelInfo.Port.HasValue && !sentinelInfo.IPAddress.IsEmpty())
                                        siblingEndPoints.Add(new RedisEndPoint(sentinelInfo.IPAddress, (int)sentinelInfo.Port.Value));
                                }

                                if (!Disposed)
                                    return new NodeRoleAndSiblings(RedisRole.Sentinel, siblingEndPoints.ToArray());
                            }
                        }
                    }
                }
            }
            catch (Exception)
            { }
            return null;
        }

        #endregion Methods
    }
}
