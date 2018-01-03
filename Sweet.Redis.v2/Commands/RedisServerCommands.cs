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
    internal class RedisServerCommands : RedisCommandSet, IRedisServerCommands
    {
        #region .Ctors

        public RedisServerCommands(RedisAsyncCommandExecuter executer)
            : base(executer)
        { }

        #endregion .Ctors

        #region Methods

        public RedisBool BGRewriteAOF()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.BGRewriteAOF));
        }

        public RedisBool BGSave()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.BGSave));
        }

        public RedisString ClientGetName()
        {
            return ExpectBulkString(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Client, RedisCommandList.GetName));
        }

        public RedisInteger ClientKill(RedisParam? ip = null, int port = -1, RedisParam? clientId = null, RedisParam? type = null, bool skipMe = true)
        {
            ValidateNotDisposed();

            var parameters = new byte[0].Join(RedisCommandList.Kill);

            if (ip.HasValue && !ip.Value.IsEmpty)
            {
                parameters = parameters
                    .Join(RedisCommandList.Addr)
                    .Join(ip.Value);

                if (port > -1)
                    parameters = parameters.Join(port.ToBytes());
            }

            if (clientId.HasValue && !clientId.Value.IsEmpty)
            {
                parameters = parameters
                    .Join(RedisCommandList.Id)
                    .Join(clientId);
            }

            if (type.HasValue && !type.Value.IsEmpty)
            {
                parameters = parameters
                    .Join(RedisCommandList.Type)
                    .Join(type);
            }

            if (!skipMe)
            {
                parameters = parameters
                    .Join(RedisCommandList.SkipMe)
                    .Join(RedisCommandList.No);
            }

            return ExpectInteger(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Client, parameters));
        }

        public RedisResult<RedisClientInfo[]> ClientList()
        {
            var response = ExpectBulkString(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Client, RedisCommandList.List));
            if (response != (string)null)
            {
                var value = response.Value;
                if (!value.IsEmpty())
                {
                    var lines = value.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (!lines.IsEmpty())
                    {
                        var list = new List<RedisClientInfo>(lines.Length);
                        foreach (var line in lines)
                        {
                            var info = RedisClientInfo.Parse(line);
                            if (info != null)
                                list.Add(info);
                        }
                        return new RedisResult<RedisClientInfo[]>((list.Count > 0) ? list.ToArray() : null);
                    }
                }
            }
            return new RedisResult<RedisClientInfo[]>(null);
        }

        public RedisResult<IDictionary<string, string>[]> ClientListDictionary()
        {
            var response = ExpectBulkString(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Client, RedisCommandList.List));
            if (response != (string)null)
            {
                var value = response.Value;
                if (!value.IsEmpty())
                {
                    var lines = value.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (!lines.IsEmpty())
                    {
                        var list = new List<IDictionary<string, string>>(lines.Length);
                        foreach (var line in lines)
                        {
                            var info = RedisClientInfo.ParseDictionary(line);
                            if (info != null)
                                list.Add(info);
                        }
                        return new RedisResult<IDictionary<string, string>[]>((list.Count > 0) ? list.ToArray() : null);
                    }
                }
            }
            return new RedisResult<IDictionary<string, string>[]>(null);
        }

        public RedisBool ClientPause(int timeout)
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Client, RedisCommandList.Pause, timeout.ToBytes()));
        }

        public RedisBool ClientReplyOff()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Client, RedisCommandList.Reply, RedisCommandList.Off));
        }

        public RedisBool ClientReplyOn()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Client, RedisCommandList.Reply, RedisCommandList.On));
        }

        public RedisBool ClientReplySkip()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Client, RedisCommandList.Reply, RedisCommandList.Skip));
        }

        public RedisBool ClientSetName(RedisParam connectionName)
        {
            if (connectionName.IsNull)
                throw new ArgumentNullException("connectionName");

            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Client, RedisCommandList.SetName, connectionName.ToBytes()));
        }

        public RedisResult<IDictionary<string, string>> ConfigGet(RedisParam parameter)
        {
            if (parameter.IsNull)
                throw new ArgumentNullException("parameter");

            var lines = ExpectMultiDataStrings(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Config, RedisCommandList.Get, parameter.ToBytes()));
            if (!ReferenceEquals(lines, null))
            {
                var value = lines.Value;
                if (value != null)
                {
                    var linesLength = value.Length;
                    if (linesLength > 0)
                    {
                        var result = new Dictionary<string, string>(linesLength / 2);
                        for (var i = 0; i < linesLength; i += 2)
                        {
                            var key = (value[i] ?? String.Empty).Trim();
                            if (!key.IsEmpty())
                                result[key] = (value[i + 1] ?? String.Empty).Trim();
                        }
                        return new RedisResult<IDictionary<string, string>>(result);
                    }
                }
            }
            return new RedisResult<IDictionary<string, string>>(null);
        }

        public RedisBool ConfigResetStat()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Config, RedisCommandList.ResetStat));
        }

        public RedisBool ConfigRewrite()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Config, RedisCommandList.Rewrite));
        }

        public RedisBool ConfigSet(RedisParam parameter, RedisParam value)
        {
            if (parameter.IsNull)
                throw new ArgumentNullException("parameter");

            if (value.IsNull)
                throw new ArgumentNullException("value");

            ValidateNotDisposed();

            if (value.Length > RedisConstants.MaxValueLength)
                throw new ArgumentException("value is limited to 1GB", "value");

            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Config, RedisCommandList.Set, parameter, value));
        }

        public RedisInteger DbSize()
        {
            return ExpectInteger(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.DbSize));
        }

        public RedisBool FlushAll()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.FlushAll));
        }

        public RedisBool FlushAllAsync()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.FlushAll, RedisCommandList.Async));
        }

        public RedisBool FlushDb()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.FlushDb));
        }

        public RedisBool FlushDbAsync()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.FlushDb, RedisCommandList.Async));
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

        public RedisDate LastSave()
        {
            var response = ExpectInteger(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.LastSave));
            if (response != null)
            {
                return response.Value.FromUnixTimeStamp();
            }
            return DateTime.MinValue;
        }

        public RedisResult<RedisRoleInfo> Role()
        {
            var response = ExpectArray(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Role));
            if (!ReferenceEquals(response, null))
            {
                var result = RedisRoleInfo.Parse(response);
                return new RedisResult<RedisRoleInfo>(result);
            }
            return new RedisResult<RedisRoleInfo>(null);
        }

        public RedisBool Save()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Save));
        }

        public RedisVoid ShutDown()
        {
            ExpectNothing(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.ShutDown));
            return RedisVoid.Default;
        }

        public RedisVoid ShutDownSave()
        {
            ExpectNothing(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.ShutDown, RedisCommandList.Async));
            return RedisVoid.Default;
        }

        public RedisBool SlaveOf(RedisParam host, int port)
        {
            if (host.IsNull)
                throw new ArgumentNullException("host");

            if (port < 0 || port > ushort.MaxValue)
                throw new ArgumentException("Invalid port number");

            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.SlaveOf, host, port.ToBytes()));
        }

        public RedisBool SlaveOfNoOne()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.SlaveOf, RedisCommandList.NoOne));
        }

        public RedisResult<RedisSlowLogInfo[]> SlowLogGet(int count)
        {
            var response = ExpectArray(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.SlowLog, RedisCommandList.Get, count.ToBytes()));
            if (response != null)
                return new RedisResult<RedisSlowLogInfo[]>(RedisSlowLogInfo.ToSlowLogInfo(response));
            return new RedisResult<RedisSlowLogInfo[]>(null);
        }

        public RedisInteger SlowLogLen()
        {
            return ExpectInteger(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.SlowLog, RedisCommandList.Len));
        }

        public RedisBool SlowLogReset()
        {
            return ExpectOK(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.SlowLog, RedisCommandList.Reset));
        }

        public RedisDate Time()
        {
            var parts = ExpectMultiDataStrings(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Time));
            if (!ReferenceEquals(parts, null))
            {
                var value = parts.Value;
                if (value != null)
                {
                    if (value.Length > 1)
                        return value[0].ToInt().FromUnixTimeStamp(value[1].ToInt());
                    return value[0].ToInt().FromUnixTimeStamp();
                }
            }
            return DateTime.MinValue;
        }

        #endregion Methods
    }
}
