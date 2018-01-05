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
using System.Net.Sockets;

namespace Sweet.Redis.v2
{
    internal class RedisManagedEndPointResolver : RedisEndPointResolver
    {
        #region .Ctors

        public RedisManagedEndPointResolver(string name, RedisManagerSettings settings)
            : base(name, settings)
        { }

        #endregion .Ctors

        #region Methods

        public Tuple<RedisManagedMSGroup, RedisManagedSentinelGroup> CreateGroups()
        {
            ValidateNotDisposed();

            var tuple = CreateGroupSockets();
            if (tuple != null)
            {
                RedisManagedSentinelGroup sentinels = null;
                RedisManagedMSGroup mastersAndSlaves = null;

                RedisManagedServerGroup slaves = null;
                RedisManagedServerGroup masters = null;
                try
                {
                    masters = (RedisManagedServerGroup)ToNodesGroup(RedisRole.Master, tuple.Masters);
                    try
                    {
                        slaves = (RedisManagedServerGroup)ToNodesGroup(RedisRole.Slave, tuple.Slaves);
                        try
                        {
                            sentinels = (RedisManagedSentinelGroup)ToNodesGroup(RedisRole.Sentinel, tuple.Sentinels);
                        }
                        catch (Exception)
                        {
                            if (sentinels != null)
                            {
                                sentinels.Dispose();
                                sentinels = null;
                            }
                            throw;
                        }
                    }
                    catch (Exception)
                    {
                        if (slaves != null)
                        {
                            slaves.Dispose();
                            slaves = null;
                        }
                        throw;
                    }
                }
                catch (Exception)
                {
                    if (masters != null)
                    {
                        masters.Dispose();
                        masters = null;
                    }
                    throw;
                }

                mastersAndSlaves = new RedisManagedMSGroup((RedisManagerSettings)Settings, masters, slaves);
                return new Tuple<RedisManagedMSGroup, RedisManagedSentinelGroup>(mastersAndSlaves, sentinels);
            }
            return null;
        }

        public Tuple<RedisRole, RedisEndPoint[], RedisAsyncClient> DiscoverNode(RedisEndPoint endPoint)
        {
            if (endPoint.IsEmpty())
                throw new RedisFatalException(new ArgumentNullException("endPoint"), RedisErrorCode.MissingParameter);

            ValidateNotDisposed();

            var settings = Settings.Clone(endPoint.Host, endPoint.Port);

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

        private RedisManagedNodesGroup ToNodesGroup(RedisRole role, RedisAsyncClient[] clients)
        {
            if (!clients.IsEmpty())
            {
                var baseSettings = Settings;

                var nodeList = new List<RedisManagedNode>();
                foreach (var client in clients)
                {
                    try
                    {
                        if (client != null && client.Connected)
                        {
                            IPEndPoint ipEP = null;

                            var endPoint = client.EndPoint;
                            if (endPoint != null)
                            {
                                ipEP = endPoint as IPEndPoint;
                                if (ipEP == null)
                                {
                                    var dnsEP = endPoint as DnsEndPoint;
                                    if (dnsEP != null)
                                    {
                                        var ipEPs = (new RedisEndPoint(dnsEP.Host, dnsEP.Port)).ResolveHost();
                                        if (!ipEPs.IsEmpty())
                                            ipEP = new IPEndPoint(ipEPs[0], dnsEP.Port);
                                    }
                                }
                            }

                            if (ipEP == null)
                                client.Dispose();
                            else
                            {
                                var settings = (RedisManagerSettings)baseSettings.Clone(ipEP.Address.ToString(), ipEP.Port);

                                if (role == RedisRole.Sentinel)
                                {
                                    var listener = new RedisManagedSentinelListener(settings, null);
                                    // TODO
                                    // listener.ReuseSocket(client);

                                    nodeList.Add(new RedisManagedSentinelNode(settings, listener, null));
                                }
                                else
                                {
                                    var server = new RedisManagedServer(settings, role, null);
                                    // TODO
                                    // server.ReuseSocket(client);

                                    nodeList.Add(new RedisManagedServerNode(settings, role, server, null));
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        client.Dispose();
                    }
                }

                if (nodeList.Count > 0)
                {
                    var settings = (RedisManagerSettings)Settings;

                    return role != RedisRole.Sentinel ?
                            (RedisManagedNodesGroup)(new RedisManagedServerGroup(settings, role, nodeList.Cast<RedisManagedServerNode>().ToArray(), null)) :
                            new RedisManagedSentinelGroup(settings, settings.MasterName, nodeList.Cast<RedisManagedSentinelNode>().ToArray(), null);
                }
            }
            return null;
        }

        #endregion Methods
    }
}
