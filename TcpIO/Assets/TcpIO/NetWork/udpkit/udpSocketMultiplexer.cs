﻿/*
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

using System.Threading;
namespace UdpKit {
    public class UdpSocketMultiplexer {
        int index;
        UdpSocket[] sockets;
        WaitHandle[] events;

        public UdpSocket[] Sockets {
            get { return sockets; }
        }

        internal UdpSocketMultiplexer (params UdpSocket[] sockets) {
            this.index = 0;
            this.sockets = sockets;
            this.events = new AutoResetEvent[this.sockets.Length];

            for (int i = 0; i < this.sockets.Length; ++i) {
                this.events[i] = this.sockets[i].EventsAvailable;
            }
        }

        public bool Wait () {
            return Wait(-1);
        }

        public bool Wait (int timeout) {
            UdpSocket socket;
            return Wait(timeout, out socket);
        }

        public bool Wait (int timeout, out UdpSocket socket) {
            int n = WaitHandle.WaitAny(this.events, timeout);

            if (n >= 0 && n < this.sockets.Length) {
                index = n;
                socket = this.sockets[n];
                return true;
            }

            socket = null;
            return false;
        }

        public bool Poll (out UdpEvent ev, out UdpSocket socket) {
            bool allowRestart = (index != 0);

        RESTART:
            for (int i = index; i < sockets.Length; ++i) {
                if (sockets[i].Poll(out ev)) {
                    index = i + 1;
                    socket = sockets[i];
                    return true;
                }
            }

            if (allowRestart) {
                index = 0;
                allowRestart = false;
                goto RESTART;
            }

            ev = default(UdpEvent);
            socket = null;
            return false;
        }
    }
}