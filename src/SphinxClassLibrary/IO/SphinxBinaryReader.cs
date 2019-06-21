namespace SphinxClassLibrary.IO
{
    using System.IO;
    using System.Net;


    public class SphinxBinaryReader
    {
        private System.IO.BinaryReader br;
        private System.Text.Encoding encoding;

        public SphinxBinaryReader(Stream _stream, string _encoding = "UTF-8")
        {
            br = new System.IO.BinaryReader(_stream);
            encoding = System.Text.Encoding.GetEncoding(_encoding);
        }

        public Stream Stream { get { return br.BaseStream; } }

        public short ReadShort() { return IPAddress.NetworkToHostOrder(br.ReadInt16()); }

        public int ReadInt() { return IPAddress.NetworkToHostOrder(br.ReadInt32()); }

        public long ReadLong() { return IPAddress.NetworkToHostOrder(br.ReadInt64()); }

        public float ReadFloat()
        {
            IntFloatUnion ifu = new IntFloatUnion();
            ifu.i = IPAddress.NetworkToHostOrder(br.ReadInt32());
            return ifu.f;
        }

        public string ReadStr()
        {
            int c = IPAddress.NetworkToHostOrder(br.ReadInt32());
            return (c > 0) ? encoding.GetString(br.ReadBytes(c)) : string.Empty;
        }

        public byte[] ReadBytes(int _c) { return br.ReadBytes(_c); }
    }
}
