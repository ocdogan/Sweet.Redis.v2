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
    public class RedisMonitorMessage
    {
        #region Static Members

        public static readonly RedisMonitorMessage Empty = new RedisMonitorMessage();

        #endregion Static Members

        #region .Ctors

        private RedisMonitorMessage()
        {
            ClientInfo = String.Empty;
            Command = String.Empty;
            Data = null;
            Time = DateTime.MinValue;
            IsEmpty = true;
        }

        public RedisMonitorMessage(DateTime time, string clientInfo, string command, string data)
        {
            ClientInfo = clientInfo;
            Command = command;
            Data = data;
            Time = time;
        }

        #endregion .Ctors

        #region Properties

        public string ClientInfo { get; private set; }

        public string Command { get; private set; }

        public string Data { get; private set; }

        public bool IsEmpty { get; private set; }

        public DateTime Time { get; private set; }

        #endregion Properties
    }
}
