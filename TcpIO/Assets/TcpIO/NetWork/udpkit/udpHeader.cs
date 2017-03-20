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
using System.Net;
using UnityEngine;

namespace UdpKit {
    struct UdpHeader {
        public const int SEQ_BITS = 15;
        public const int SEQ_PADD = 16 - SEQ_BITS;
        public const int SEQ_MASK = (1 << SEQ_BITS) - 1;
        public const int NETPING_BITS = 16;

        public ushort ObjSequence;
        public ushort AckSequence;
        public ulong AckHistory;
        public ushort AckTime;
        public bool IsObject;
        public uint Now;



        public void Pack (UdpStream buffer, UdpSocket socket) {
            int pos = buffer.Position;

            buffer.Position = 0;
			buffer.WriteUShort( (ushort)IPAddress.HostToNetworkOrder( (short)PadSequence(ObjSequence) ), SEQ_BITS + SEQ_PADD);
			buffer.WriteUShort( (ushort)IPAddress.HostToNetworkOrder( (short)PadSequence(AckSequence) ), SEQ_BITS + SEQ_PADD);
			buffer.WriteULong( (ulong)IPAddress.HostToNetworkOrder( (long)AckHistory ), UdpSocket.AckRedundancy);

            if (UdpSocket.CalculateNetworkPing) {
				buffer.WriteUShort( (ushort)IPAddress.HostToNetworkOrder( (short)AckTime ), NETPING_BITS);
            }

            buffer.Position = pos;
#if UNITY_EDITOR
			Log.info(this, "UdpHeader##Pack#ObjSequence: " + PadSequence(ObjSequence) + ":AckSequence: " + PadSequence(AckSequence));
#endif
        }

        public void Unpack (UdpStream buffer, UdpSocket socket) {
            buffer.Position = 0;

			ObjSequence = TrimSequence( (ushort)IPAddress.NetworkToHostOrder( (short)buffer.ReadUShort(SEQ_BITS + SEQ_PADD) ) );
			AckSequence = TrimSequence( (ushort)IPAddress.NetworkToHostOrder( (short)buffer.ReadUShort(SEQ_BITS + SEQ_PADD) ) );
			AckHistory = (ulong)IPAddress.NetworkToHostOrder( (long)buffer.ReadULong(UdpSocket.AckRedundancy) );

            if (UdpSocket.CalculateNetworkPing) {
				AckTime = (ushort)IPAddress.NetworkToHostOrder( (short)buffer.ReadUShort(NETPING_BITS) );
            }
        }

        ushort PadSequence (ushort sequence) {
            sequence <<= SEQ_PADD;

			//obejct type = 1.
            if (IsObject)
                sequence |= ((1 << SEQ_PADD) - 1);

            return sequence;
        }

        ushort TrimSequence (ushort sequence) {
            sequence >>= SEQ_PADD;
            return sequence;
        }
    }
}
