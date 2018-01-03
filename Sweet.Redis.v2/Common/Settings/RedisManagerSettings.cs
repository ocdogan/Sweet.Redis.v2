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
using System.Net.Security;
using System.Text;

namespace Sweet.Redis.v2
{
    public class RedisManagerSettings : RedisPoolSettings
    {
        #region Static Members

        public new static readonly RedisManagerSettings Default = new RedisManagerSettings((RedisEndPoint[])null);

        #endregion Static Members

        #region .Ctors

        public RedisManagerSettings()
            : this(endPoints: (RedisEndPoint[])null)
        { }

        public RedisManagerSettings(string host = RedisConstants.LocalHost,
            int port = RedisConstants.DefaultPort,
            RedisManagerType managerType = RedisManagerType.MasterSlave,
            string masterName = null,
            string password = null,
            string clientName = null,
            int connectionTimeout = RedisConstants.DefaultConnectionTimeout,
            int receiveTimeout = RedisConstants.DefaultReceiveTimeout,
            int sendTimeout = RedisConstants.DefaultSendTimeout,
            int maxConnectionCount = RedisConstants.DefaultMaxConnectionCount,
            int connectionWaitTimeout = RedisConstants.DefaultWaitTimeout,
            int connectionIdleTimeout = RedisConstants.DefaultIdleTimeout,
            int readBufferSize = 0,
            int writeBufferSize = 0,
            bool heartBeatEnabled = true,
            int hearBeatIntervalInSecs = RedisConstants.DefaultHeartBeatIntervalSecs,
            bool useAsyncCompleter = true,
            bool useSlaveAsMasterIfNoMasterFound = false,
            int bulkSendFactor = RedisConstants.DefaultBulkSendFactor,
            bool useBackgroundThread = false,
            bool throwOnError = true,
            bool useSsl = false,
            LocalCertificateSelectionCallback sslCertificateSelection = null,
            RemoteCertificateValidationCallback sslCertificateValidation = null)
            : this(new[] { new RedisEndPoint(host, port) }, managerType, masterName, password, clientName, connectionTimeout, receiveTimeout,
                sendTimeout, maxConnectionCount, connectionWaitTimeout, connectionIdleTimeout, readBufferSize, writeBufferSize,
                heartBeatEnabled, hearBeatIntervalInSecs, useAsyncCompleter, useSlaveAsMasterIfNoMasterFound,
                bulkSendFactor, useBackgroundThread, throwOnError, useSsl, sslCertificateSelection, sslCertificateValidation)
        { }

        public RedisManagerSettings(HashSet<RedisEndPoint> endPoints,
            RedisManagerType managerType = RedisManagerType.MasterSlave,
            string masterName = null,
            string password = null,
            string clientName = null,
            int connectionTimeout = RedisConstants.DefaultConnectionTimeout,
            int receiveTimeout = RedisConstants.DefaultReceiveTimeout,
            int sendTimeout = RedisConstants.DefaultSendTimeout,
            int maxConnectionCount = RedisConstants.DefaultMaxConnectionCount,
            int connectionWaitTimeout = RedisConstants.DefaultWaitTimeout,
            int connectionIdleTimeout = RedisConstants.DefaultIdleTimeout,
            int readBufferSize = 0,
            int writeBufferSize = 0,
            bool heartBeatEnabled = true,
            int hearBeatIntervalInSecs = RedisConstants.DefaultHeartBeatIntervalSecs,
            bool useAsyncCompleter = true,
            bool useSlaveAsMasterIfNoMasterFound = false,
            int bulkSendFactor = RedisConstants.DefaultBulkSendFactor,
            bool useBackgroundThread = false,
            bool throwOnError = true,
            bool useSsl = false,
            LocalCertificateSelectionCallback sslCertificateSelection = null,
            RemoteCertificateValidationCallback sslCertificateValidation = null)
            : this(ToEndPointList(endPoints), managerType, masterName, password, clientName, connectionTimeout, receiveTimeout,
                sendTimeout, maxConnectionCount, connectionWaitTimeout, connectionIdleTimeout, readBufferSize, writeBufferSize,
                heartBeatEnabled, hearBeatIntervalInSecs, useAsyncCompleter, useSlaveAsMasterIfNoMasterFound,
                bulkSendFactor, useBackgroundThread, throwOnError, useSsl, sslCertificateSelection, sslCertificateValidation)
        { }

        public RedisManagerSettings(RedisEndPoint[] endPoints = null,
            RedisManagerType managerType = RedisManagerType.MasterSlave,
            string masterName = null,
            string password = null,
            string clientName = null,
            int connectionTimeout = RedisConstants.DefaultConnectionTimeout,
            int receiveTimeout = RedisConstants.DefaultReceiveTimeout,
            int sendTimeout = RedisConstants.DefaultSendTimeout,
            int maxConnectionCount = RedisConstants.DefaultMaxConnectionCount,
            int connectionWaitTimeout = RedisConstants.DefaultWaitTimeout,
            int connectionIdleTimeout = RedisConstants.DefaultIdleTimeout,
            int readBufferSize = 0,
            int writeBufferSize = 0,
            bool heartBeatEnabled = true,
            int hearBeatIntervalInSecs = RedisConstants.DefaultHeartBeatIntervalSecs,
            bool useAsyncCompleter = true,
            bool useSlaveAsMasterIfNoMasterFound = false,
            int bulkSendFactor = RedisConstants.DefaultBulkSendFactor,
            bool useBackgroundThread = false,
            bool throwOnError = true,
            bool useSsl = false,
            LocalCertificateSelectionCallback sslCertificateSelection = null,
            RemoteCertificateValidationCallback sslCertificateValidation = null)
            : base(endPoints,
                masterName,
                password,
                clientName,
                connectionTimeout,
                receiveTimeout,
                sendTimeout,
                maxConnectionCount,
                connectionWaitTimeout,
                connectionIdleTimeout,
                readBufferSize,
                writeBufferSize,
                heartBeatEnabled,
                hearBeatIntervalInSecs,
                useAsyncCompleter,
                useSlaveAsMasterIfNoMasterFound,
                bulkSendFactor,
                useBackgroundThread,
                throwOnError,
                useSsl,
                sslCertificateSelection,
                sslCertificateValidation)
        {
            ManagerType = managerType;
        }

        #endregion .Ctors

        #region Properties

        public RedisManagerType ManagerType { get; private set; }

        # endregion Properties

        #region Methods

        public override RedisConnectionSettings Clone(string host = null, int port = -1)
        {
            return new RedisManagerSettings(host ?? RedisConstants.LocalHost,
                            port < 1 ? RedisConstants.DefaultPort : port,
                            ManagerType,
                            MasterName,
                            Password,
                            ClientName,
                            ConnectionTimeout,
                            ReceiveTimeout,
                            SendTimeout,
                            MaxConnectionCount,
                            ConnectionWaitTimeout,
                            ConnectionIdleTimeout,
                            ReadBufferSize,
                            WriteBufferSize,
                            HeartBeatEnabled,
                            HearBeatIntervalInSecs,
                            UseAsyncCompleter,
                            UseSlaveAsMasterIfNoMasterFound,
                            BulkSendFactor,
                            UseBackgroundThread, 
                            ThrowOnError, 
                            UseSsl,
                            SslCertificateSelection,
                            SslCertificateValidation);
        }

        protected override void WriteTo(StringBuilder sBuilder)
        {
            base.WriteTo(sBuilder);

            if (ManagerType != RedisManagerType.MasterSlave)
            {
                sBuilder.Append("managerType=");
                sBuilder.Append(ManagerType.ToString("F"));
                sBuilder.Append(';');
            }
        }

        #region Settings

        protected override int GetDefaultPort()
        {
            return RedisConstants.DefaultSentinelPort;
        }

        protected override void SetSettings(IDictionary<string, object> settings)
        {
            base.SetSettings(settings);

            object obj;
            if (settings.TryGetValue("managertype", out obj))
                ManagerType = (RedisManagerType)obj;
        }

        protected override IDictionary<string, object> GetSettingsWithDefaults()
        {
            var settings = base.GetSettingsWithDefaults();
            settings["managertype"] = RedisManagerType.MasterSlave;
            return settings;
        }

        protected override bool ParseProperty(IDictionary<string, object> settings, string key, string value)
        {
            if (base.ParseProperty(settings, key, value))
                return true;

            if (key == "managertype")
            {
                settings[key] = Enum.Parse(typeof(RedisManagerType), value);
                return true;
            }
            return true;
        }

        #endregion Settings

        #endregion Methods
    }
}
