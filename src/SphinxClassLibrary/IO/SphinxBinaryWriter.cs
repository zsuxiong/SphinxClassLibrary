namespace SphinxClassLibrary.IO
{
    using System.IO;
    using System.Net;


    public class SphinxBinaryWriter
    {
        private System.IO.BinaryWriter bw;
        private System.Text.Encoding encoding;

        public SphinxBinaryWriter(Stream _stream, string _encoding = "UTF-8")
        {
            bw = new System.IO.BinaryWriter(_stream);
            encoding = System.Text.Encoding.GetEncoding(_encoding);
        }

        public Stream Stream { get { return bw.BaseStream; } }

        public void WriteShort(short _v) { bw.Write(IPAddress.HostToNetworkOrder(_v)); }

        public void WriteInt(int _v) { bw.Write(IPAddress.HostToNetworkOrder(_v)); }

        public void WriteLong(long _v) { bw.Write(IPAddress.HostToNetworkOrder(_v)); }

        public void WriteFloat(float _v)
        {
            IntFloatUnion ifu = new IntFloatUnion();
            ifu.f = _v;
            WriteInt(ifu.i);
        }

        public void WriteStr(string _v)
        {
            if (_v == null)
                WriteInt(0);
            else
            {
                byte[] bytes = encoding.GetBytes(_v);
                bw.Write(IPAddress.HostToNetworkOrder(bytes.Length));
                bw.Write(bytes);
            }
        }

        public void Flush() { bw.Flush(); }

        public long Seek(int _offset, SeekOrigin _origin) { return bw.Seek(_offset, _origin); }

        public void WriteStream(Stream _s)
        {
            byte[] buffer = new byte[_s.Length];
            long pos = _s.Position;
            _s.Position = 0;
            _s.Read(buffer, 0, (int)_s.Length);
            _s.Position = pos;
            bw.Write(buffer);
        }
    }
}
