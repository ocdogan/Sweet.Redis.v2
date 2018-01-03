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

namespace Sweet.Redis.v2
{
    /*
    EVAL script numkeys key [key ...] arg [arg ...]
    summary: Execute a Lua script server side
    since: 2.6.0

    EVALSHA sha1 numkeys key [key ...] arg [arg ...]
    summary: Execute a Lua script server side
    since: 2.6.0

    SCRIPT DEBUG YES|SYNC|NO
    summary: Set the debug mode for executed scripts.
    since: 3.2.0

    SCRIPT EXISTS script [script ...]
    summary: Check existence of scripts in the script cache.
    since: 2.6.0

    SCRIPT FLUSH -
    summary: Remove all the scripts from the script cache.
    since: 2.6.0

    SCRIPT KILL -
    summary: Kill the script currently in execution.
    since: 2.6.0

    SCRIPT LOAD script
    summary: Load the specified Lua script into the script cache.
    since: 2.6.0
     */
    public interface IRedisScriptingCommands
    {
        RedisArray Eval(RedisParam script, params RedisKeyValue<RedisParam, RedisParam>[] args);

        RedisArray EvalSHA(RedisParam sha1, params RedisKeyValue<RedisParam, RedisParam>[] args);
        RedisArray EvalSHA(ref RedisParam sha1, RedisParam script, params RedisKeyValue<RedisParam, RedisParam>[] args);

        RedisBool ScriptDebugYes();
        RedisBool ScriptDebugSync();
        RedisBool ScriptDebugNo();
        RedisMultiInteger ScriptExists(RedisParam sha1, params RedisParam[] sha1s);
        RedisBool ScriptFush();
        RedisBool ScriptKill();
        RedisString ScriptLoad(RedisParam script);
    }
}
