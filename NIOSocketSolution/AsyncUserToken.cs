using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NIOSocketSolution
{
    public sealed class AsyncUserToken
    {
        public IPAddress IPAddress { get; set; }
        public EndPoint Remote { get; set; }
        public Socket Socket { get; set; }
        public DateTime ConnectTime { get; set; }
        public List<Byte> Buffer { get; set; }
        public String UserName { get; set; }
        public AsyncUserToken()
        {
            Buffer = new List<byte>();
        }
    }
}
