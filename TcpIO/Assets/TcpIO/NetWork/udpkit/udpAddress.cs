/*
* The MIT License (MIT)
* 
* Copyright (c) 2012-2014 Fredrik Holmstrom (fredrik.johan.holmstrom@gmail.com)
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;

namespace UdpKit {
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct UdpIPv4Address : IEquatable<UdpIPv4Address>, IComparable<UdpIPv4Address> {
        public class Comparer : IComparer<UdpIPv4Address>, IEqualityComparer<UdpIPv4Address> {
            int IComparer<UdpIPv4Address>.Compare (UdpIPv4Address x, UdpIPv4Address y) {
                return Compare(x, y);
            }

            bool IEqualityComparer<UdpIPv4Address>.Equals (UdpIPv4Address x, UdpIPv4Address y) {
                return Compare(x, y) == 0;
            }

            int IEqualityComparer<UdpIPv4Address>.GetHashCode (UdpIPv4Address obj) {
                return (int) obj.Packed;
            }
        }

        public static readonly UdpIPv4Address Any = new UdpIPv4Address();
        public static readonly UdpIPv4Address Localhost = new UdpIPv4Address(127, 0, 0, 1);

        [FieldOffset(0)]
        public readonly uint Packed;
        [FieldOffset(0)]
        public readonly byte Byte0;
        [FieldOffset(1)]
        public readonly byte Byte1;
        [FieldOffset(2)]
        public readonly byte Byte2;
        [FieldOffset(3)]
        public readonly byte Byte3;

        public UdpIPv4Address (long addr) {
            Byte0 = Byte1 = Byte2 = Byte3 = 0;
            Packed = (uint) IPAddress.NetworkToHostOrder((int) addr);
        }

        public UdpIPv4Address (byte a, byte b, byte c, byte d) {
            Packed = 0;
            Byte0 = d;
            Byte1 = c;
            Byte2 = b;
            Byte3 = a;
        }

        public bool Equals (UdpIPv4Address other) {
            return Compare(this, other) == 0;
        }
        
        public int CompareTo (UdpIPv4Address other) {
            return Compare(this, other);
        }

        public override int GetHashCode () {
            return (int) Packed;
        }

        public override bool Equals (object obj) {
            if (obj is UdpIPv4Address) {
                return Compare(this, (UdpIPv4Address) obj) == 0;
            }

            return false;
        }

        public override string ToString () {
            return string.Format("{0}.{1}.{2}.{3}", Byte3, Byte2, Byte1, Byte0);
        }

        public static bool operator == (UdpIPv4Address x, UdpIPv4Address y) {
            return Compare(x, y) == 0;
        }

        public static bool operator != (UdpIPv4Address x, UdpIPv4Address y) {
            return Compare(x, y) != 0;
        }

        static int Compare (UdpIPv4Address x, UdpIPv4Address y) {
            if (x.Packed > y.Packed) return 1;
            if (x.Packed < y.Packed) return -1;
            return 0;
        }

        public static UdpIPv4Address Parse (string address) {
            string[] parts = address.Split('.');

            if (parts.Length != 4) { 
                throw new FormatException("address is not in the correct format");
            }

            return new UdpIPv4Address(byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3]));
        }
    }
}
