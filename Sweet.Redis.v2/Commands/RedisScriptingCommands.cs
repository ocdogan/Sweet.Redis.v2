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

namespace Sweet.Redis.v2
{
    internal class RedisScriptingCommands : RedisCommandSet, IRedisCommandsScripting
    {
        #region .Ctors

        public RedisScriptingCommands(RedisAsyncCommandExecuter executer)
            : base(executer)
        { }

        #endregion .Ctors

        #region Methods

        private RedisArray Eval(byte[] cmd, RedisParam source, params RedisKeyValue<RedisParam, RedisParam>[] args)
        {
            var argsLength = args.Length;
            if (argsLength == 0)
                return ExpectArray(new RedisCommand(cmd, source, RedisConstants.ZeroBytes));

            var parameters = new RedisParam[2 * (1 + argsLength)];

            parameters[0] = source;
            parameters[1] = argsLength.ToBytes();

            for (int i = 0, paramsIndex = 2; i < argsLength; i++, paramsIndex++)
            {
                parameters[paramsIndex] = args[i].Key;
                parameters[argsLength + paramsIndex] = args[i].Value;
            }

            return ExpectArray(new RedisCommand(cmd, parameters));
        }

        public RedisArray Eval(RedisParam script, params RedisKeyValue<RedisParam, RedisParam>[] args)
        {
            if (script.IsEmpty)
                throw new ArgumentNullException("script");

            return Eval(RedisCommandList.Eval, script, args);
        }

        public RedisArray EvalSHA(RedisParam sha1, params RedisKeyValue<RedisParam, RedisParam>[] args)
        {
            if (sha1.IsEmpty)
                throw new ArgumentNullException("sha1");

            return Eval(RedisCommandList.EvalSha, sha1, args);
        }

        public RedisArray EvalSHA(ref RedisParam sha1, RedisParam script, params RedisKeyValue<RedisParam, RedisParam>[] args)
        {
            if (sha1.IsEmpty)
                throw new ArgumentNullException("sha1");

            if (script.IsEmpty)
                return Eval(RedisCommandList.EvalSha, sha1, args);

            var response = ScriptExists(sha1);

            var exists = false;
            if (!ReferenceEquals(response, null))
            {
                var value = response.Value;
                if (value != null)
                    exists = (value.Length > 0) && value[0] == RedisConstants.One;
            }

            if (!exists)
            {
                var sha1S = ScriptLoad(script);
                sha1 = new RedisParam(sha1S);
            }

            if (sha1.IsEmpty)
                return null;

            try
            {
                return Eval(RedisCommandList.EvalSha, sha1, args);
            }
            catch (RedisException e)
            {
                var msg = e.Message;
                if (!msg.IsEmpty() &&
                    msg.StartsWith("NOSCRIPT", StringComparison.OrdinalIgnoreCase))
                {
                    var sha1S = ScriptLoad(script);
                    sha1 = new RedisParam(sha1S);

                    if (!sha1.IsEmpty)
                        return Eval(RedisCommandList.EvalSha, sha1, args);
                }
                throw;
            }
        }

        public RedisBool ScriptDebugNo()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Script, RedisCommandList.Debug, RedisCommandList.No));
        }

        public RedisBool ScriptDebugSync()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Script, RedisCommandList.Debug, RedisCommandList.Sync));
        }

        public RedisBool ScriptDebugYes()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Script, RedisCommandList.Debug, RedisCommandList.Yes));
        }

        public RedisMultiInteger ScriptExists(RedisParam sha1, params RedisParam[] sha1s)
        {
            if (sha1.IsEmpty)
                throw new ArgumentNullException("sha1");

            RedisArray response = null;
            if (sha1s.Length == 0)
                response = ExpectArray(new RedisCommand(DbIndex, RedisCommandList.Script, RedisCommandList.Exists, sha1));
            else
            {
                var parameters = RedisCommandList.Exists
                                              .Join(sha1)
                                              .Join(sha1s);

                response = ExpectArray(new RedisCommand(DbIndex, RedisCommandList.Script, RedisCommandList.Exists, sha1));
            }

            var resultLength = sha1.Length + 1;
            var result = new long[resultLength];

            if (response != null)
            {
                var items = response.Value;
                if (items != null)
                {
                    var responseLength = items.Count;

                    for (var i = 0; i < resultLength && i < responseLength; i++)
                    {
                        var item = items[i];
                        if (item != null &&
                            item.Type == RedisResultType.Integer)
                            result[i] = ((RedisInteger)item).Value;
                    }
                }
            }
            return result;
        }

        public RedisBool ScriptFush()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Script, RedisCommandList.Flush));
        }

        public RedisBool ScriptKill()
        {
            return ExpectOK(new RedisCommand(DbIndex, RedisCommandList.Script, RedisCommandList.Kill));
        }

        public RedisString ScriptLoad(RedisParam script)
        {
            if (script.IsEmpty)
                throw new ArgumentNullException("script");

            return ExpectBulkString(new RedisCommand(DbIndex, RedisCommandList.Script, RedisCommandList.Load, script));
        }

        #endregion Methods
    }
}
