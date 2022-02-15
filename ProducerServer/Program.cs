using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProducerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int? a = null;
            a = 0;
            Console.WriteLine(a == 0);


            byte[] data = new byte[1024];
            string input, stringData;
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipEnd = new IPEndPoint(ip, 5566);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(ipEnd);
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
                input = Console.ReadLine();
                if (input == "exit")
                {
                    break;
                }
                socket.Send(Encoding.UTF8.GetBytes(input));
                data = new byte[1024];
                recv = socket.Receive(data);
                stringData = Encoding.UTF8.GetString(data, 0, recv);
                Console.WriteLine(stringData);
            }
            Console.WriteLine("disconnect from server");
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}
