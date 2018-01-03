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
using System.Linq;
using System.Net.Security;
using System.Text;

namespace Sweet.Redis.v2
{
    public class RedisConnectionSettings
    {
        #region Static Members

        public static readonly RedisConnectionSettings Default = new RedisConnectionSettings((RedisEndPoint[])null);

        #endregion Static Members

        #region Field Members

        private uint? m_CRCHash;

        #endregion Field Members

        #region .Ctors

        public RedisConnectionSettings()
            : this(endPoints: (RedisEndPoint[])null)
        { }

        public RedisConnectionSettings(string host = RedisConstants.LocalHost,
            int port = RedisConstants.DefaultPort,
            string masterName = null,
            string password = null,
            string clientName = null,
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
        { }

        public RedisConnectionSettings(HashSet<RedisEndPoint> endPoints = null,
            string masterName = null,
            string password = null,
            string clientName = null,
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
        { }

        public RedisConnectionSettings(RedisEndPoint[] endPoints = null,
            string masterName = null,
            string password = null,
            string clientName = null,
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
            bool throwOnError = true,
            bool useSsl = false,
            LocalCertificateSelectionCallback sslCertificateSelection = null,
            RemoteCertificateValidationCallback sslCertificateValidation = null)
        {
            EndPoints = !endPoints.IsEmpty() ? endPoints :
                new[] { new RedisEndPoint(RedisConstants.LocalHost, RedisConstants.DefaultPort) };
            UseSsl = useSsl;
            Password = password;
            ClientName = clientName;
            MasterName = masterName;
            ThrowOnError = throwOnError;
            SslCertificateSelection = sslCertificateSelection;
            SslCertificateValidation = sslCertificateValidation;
            BulkSendFactor = (bulkSendFactor < 1 ? RedisConstants.DefaultBulkSendFactor : Math.Min(bulkSendFactor, RedisConstants.MaxBulkSendFactor));
            ConnectionTimeout = Math.Max(RedisConstants.MinConnectionTimeout, Math.Min(RedisConstants.MaxConnectionTimeout, connectionTimeout));
            ConnectionWaitTimeout = Math.Max(RedisConstants.MinWaitTimeout, Math.Min(RedisConstants.MaxWaitTimeout, connectionWaitTimeout));
            ReceiveTimeout = Math.Max(RedisConstants.MinReceiveTimeout, Math.Min(RedisConstants.MaxReceiveTimeout, receiveTimeout));
            SendTimeout = Math.Max(RedisConstants.MinSendTimeout, Math.Min(RedisConstants.MaxSendTimeout, sendTimeout));
            ReadBufferSize = Math.Max(0, readBufferSize);
            WriteBufferSize = Math.Max(0, writeBufferSize);
            HeartBeatEnabled = heartBeatEnabled;
            HearBeatIntervalInSecs = Math.Max(RedisConstants.MinHeartBeatIntervalSecs, Math.Min(RedisConstants.MaxHeartBeatIntervalSecs, hearBeatIntervalInSecs));
        }

        #endregion .Ctors

        #region Properties

        public int BulkSendFactor { get; private set; }

        public string ClientName { get; private set; }

        public int ConnectionTimeout { get; private set; }

        public int ConnectionWaitTimeout { get; private set; }

        public RedisEndPoint[] EndPoints { get; private set; }

        public bool HeartBeatEnabled { get; private set; }

        public int HearBeatIntervalInSecs { get; private set; }

        public string MasterName { get; private set; }

        public string Password { get; private set; }

        public int ReadBufferSize { get; private set; }

        public int ReceiveTimeout { get; private set; }

        public int SendTimeout { get; private set; }

        public LocalCertificateSelectionCallback SslCertificateSelection { get; private set; }

        public RemoteCertificateValidationCallback SslCertificateValidation { get; private set; }

        public bool ThrowOnError { get; private set; }

        public int WriteBufferSize { get; private set; }

        public bool UseBackgroundThread { get; private set; }

        public bool UseSsl { get; private set; }

        #endregion Properties

        #region Methods

        public virtual uint GetCRCHashCode()
        {
            if (!m_CRCHash.HasValue)
                m_CRCHash = RedisCommon.CRC32ChecksumOf(ToString().ToBytes());
            return m_CRCHash.Value;
        }

        public void SetSslCertificateSelection(LocalCertificateSelectionCallback sslCertificateSelection)
        {
            SslCertificateSelection = sslCertificateSelection;
        }

        public void SetSslCertificateValidation(RemoteCertificateValidationCallback sslCertificateValidation)
        {
            SslCertificateValidation = sslCertificateValidation;
        }

        public virtual RedisConnectionSettings Clone(string host = null, int port = -1)
        {
            return new RedisConnectionSettings(
                            host: host ?? RedisConstants.LocalHost,
                            port: port < 1 ? RedisConstants.DefaultPort : port,
                            masterName: MasterName,
                            password: Password,
                            clientName: ClientName,
                            connectionTimeout: ConnectionTimeout,
                            receiveTimeout: ReceiveTimeout,
                            sendTimeout: SendTimeout,
                            connectionWaitTimeout: ConnectionWaitTimeout,
                            readBufferSize: ReadBufferSize,
                            writeBufferSize: WriteBufferSize,
                            heartBeatEnabled: HeartBeatEnabled,
                            hearBeatIntervalInSecs: HearBeatIntervalInSecs,
                            bulkSendFactor: BulkSendFactor,
                            useBackgroundThread: UseBackgroundThread,
                            throwOnError: ThrowOnError,
                            useSsl: UseSsl,
                            sslCertificateSelection: SslCertificateSelection,
                            sslCertificateValidation: SslCertificateValidation);
        }

        #region Overrides

        public override string ToString()
        {
            var sBuilder = new StringBuilder();
            WriteTo(sBuilder);

            return sBuilder.ToString();
        }

        protected virtual void WriteTo(StringBuilder sBuilder)
        {
            var endPoints = EndPoints;
            if (!endPoints.IsEmpty())
            {
                sBuilder.Append("host=");

                var index = 0;
                foreach (var endPoint in endPoints)
                {
                    if (!endPoint.IsEmpty())
                    {
                        sBuilder.Append(endPoint.Host);
                        sBuilder.Append(':');
                        sBuilder.Append(endPoint.Port);

                        if (index++ > 0)
                            sBuilder.Append(',');
                    }
                }

                if (sBuilder.Length > 0)
                    sBuilder.Append(';');
            }

            if (!MasterName.IsEmpty())
            {
                sBuilder.Append("masterName=");
                sBuilder.Append(MasterName);
                sBuilder.Append(';');
            }

            if (!Password.IsEmpty())
            {
                sBuilder.Append("password=");
                sBuilder.Append(Password);
                sBuilder.Append(';');
            }

            if (!ClientName.IsEmpty())
            {
                sBuilder.Append("clientName=");
                sBuilder.Append(ClientName);
                sBuilder.Append(';');
            }

            if (ConnectionTimeout != RedisConstants.DefaultConnectionTimeout)
            {
                sBuilder.Append("connectionTimeout=");
                sBuilder.Append(ConnectionTimeout);
                sBuilder.Append(';');
            }

            if (ReceiveTimeout != RedisConstants.DefaultReceiveTimeout)
            {
                sBuilder.Append("receiveTimeout=");
                sBuilder.Append(ReceiveTimeout);
                sBuilder.Append(';');
            }

            if (SendTimeout != RedisConstants.DefaultSendTimeout)
            {
                sBuilder.Append("sendTimeout=");
                sBuilder.Append(SendTimeout);
                sBuilder.Append(';');
            }

            if (ConnectionWaitTimeout != RedisConstants.DefaultWaitTimeout)
            {
                sBuilder.Append("connectionWaitTimeout=");
                sBuilder.Append(ConnectionWaitTimeout);
                sBuilder.Append(';');
            }

            if (ReadBufferSize > 0)
            {
                sBuilder.Append("readBufferSize=");
                sBuilder.Append(ReadBufferSize);
                sBuilder.Append(';');
            }

            if (WriteBufferSize > 0)
            {
                sBuilder.Append("writeBufferSize=");
                sBuilder.Append(WriteBufferSize);
                sBuilder.Append(';');
            }

            if (!HeartBeatEnabled)
            {
                sBuilder.Append("heartBeatEnabled=");
                sBuilder.Append(HeartBeatEnabled);
                sBuilder.Append(';');
            }

            if (HearBeatIntervalInSecs != RedisConstants.DefaultHeartBeatIntervalSecs)
            {
                sBuilder.Append("hearBeatIntervalInSecs=");
                sBuilder.Append(HearBeatIntervalInSecs);
                sBuilder.Append(';');
            }

            if (BulkSendFactor > 0 && BulkSendFactor != RedisConstants.DefaultBulkSendFactor)
            {
                sBuilder.Append("bulkSendFactor=");
                sBuilder.Append(UseBackgroundThread);
                sBuilder.Append(';');
            }

            if (UseBackgroundThread)
            {
                sBuilder.Append("useBackgroundThread=");
                sBuilder.Append(UseBackgroundThread);
                sBuilder.Append(';');
            }

            if (ThrowOnError)
            {
                sBuilder.Append("throwOnError=");
                sBuilder.Append(ThrowOnError);
                sBuilder.Append(';');
            }

            if (UseSsl)
            {
                sBuilder.Append("useSsl=");
                sBuilder.Append(UseSsl);
                sBuilder.Append(';');
            }
        }

        #endregion Overrides

        #region Settings

        private void LoadFrom(string connectionString)
        {
            var settingsWithDefaults = GetSettingsWithDefaults();

            var settings = ParseConnectionString(connectionString);
            if (settings != null)
            {
                foreach (var kv in settings)
                {
                    if (!kv.Value.IsEmpty())
                        ParseProperty(settingsWithDefaults, kv.Key.ToLowerInvariant(), kv.Value);
                }

                var port = -1;
                string str;
                if (settings.TryGetValue("port", out str) && !str.IsEmpty())
                    port = int.Parse(str);

                string host;
                settings.TryGetValue("host", out host);

                settingsWithDefaults["host"] = ToRedisEndPoints(host, port);
            }

            SetSettings(settingsWithDefaults);
        }

        private static IDictionary<string, string> ParseConnectionString(string connectionString)
        {
            if (!connectionString.IsEmpty())
            {
                var parts = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts != null)
                {
                    var length = parts.Length;
                    if (length > 0)
                    {
                        var result = new Dictionary<string, string>(length);

                        for (var i = 0; i < length; i++)
                        {
                            var part = (parts[i] ?? String.Empty).Trim();
                            if (!part.IsEmpty())
                            {
                                var pos = part.IndexOf('=');
                                if (pos == -1)
                                    result[part] = null;
                                else
                                {
                                    var key = (part.Substring(0, pos) ?? String.Empty).TrimEnd();
                                    if (!key.IsEmpty())
                                    {
                                        if (pos == part.Length - 1)
                                            result[key] = null;
                                        else
                                            result[key] = (part.Substring(pos + 1) ?? String.Empty).TrimStart();
                                    }
                                }
                            }
                        }
                        return result;
                    }
                }
            }
            return null;
        }

        protected virtual void SetSettings(IDictionary<string, object> settings)
        {
            object obj;
            RedisEndPoint[] endPoints = null;
            if (settings.TryGetValue("host", out obj))
            {
                var endPointList = obj as HashSet<RedisEndPoint>;
                if (endPointList != null)
                    endPoints = endPointList.ToArray();
            }

            EndPoints = !endPoints.IsEmpty() ? endPoints :
                new[] { new RedisEndPoint(RedisConstants.LocalHost, RedisConstants.DefaultPort) };

            foreach (var kv in settings)
            {
                switch (kv.Key)
                {
                    case "mastername":
                        MasterName = kv.Value as string;
                        break;
                    case "password":
                        Password = kv.Value as string;
                        break;
                    case "clientname":
                        ClientName = kv.Value as string;
                        break;
                    case "connectiontimeout":
                        ConnectionTimeout = (int)kv.Value;
                        break;
                    case "receivetimeout":
                        ReceiveTimeout = (int)kv.Value;
                        break;
                    case "sendtimeout":
                        SendTimeout = (int)kv.Value;
                        break;
                    case "connectionwaittimeout":
                        ConnectionWaitTimeout = (int)kv.Value;
                        break;
                    case "readbuffersize":
                        ReadBufferSize = (int)kv.Value;
                        break;
                    case "writebuffersize":
                        WriteBufferSize = (int)kv.Value;
                        break;
                    case "heartbeatenabled":
                        HeartBeatEnabled = (bool)kv.Value;
                        break;
                    case "hearbeatintervalinsecs":
                        HearBeatIntervalInSecs = (int)kv.Value;
                        break;
                    case "bulksendfactor":
                        BulkSendFactor = (int)kv.Value;
                        break;
                    case "usebackgroundthread":
                        UseBackgroundThread = (bool)kv.Value;
                        break;
                    case "throwonerror":
                        ThrowOnError = (bool)kv.Value;
                        break;
                    case "usessl":
                        UseSsl = (bool)kv.Value;
                        break;
                    default:
                        break;
                }
            }
        }

        protected virtual IDictionary<string, object> GetSettingsWithDefaults()
        {
            return new Dictionary<string, object>
            {
                { "mastername", null },
                { "password", null },
                { "clientname", null },
                { "connectiontimeout", RedisConstants.DefaultConnectionTimeout },
                { "receivetimeout", RedisConstants.DefaultReceiveTimeout },
                { "sendtimeout", RedisConstants.DefaultSendTimeout },
                { "connectionwaittimeout", RedisConstants.DefaultWaitTimeout },
                { "readbuffersize", 0 },
                { "writebuffersize", 0 },
                { "heartbeatenabled", true },
                { "hearbeatintervalinsecs", RedisConstants.DefaultHeartBeatIntervalSecs },
                { "bulksendfactor", RedisConstants.DefaultBulkSendFactor },
                { "throwonerror", true },
                { "usessl", false }
            };
        }

        protected virtual int GetDefaultPort()
        {
            return RedisConstants.DefaultPort;
        }

        protected virtual bool ParseProperty(IDictionary<string, object> settings, string key, string value)
        {
            switch (key)
            {
                case "mastername":
                case "password":
                case "clientname":
                    settings[key] = value;
                    break;
                case "connectiontimeout":
                case "receivetimeout":
                case "sendtimeout":
                case "connectionwaittimeout":
                case "readbuffersize":
                case "writebuffersize":
                case "hearbeatintervalinsecs":
                case "bulksendfactor":
                    settings[key] = int.Parse(value);
                    break;
                case "heartbeatenabled":
                case "throwonerror":
                case "usebackgroundthread":
                case "usessl":
                    settings[key] = bool.Parse(value);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private HashSet<RedisEndPoint> ToRedisEndPoints(string host, int port)
        {
            host = (host ?? String.Empty).Trim();
            port = port > 0 ? port : GetDefaultPort();

            if (!host.IsEmpty())
            {
                if (host.IndexOfAny(new[] { ':', ',', '|' }) == -1)
                    return new HashSet<RedisEndPoint> { new RedisEndPoint(host, port) };

                var hosts = host.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (hosts != null)
                {
                    var length = hosts.Length;
                    if (length > 0)
                    {
                        var endPoints = new HashSet<RedisEndPoint>();

                        for (var i = 0; i < length; i++)
                        {
                            var endPoint = hosts[i].ToRedisEndPoint(port);
                            if (!endPoint.IsEmpty())
                                endPoints.Add(endPoint);
                        }

                        if (endPoints.Count == 0)
                            endPoints.Add(RedisEndPoint.LocalHostEndPoint);

                        return endPoints;
                    }
                }
            }
            return new HashSet<RedisEndPoint> { RedisEndPoint.LocalHostEndPoint };
        }

        private void HostToEndPoint(HashSet<RedisEndPoint> hostList, string host)
        {
            if (!host.IsEmpty())
            {
                var pos = host.IndexOf(':');
                if (pos == -1)
                    hostList.Add(new RedisEndPoint(host, GetDefaultPort()));
                else
                {
                    var name = (host.Substring(0, pos) ?? String.Empty).TrimEnd();
                    if (!name.IsEmpty())
                    {
                        if (pos == host.Length - 1)
                            hostList.Add(RedisEndPoint.IP4LoopbackEndPoint);
                        else
                        {
                            var port = (host.Substring(pos + 1) ?? String.Empty).TrimStart();
                            if (port.IsEmpty())
                                hostList.Add(new RedisEndPoint(name, GetDefaultPort()));
                            else
                                hostList.Add(new RedisEndPoint(name, int.Parse(port)));
                        }
                    }
                }
            }
        }

        public static T Parse<T>(string connectionString)
            where T : RedisConnectionSettings, new()
        {
            var result = new T();
            result.LoadFrom(connectionString);

            return result;
        }

        #endregion Settings

        #endregion Methods

        #region Static Methods

        protected static RedisEndPoint[] ToEndPointList(HashSet<RedisEndPoint> endPoints, int defaultPort = RedisConstants.DefaultPort)
        {
            if (endPoints != null)
            {
                var count = endPoints.Count;
                if (count > 0)
                {
                    var result = endPoints.Where(ep => !ep.IsEmpty()).ToArray();
                    if (!result.IsEmpty())
                        return result;
                }
            }
            return new[] { new RedisEndPoint(RedisConstants.LocalHost, defaultPort) };
        }

        #endregion Static Methods
    }
}
