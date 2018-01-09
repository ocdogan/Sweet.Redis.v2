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
using System.Threading;

namespace Sweet.Redis.v2
{
    internal class RedisSentinelClient : RedisDisposable, IRedisSentinelClient, IRedisDisposable
    {
        #region Field Members

        private RedisAsyncClient m_Client;
        private IRedisCommandsSentinel m_Commands;
        private RedisAsyncCommandExecuter m_Executer;

        private bool m_ThrowOnError;
        private long m_Id = RedisIDGenerator<RedisSentinelClient>.NextId();

        #endregion Field Members

        #region .Ctors

        protected internal RedisSentinelClient(RedisAsyncClient client)
        {
            if (client == null)
                throw new RedisFatalException(new ArgumentNullException("client"), RedisErrorCode.MissingParameter);

            m_Client = client.Disposed ? new RedisAsyncClient(client.Settings) : client;
            m_ThrowOnError = m_Client.Settings.ThrowOnError;
            m_Executer = new RedisAsyncCommandExecuter(m_Client, RedisConstants.UninitializedDbIndex, m_ThrowOnError);
        }

        public RedisSentinelClient(RedisSentinelSettings settings)
            : base()
        {
            if (settings == null)
                throw new RedisFatalException(new ArgumentNullException("settings"), RedisErrorCode.MissingParameter);

            m_ThrowOnError = settings.ThrowOnError;
            m_Client = new RedisAsyncClient(settings);
            m_Executer = new RedisAsyncCommandExecuter(m_Client, RedisConstants.UninitializedDbIndex, m_ThrowOnError);
        }

        #endregion .Ctors

        #region Destructors

        protected override void OnDispose(bool disposing)
        {
            using (Interlocked.Exchange(ref m_Client, null)) { }
            using (Interlocked.Exchange(ref m_Executer, null)) { }

            base.OnDispose(disposing);
        }

        #endregion Destructors

        #region Properties

        public IRedisCommandsSentinel Commands
        {
            get
            {
                ValidateNotDisposed();
                if (m_Commands == null)
                    m_Commands = new RedisSentinelCommands(m_Executer);
                return m_Commands;
            }
        }

        public RedisRole ExpectedRole
        {
            get { return RedisRole.Sentinel; }
        }

        public int DbIndex
        {
            get { return RedisConstants.UninitializedDbIndex; }
        }

        public long Id
        {
            get { return m_Id; }
        }

        public bool ThrowOnError
        {
            get { return m_ThrowOnError; }
        }

        #endregion Properties

        #region Methods

        public override void ValidateNotDisposed()
        {
            base.ValidateNotDisposed();
        }

        #endregion Methods
    }
}
