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

namespace Sweet.Redis.v2
{
    internal class RedisManagedServerGroup : RedisManagedNodesGroup
    {
        #region .Ctors

        public RedisManagedServerGroup(RedisManagerSettings settings, RedisRole role,
                   RedisManagedServerNode[] nodes, Action<object, RedisCardioPulseStatus> onPulseStateChange)
            : base(settings, role, nodes, onPulseStateChange)
        {
            if (!(role == RedisRole.Slave || role == RedisRole.Master))
                throw new RedisException("Role must be master or slave");
        }

        #endregion .Ctors

        #region Methods

        public RedisManagedServer Next()
        {
            var node = NextNode();
            if (node.IsAlive())
                return (RedisManagedServer)node.Seed;
            return null;
        }

        #endregion Methods
    }
}
