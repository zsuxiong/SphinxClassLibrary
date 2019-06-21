namespace SphinxClassLibrary.Net
{
    using System;
    using System.Net.Sockets;
    using SphinxClassLibrary.IO;


    class SphinxConnection : IDisposable
    {
        private TcpClient tcpclient = new TcpClient();

        public SphinxConnection(String _host, int _port)
        {
            tcpclient.Connect(_host, _port);
            SphinxBinaryWriter bw = new SphinxBinaryWriter(tcpclient.GetStream());
            bw.WriteInt(1); // dummy write (Nagle)
            SphinxBinaryReader br = new SphinxBinaryReader(tcpclient.GetStream());
            if (br.ReadInt() < 1)
                throw new SphinxClientException("Server version < 1");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && tcpclient.Connected)
                tcpclient.Close();
        }

        public NetworkStream Stream { get { return tcpclient.GetStream(); } }
    }
}
