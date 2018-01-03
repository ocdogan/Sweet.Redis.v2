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
    public class RedisMasterRoleSlaveInfo
    {
        #region .Ctors

        internal RedisMasterRoleSlaveInfo()
        { }

        #endregion .Ctors

        #region Properties

        public string IPAddress { get; private set; }

        public int? Port { get; private set; }

        public long? ReplicationOffset { get; private set; }

        #endregion Properties

        #region Methods

        public static RedisMasterRoleSlaveInfo Parse(RedisArray array)
        {
            if (!ReferenceEquals(array, null))
            {
                var list = array.Value;
                if (list != null)
                {
                    var count = list.Count;
                    if (count > 0)
                    {
                        var result = new RedisMasterRoleSlaveInfo();

                        var item = list[0];
                        if (!ReferenceEquals(item, null) && item.Type == RedisResultType.Bytes)
                            result.IPAddress = ((RedisBytes)item).Value.ToUTF8String();

                        if (count > 1)
                        {
                            item = list[1];
                            if (!ReferenceEquals(item, null) && item.Type == RedisResultType.Bytes)
                            {
                                var port = ((RedisBytes)item).Value.ToUTF8String();
                                if (!port.IsEmpty())
                                {
                                    int i;
                                    if (port.TryParse(out i))
                                        result.Port = i;
                                }
                            }

                            if (count > 2)
                            {
                                item = list[2];
                                if (!ReferenceEquals(item, null) && item.Type == RedisResultType.Bytes)
                                {
                                    var offset = ((RedisBytes)item).Value.ToUTF8String();
                                    if (!offset.IsEmpty())
                                    {
                                        long l;
                                        if (offset.TryParse(out l))
                                            result.ReplicationOffset = l;
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

        #endregion Methods
    }
}