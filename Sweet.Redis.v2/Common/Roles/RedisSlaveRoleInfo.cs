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
    public class RedisSlaveRoleInfo : RedisRoleInfo
    {
        #region .Ctors

        internal RedisSlaveRoleInfo(string role)
            : base(role)
        { }

        #endregion .Ctors

        #region Properties

        public string IPAddress { get; protected set; }

        public int? Port { get; protected set; }

        public string State { get; protected set; }

        public long? DataReceived { get; protected set; }

        #endregion Properties

        #region Methods

        protected override void ParseInfo(RedisArray array)
        {
            if (!ReferenceEquals(array, null))
            {
                var list = array.Value;
                if (list != null)
                {
                    var count = list.Count;
                    if (count > 1)
                    {
                        var item = list[1];
                        if (!ReferenceEquals(item, null) && item.Type == RedisResultType.Bytes)
                            IPAddress = ((RedisBytes)item).Value.ToUTF8String();

                        if (count > 2)
                        {
                            item = list[2];
                            if (!ReferenceEquals(item, null))
                            {
                                if (item.Type == RedisResultType.Integer)
                                    Port = (int)((RedisInteger)item).Value;
                                else if (item.Type == RedisResultType.Bytes)
                                {
                                    var data = ((RedisBytes)item).Value.ToUTF8String();
                                    if (!string.IsNullOrEmpty(data))
                                    {
                                        long l;
                                        if (data.TryParse(out l))
                                            Port = (int)l;
                                    }
                                }
                            }

                            if (count > 3)
                            {
                                item = list[3];
                                if (!ReferenceEquals(item, null) && item.Type == RedisResultType.Bytes)
                                    State = ((RedisBytes)item).Value.ToUTF8String();

                                if (count > 4)
                                {
                                    item = list[4];
                                    if (!ReferenceEquals(item, null))
                                    {
                                        if (item.Type == RedisResultType.Integer)
                                            DataReceived = ((RedisInteger)item).Value;
                                        else if (item.Type == RedisResultType.Bytes)
                                        {
                                            var data = ((RedisBytes)item).Value.ToUTF8String();
                                            if (!string.IsNullOrEmpty(data))
                                            {
                                                long l;
                                                if (data.TryParse(out l))
                                                    DataReceived = l;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion Methods
    }
}
