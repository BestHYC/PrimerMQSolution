using ProducerServer.NIOClient;
using System;
using System.Net;
using System.Net.Sockets;

namespace BrokerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            IPEndPoint ipEnd = new IPEndPoint(IPAddress.Any, 5566);
            Server server = new Server(1000, 1024 * 1024);
            server.OnReceiveComplete += (buff) =>
            {
                Console.WriteLine(buff);
                return buff;
            };
            server.Start(ipEnd);
            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }
        private void A()
        {
            int recv;
            byte[] data = new byte[1024];
            IPEndPoint ipEnd = new IPEndPoint(IPAddress.Any, 5566);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipEnd);
            socket.Listen(10);
            Console.WriteLine("Waiting for a client");
            try
            {
                while (true)
                {
                    Socket client1 = socket.Accept();
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
