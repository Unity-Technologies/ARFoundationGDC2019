using System;
using System.IO;
using System.Text;

namespace EditorRemoting
{
    public class PacketWriter
    {
        BinaryWriter writer;
        public MemoryStream packet;
        byte message;
        byte[] buffer = new byte[10 * 1024 * 1024];

        public static void CopyToStream(Stream src, Stream dst, byte[] buffer, int numBytes)
        {
            while (numBytes > 0)
            {
                int req = Math.Min(buffer.Length, numBytes);
                int read = src.Read(buffer, 0, req);
                dst.Write(buffer, 0, read);
                numBytes -= read;
            }
        }

        public void BeginMessage(byte message)
        {
            //SDebug.Assert(message == RemoteMessage.Invalid);

            this.message = message;
            packet.Position = 0;
            packet.SetLength(0);
        }

        public void EndMessage(Stream stream)
        {
            //SDebug.Assert(message != RemoteMessage.Invalid);

            // Write message header
            stream.WriteByte((byte)message);
            uint len = (uint)packet.Length;
            stream.WriteByte((byte)(len & 0xFF));
            stream.WriteByte((byte)((len >> 8) & 0xFF));
            stream.WriteByte((byte)((len >> 16) & 0xFF));
            stream.WriteByte((byte)((len >> 24) & 0xFF));

            // Write the message
            packet.Position = 0;
            CopyToStream(packet, stream, buffer, (int)packet.Length);

            message = 0;// RemoteMessage.Invalid;
        }

        public void CopyPacketToStream(Stream stream)
        {
            packet.Position = 0;
            CopyToStream(packet, stream, buffer, (int)packet.Length);
        }

        public void Write(bool value) { writer.Write(value); }
        public void Write(byte value) { writer.Write(value); }
        public void Write(int value) { writer.Write(value); }
        public void Write(uint value) { writer.Write(value); }
        public void Write(long value) { writer.Write(value); }
        public void Write(ulong value) { writer.Write(value); }
        public void Write(float value) { writer.Write(value); }
        public void Write(double value) { writer.Write(value); }
        public void Write(byte[] value) { writer.Write(value); }
        public void Write(byte[] value, int count) { writer.Write(value, 0, count); }

        public void Write(string value)
        {
            writer.Write((uint)value.Length);
            writer.Write(Encoding.UTF8.GetBytes(value));
        }

        public PacketWriter()
        {
            packet = new MemoryStream();
            writer = new BinaryWriter(packet);
            message = 0;
        }
    }
}