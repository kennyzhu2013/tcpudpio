using System.Collections.Generic;

namespace UdpKit {
  public class UdpStreamPool {
    readonly UdpSocket socket;
    readonly Stack<UdpStream> pool = new Stack<UdpStream>();

    internal UdpStreamPool (UdpSocket s) {
      socket = s;
    }

    internal void Release (UdpStream stream) {
      UdpAssert.Assert(stream.IsPooled == false);

      lock (pool) {
        stream.Size = 0;
        stream.Position = 0;
        stream.IsPooled = true;

        pool.Push(stream);
      }
    }

    public UdpStream Acquire () {
      UdpStream stream = null;

      lock (pool) {
        if (pool.Count > 0) {
          stream = pool.Pop();
        }
      }

      if (stream == null) {
        stream = new UdpStream(new byte[socket.Config.PacketSize * 2]);
        stream.Pool = this;
      }

      UdpAssert.Assert(stream.IsPooled);

      stream.IsPooled = false;
      stream.Position = 0;
      stream.Size = (socket.Config.PacketSize - UdpMath.BytesRequired(UdpSocket.HeaderBitSize)) << 3;

      return stream;
    }

    public void Free () {
      lock (pool) {
        while (pool.Count > 0) {
          pool.Pop();
        }
      }
    }
  }
}