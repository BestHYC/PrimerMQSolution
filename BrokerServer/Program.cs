using BrokerServer.NIOClient;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BrokerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int recv;
            byte[] data = new byte[1024];
            IPEndPoint ipEnd = new IPEndPoint(IPAddress.Any, 5566);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipEnd);
            socket.Listen(10);
            Console.WriteLine("Waiting for a client");
            NioClientCollection nioClientList = new NioClientCollection();
            try
            {
                while (true)
                {
                    Socket client1 = socket.Accept();
                    nioClientList.Add(client1);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                socket.Close();
            }
        }
    }
}
