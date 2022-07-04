using NIOSocketSolution;
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
            Client client = new Client(1024 * 1024);
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipConnect = new IPEndPoint(ip, 5566);
            client.StartConnect(ipConnect);
            client.OnReceiveComplete += result =>
            {
                Console.WriteLine(result);
                return "";
            };
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
    }
}
