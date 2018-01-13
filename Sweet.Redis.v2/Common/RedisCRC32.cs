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
    public static class RedisCRC32
    {
        #region Static Members

        private static readonly uint[] DefaultCRC32Vector;

        #endregion Static Members

        #region Constants

        private const uint CrcPoly = 0xedb88320;
        private const uint CrcInit = 0xffffffff;
        private const int CRC32TableLength = 256;

        #endregion Constants

        #region .Ctors

        static RedisCRC32()
        {
            DefaultCRC32Vector = CalculateCRC32Vector();
        }

        #endregion .Ctors

        #region Methods

        public static uint CRC32(byte[] bytes)
        {
            if (bytes != null)
            {
                var vector = NewCRC32Vector();
                var bytesLength = bytes.Length;

                var crc = CrcInit;
                for (var i = 0; i < bytesLength; ++i)
                {
                    var index = (byte)(((crc) & 0xff) ^ bytes[i]);
                    crc = (uint)((crc >> 8) ^ vector[index]);
                }
                return ~crc;
            }
            return 0u;
        }


        public static byte[] CRC32BytesOf(byte[] bytes)
        {
            return BitConverter.GetBytes(CRC32(bytes));
        }

        private static uint[] NewCRC32Vector()
        {
            var result = new uint[CRC32TableLength];
            Array.Copy(DefaultCRC32Vector, result, CRC32TableLength);
            return result;
        }

        private static uint[] CalculateCRC32Vector()
        {
            var result = new uint[CRC32TableLength];

            var crcItem = 0u;
            for (var i = 0u; i < CRC32TableLength; ++i)
            {
                crcItem = i;
                for (var j = 8; j > 0; --j)
                {
                    if ((crcItem & 1) == 1)
                        crcItem = (uint)((crcItem >> 1) ^ CrcPoly);
                    else
                        crcItem >>= 1;
                }
                result[i] = crcItem;
            }
            return result;
        }

        #endregion Methods
    }
}
