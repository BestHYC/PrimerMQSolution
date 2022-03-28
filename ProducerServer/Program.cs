using ProducerServer.NIOClient;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ProducerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client(1024 * 1024);
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipConnect = new IPEndPoint(ip, 5566);
            client.StartConnect(ipConnect);
            string input;
            while (true)
            {
                input = Console.ReadLine();
                if (input == "exit")
                {
                    break;
                }
                client.SendAsync(input);
            }
            Console.ReadKey();
        }
        private void SocketBlock()
        {
            var a11 = Encoding.UTF8.GetBytes(String.Format("{0,8}", 1));
            int? a = null;
            a = 0;
            Console.WriteLine(a == 0);

            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipEnd = new IPEndPoint(ip, 5566);
            byte[] data = new byte[1024];
            string input, stringData;
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
                byte[] body = Encoding.UTF8.GetBytes(input);
                byte[] len = BitConverter.GetBytes(body.Length);
                byte[] buffer = new byte[body.Length + 4];
                Array.Copy(len, buffer, 4);
                Array.Copy(body, 0, buffer, 4, body.Length);
                socket.Send(buffer);
                data = new byte[1024];
                while (recv < body.Length)
                {
                    int r = socket.Receive(data);
                    stringData = Encoding.UTF8.GetString(data, 0, r);
                    Console.WriteLine(stringData);
                    recv += r;
                }
                recv = 0;


            }
            Console.WriteLine("disconnect from server");
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}
