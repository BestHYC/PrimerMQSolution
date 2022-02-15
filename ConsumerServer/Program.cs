using ConsumerServer.NIOClient;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ConsumerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipEnd = new IPEndPoint(ip, 5566);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            NioClientCollection collection = new NioClientCollection();
            try
            {
                socket.Connect(ipEnd);
                
                collection.Add(socket);
            }
            catch (SocketException e)
            {
                Console.WriteLine("Fail to connect server");
                Console.WriteLine(e.ToString());
                return;
            }
            int recv = 0;// socket.Receive(data);
            //stringData = Encoding.ASCII.GetString(data, 0, recv);
            //Console.WriteLine(stringData);
            while (true)
            {
                String input = Console.ReadLine();
                if (input == "exit")
                {
                    break;
                }
                collection.SocketSend(input);
            }
            Console.WriteLine("disconnect from server");
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}
