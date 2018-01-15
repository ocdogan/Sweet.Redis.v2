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

using System.Collections.Generic;
using System.Net.Security;

namespace Sweet.Redis.v2
{
    public class RedisSentinelSettings : RedisConnectionSettings
    {
        #region Static Members

        public new static readonly RedisSentinelSettings Default = new RedisSentinelSettings((RedisEndPoint[])null);

        #endregion Static Members

        #region .Ctors

        public RedisSentinelSettings()
            : this(endPoints: (RedisEndPoint[])null)
        { }

        public RedisSentinelSettings(string connectionString)
            : base(connectionString)
        { }

        public RedisSentinelSettings(string host = RedisConstants.LocalHost,
            int port = RedisConstants.DefaultSentinelPort,
            string masterName = null,
            string password = null,
            string clientName = null,
            int connectionCount = RedisConstants.DefaultConnectionCount,
            int connectionTimeout = RedisConstants.DefaultConnectionTimeout,
            int receiveTimeout = RedisConstants.DefaultReceiveTimeout,
            int sendTimeout = RedisConstants.DefaultSendTimeout,
            int connectionWaitTimeout = RedisConstants.DefaultWaitTimeout,
            int readBufferSize = 0,
            int writeBufferSize = 0,
            bool heartBeatEnabled = true,
            int hearBeatIntervalInSecs = RedisConstants.DefaultHeartBeatIntervalSecs,
            int bulkSendFactor = RedisConstants.DefaultBulkSendFactor,
            bool useBackgroundThread = false,
            bool useSsl = false,
            LocalCertificateSelectionCallback sslCertificateSelection = null,
            RemoteCertificateValidationCallback sslCertificateValidation = null)
            : this(new[] { new RedisEndPoint(host, port) }, masterName, password, clientName, connectionCount, connectionTimeout, 
                receiveTimeout, sendTimeout, connectionWaitTimeout, readBufferSize, writeBufferSize, heartBeatEnabled, hearBeatIntervalInSecs,
                bulkSendFactor, useBackgroundThread, useSsl, sslCertificateSelection, sslCertificateValidation)
        { }

        public RedisSentinelSettings(HashSet<RedisEndPoint> endPoints,
            string masterName = null,
            string password = null,
            string clientName = null,
            int connectionCount = RedisConstants.DefaultConnectionCount,
            int connectionTimeout = RedisConstants.DefaultConnectionTimeout,
            int receiveTimeout = RedisConstants.DefaultReceiveTimeout,
            int sendTimeout = RedisConstants.DefaultSendTimeout,
            int connectionWaitTimeout = RedisConstants.DefaultWaitTimeout,
            int readBufferSize = 0,
            int writeBufferSize = 0,
            bool heartBeatEnabled = true,
            int hearBeatIntervalInSecs = RedisConstants.DefaultHeartBeatIntervalSecs,
            int bulkSendFactor = RedisConstants.DefaultBulkSendFactor,
            bool useBackgroundThread = false,
            bool useSsl = false,
            LocalCertificateSelectionCallback sslCertificateSelection = null,
            RemoteCertificateValidationCallback sslCertificateValidation = null)
            : this(ToEndPointList(endPoints, RedisConstants.DefaultSentinelPort), masterName, password, clientName, connectionCount, 
                connectionTimeout, receiveTimeout, sendTimeout, connectionWaitTimeout, readBufferSize, writeBufferSize,
                heartBeatEnabled, hearBeatIntervalInSecs, bulkSendFactor, useBackgroundThread, useSsl, sslCertificateSelection, sslCertificateValidation)
        { }

        public RedisSentinelSettings(RedisEndPoint[] endPoints = null,
            string masterName = null,
            string password = null,
            string clientName = null,
            int connectionCount = RedisConstants.DefaultConnectionCount,
            int connectionTimeout = RedisConstants.DefaultConnectionTimeout,
            int receiveTimeout = RedisConstants.DefaultReceiveTimeout,
            int sendTimeout = RedisConstants.DefaultSendTimeout,
            int connectionWaitTimeout = RedisConstants.DefaultWaitTimeout,
            int readBufferSize = 0,
            int writeBufferSize = 0,
            bool heartBeatEnabled = true,
            int hearBeatIntervalInSecs = RedisConstants.DefaultHeartBeatIntervalSecs,
            int bulkSendFactor = RedisConstants.DefaultBulkSendFactor,
            bool useBackgroundThread = false,
            bool useSsl = false,
            LocalCertificateSelectionCallback sslCertificateSelection = null,
            RemoteCertificateValidationCallback sslCertificateValidation = null)
            : base(!endPoints.IsEmpty() ? endPoints : new[] { new RedisEndPoint(RedisConstants.LocalHost, RedisConstants.DefaultPort) },
                   masterName, password, clientName, connectionCount, connectionTimeout, receiveTimeout, sendTimeout,
                   connectionWaitTimeout, readBufferSize, writeBufferSize, heartBeatEnabled, hearBeatIntervalInSecs,
                   bulkSendFactor, useBackgroundThread, useSsl, sslCertificateSelection, sslCertificateValidation)
        { }

        #endregion .Ctors

        #region Methods

        public override RedisConnectionSettings Clone(string host = null, int port = -1)
        {
            return new RedisSentinelSettings(
                            host ?? RedisConstants.LocalHost,
                            port < 1 ? RedisConstants.DefaultSentinelPort : port,
                            MasterName,
                            Password,
                            ClientName,
                            ConnectionCount,
                            ConnectionTimeout,
                            ReceiveTimeout,
                            SendTimeout,
                            ConnectionWaitTimeout,
                            ReadBufferSize,
                            WriteBufferSize,
                            HeartBeatEnabled,
                            HearBeatIntervalInSecs,
                            BulkSendFactor,
                            UseBackgroundThread, 
                            UseSsl,
                            SslCertificateSelection,
                            SslCertificateValidation);
        }

        #region Settings

        protected override int GetDefaultPort()
        {
            return RedisConstants.DefaultSentinelPort;
        }

        #endregion Settings

        #endregion Methods
    }
}
