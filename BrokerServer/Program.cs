using BrokerServer.Exchange;
using BrokerServer.Queue;
using NIOSocketSolution;
using System;
using System.Collections.Generic;
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
            
            
            server.Start(ipEnd);
            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }
    }
    /// <summary>
    /// 本来想写的复杂些,但是越些越难,反而把自己绕了进去, 就简单操作下结束
    /// 没必要总是实现重复的简单的逻辑,只要知道运行实现即可
    /// </summary>
    public class ClientCollection
    {
        private readonly Server m_server;
        private Dictionary<String, AsyncUserToken> m_client = new Dictionary<string, AsyncUserToken>();
        public ClientCollection(Server server)
        {
            m_server = server;
            server.OnReceiveComplete += (token, buff) =>
            {
                Console.WriteLine(buff);
                return buff;
            };
            server.OnAcceptComplete += token =>
            {
            };
        }
        /// <summary>
        /// 一个Socket 对应MQ中的 一个Channel
        /// 一个Channel可以订阅多个队列的消息或者发送数据给多个队列
        /// </summary>
        /// <param name="token"></param>
        public void AddChannel(AsyncUserToken token)
        {
            lock (this)
            {

            }
        }
        public void AddQueueToChannel(AsyncUserToken token, String queueName)
        {
            lock (this)
            {

            }
        }
        private void CreateQueue(QuequeDescriptor descriptor)
        {

        }
        private void CreateExchange(ExchangeDescriptor descriptor)
        {

        }
    }
}
