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
    public class RedisPoolSettings : RedisConnectionSettings
    {
        #region Static Members

        public new static readonly RedisPoolSettings Default = new RedisPoolSettings((RedisEndPoint[])null);

        #endregion Static Members

        #region .Ctors

        public RedisPoolSettings()
            : this(endPoints: (RedisEndPoint[])null)
        { }

        public RedisPoolSettings(string host = RedisConstants.LocalHost,
            int port = RedisConstants.DefaultPort,
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
            : this(new[] { new RedisEndPoint(host, port) }, 
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
        { }

        public RedisPoolSettings(HashSet<RedisEndPoint> endPoints,
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
            : this(ToEndPointList(endPoints), 
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
        { }

        public RedisPoolSettings(RedisEndPoint[] endPoints = null,
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
                connectionWaitTimeout, 
                readBufferSize, 
                writeBufferSize, 
                heartBeatEnabled, 
                hearBeatIntervalInSecs,
                bulkSendFactor, 
                useBackgroundThread, 
                throwOnError, 
                useSsl, 
                sslCertificateSelection, 
                sslCertificateValidation)
        {
            UseAsyncCompleter = useAsyncCompleter;
            UseSlaveAsMasterIfNoMasterFound = useSlaveAsMasterIfNoMasterFound;
            ConnectionIdleTimeout = connectionIdleTimeout <= 0 ? 0 : Math.Max(RedisConstants.MinIdleTimeout, Math.Min(RedisConstants.MaxIdleTimeout, connectionIdleTimeout));
            MaxConnectionCount = Math.Max(Math.Min(maxConnectionCount, RedisConstants.MaxConnectionCount), RedisConstants.MinConnectionCount);
        }

        #endregion .Ctors

        #region Properties

        public int ConnectionIdleTimeout { get; private set; }

        public int MaxConnectionCount { get; private set; }

        public bool UseAsyncCompleter { get; private set; }

        public bool UseSlaveAsMasterIfNoMasterFound { get; private set; }

        # endregion Properties

        #region Methods

        public override RedisConnectionSettings Clone(string host = null, int port = -1)
        {
            return new RedisPoolSettings(host ?? RedisConstants.LocalHost,
                            port < 1 ? RedisConstants.DefaultPort : port,
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

            if (ConnectionIdleTimeout != RedisConstants.DefaultIdleTimeout)
            {
                sBuilder.Append("connectionIdleTimeout=");
                sBuilder.Append(ConnectionIdleTimeout);
                sBuilder.Append(';');
            }

            if (MaxConnectionCount != RedisConstants.DefaultMaxConnectionCount)
            {
                sBuilder.Append("maxConnectionCount=");
                sBuilder.Append(MaxConnectionCount);
                sBuilder.Append(';');
            }

            if (!UseAsyncCompleter)
            {
                sBuilder.Append("useAsyncCompleter=");
                sBuilder.Append(UseAsyncCompleter);
                sBuilder.Append(';');
            }

            if (UseSlaveAsMasterIfNoMasterFound)
            {
                sBuilder.Append("useSlaveAsMasterIfNoMasterFound=");
                sBuilder.Append(UseSlaveAsMasterIfNoMasterFound);
                sBuilder.Append(';');
            }
        }

        #region Settings

        protected override void SetSettings(IDictionary<string, object> settings)
        {
            base.SetSettings(settings);

            foreach (var kv in settings)
            {
                switch (kv.Key)
                {
                    case "connectionidletimeout":
                        ConnectionIdleTimeout = (int)kv.Value;
                        break;
                    case "maxconnectioncount":
                        MaxConnectionCount = (int)kv.Value;
                        break;
                    case "useasynccompleter":
                        UseAsyncCompleter = (bool)kv.Value;
                        break;
                    case "useslaveasmasterifnomasterfound":
                        UseSlaveAsMasterIfNoMasterFound = (bool)kv.Value;
                        break;
                    default:
                        break;
                }
            }
        }

        protected override IDictionary<string, object> GetSettingsWithDefaults()
        {
            var settings = base.GetSettingsWithDefaults();

            settings["connectionidletimeout"] = RedisConstants.DefaultIdleTimeout;
            settings["maxconnectioncount"] = RedisConstants.DefaultMaxConnectionCount;
            settings["useasynccompleter"] = true;
            settings["useslaveasmasterifnomasterfound"] = false;

            return settings;
        }

        protected override bool ParseProperty(IDictionary<string, object> settings, string key, string value)
        {
            if (base.ParseProperty(settings, key, value))
                return true;

            switch (key)
            {
                case "connectionidletimeout":
                    settings[key] = int.Parse(value);
                    break;
                case "maxconnectioncount":
                    settings[key] = int.Parse(value);
                    break;
                case "useasynccompleter":
                    settings[key] = bool.Parse(value);
                    break;
                case "useslaveasmasterifnomasterfound":
                    settings[key] = bool.Parse(value);
                    break;
                default:
                    return false;
            }
            return true;
        }

        #endregion Settings

        #endregion Methods
    }
}
