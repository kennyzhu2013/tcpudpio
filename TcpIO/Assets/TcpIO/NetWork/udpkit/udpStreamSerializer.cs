namespace UdpKit {
  public class UdpStreamSerializer : UdpSerializer<UdpStream> {
    public override bool Pack (UdpStream stream, UdpStream input, out UdpStream sent) {
      int writeOffset = 0;
      int writeLength = UdpMath.BytesRequired(input.Position);

      // we always send the entire thing
      sent = input;

      // copy data from input stream to network stream
      stream.WriteByteArray(input.ByteBuffer, writeOffset, writeLength);

      // done!
      return true;
    }

    public override bool Unpack (UdpStream stream, out UdpStream received) {
      int readOffset = UdpMath.BytesRequired(stream.Position);
      int readLength = UdpMath.BytesRequired(stream.Size - stream.Position);

      // allocate a new stream and copy data
      received = Connection.Socket.StreamPool.Acquire();
      received.WriteByteArray(stream.ByteBuffer, readOffset, readLength);
      received.Position = 0;
      received.Size = stream.Size - stream.Position;

      // done!
      return true;
    }
  }
}
