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

namespace Sweet.Redis.v2
{
    internal class RedisAdmin : RedisAsyncCommandExecuter, IRedisAdmin, IRedisDisposable
    {
        #region Field Members

        private IRedisCommandsCluster m_ClusterCommands;
        private IRedisCommandsServer m_ServerCommands;

        #endregion Field Members

        #region .Ctors

        public RedisAdmin(RedisAsyncClient asyncClient, int dbIndex, bool throwOnError = true)
            : base(asyncClient, dbIndex, throwOnError)
        { }

        #endregion .Ctors

        #region Properties

        public IRedisCommandsServer Server
        {
            get
            {
                ValidateNotDisposed();
                if (m_ServerCommands == null)
                    m_ServerCommands = new RedisServerCommands(this);
                return m_ServerCommands;
            }
        }

        public IRedisCommandsCluster Cluster
        {
            get
            {
                ValidateNotDisposed();
                if (m_ClusterCommands == null)
                    m_ClusterCommands = new RedisClusterCommands(this);
                return m_ClusterCommands;
            }
        }

        #endregion Properties
    }
}
