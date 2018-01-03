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
    internal class RedisPubSubCommands : RedisCommandSet, IRedisPubSubCommands
    {
        #region .Ctors

        public RedisPubSubCommands(RedisAsyncCommandExecuter executer)
            : base(executer)
        { }

        #endregion .Ctors

        #region Methods

        public RedisInteger Publish(RedisParam channel, RedisParam message)
        {
            if (channel.IsNull)
                throw new ArgumentNullException("channel");

            if (message.IsNull)
                throw new ArgumentNullException("message");

            return ExpectInteger(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.Publish, channel, message));
        }

        public RedisMultiString PubSubChannels(RedisParam? pattern = null)
        {
            RedisArray response;
            if (pattern.HasValue && !pattern.Value.IsEmpty)
                response = ExpectArray(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.PubSub, RedisCommandList.Channels, pattern.Value));
            else
                response = ExpectArray(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.PubSub, RedisCommandList.Channels));

            if (response != null)
            {
                var items = response.Value;
                if (items != null)
                {
                    var itemCount = items.Count;
                    var result = new string[itemCount];

                    if (itemCount > 0)
                    {
                        for (var i = 0; i < itemCount; i++)
                        {
                            var item = items[i];
                            if (item != null)
                            {
                                if (item.Type == RedisResultType.Bytes)
                                    result[i] = ((RedisBytes)item).Value.ToUTF8String() ?? String.Empty;
                                else if (item.Type == RedisResultType.String)
                                    result[i] = ((RedisString)item).Value ?? String.Empty;
                            }
                        }
                    }

                    return result;
                }
            }
            return new string[0];
        }

        public RedisResult<RedisKeyValue<string, long>[]> PubSubNumerOfSubscribers(params RedisParam[] channels)
        {
            RedisArray response;
            if (channels.Length > 0)
                response = ExpectArray(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.PubSub, RedisCommandList.NumSub.Join(channels)));
            else
                response = ExpectArray(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.PubSub, RedisCommandList.NumSub));

            if (response != null)
            {
                var items = response.Value;
                if (items != null)
                {
                    var itemCount = items.Count;
                    var result = new RedisKeyValue<string, long>[itemCount / 2];

                    if (itemCount > 0)
                    {
                        for (int i = 0, index = 0; i < itemCount; index++)
                        {
                            var nameItem = items[i++];
                            var countItem = items[i++];

                            var name = String.Empty;
                            var count = RedisConstants.Zero;

                            if (nameItem != null)
                            {
                                if (nameItem.Type == RedisResultType.Bytes)
                                    name = ((RedisBytes)nameItem).Value.ToUTF8String() ?? String.Empty;
                                else if (nameItem.Type == RedisResultType.String)
                                    name = ((RedisString)nameItem).Value ?? String.Empty;
                            }

                            if (countItem != null &&
                                countItem.Type == RedisResultType.Integer)
                                count = ((RedisInteger)countItem).Value;

                            result[index] = new RedisKeyValue<string, long>(name, count);
                        }
                    }

                    return new RedisResult<RedisKeyValue<string, long>[]>(result);
                }
            }
            return new RedisResult<RedisKeyValue<string, long>[]>(new RedisKeyValue<string, long>[0]);
        }

        public RedisInteger PubSubNumerOfSubscriptionsToPatterns()
        {
            return ExpectInteger(new RedisCommand(RedisConstants.UninitializedDbIndex, RedisCommandList.PubSub, RedisCommandList.NumPat));
        }

        #endregion Methods
    }
}
