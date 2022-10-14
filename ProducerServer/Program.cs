using NIOSocketSolution;
using NIOSocketSolution.Common;
using NIOSocketSolution.RedisCache;
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
            var ready = Console.ReadLine();
            Console.WriteLine(ready);
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
