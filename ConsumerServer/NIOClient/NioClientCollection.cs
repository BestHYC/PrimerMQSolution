using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace ConsumerServer.NIOClient
{
    public class NioClientCollection
    {
        /// <summary>
        /// NIO请求日志及心跳集合
        /// 每1000个socket 起一个线程轮询去处理心跳,如果是 20000 那么就起20个线程
        /// Register 记录历史日志,并做压缩处理, 目的是 server 如果是单独pod 当停止的时候 会直接清除所有日志
        /// </summary>
        private NioClientDetail m_server = null;
        private SocketAsyncEventArgs m_receiveEventArg = null;
        private SocketAsyncEventArgs m_sendEvevntArg = null;
        private Object m_lock = new object();
        public void Add(Socket socket)
        {
            if (socket == null) return;
            lock (m_lock)
            {
                
                var client = new NioClientDetail()
                {
                    Socket = socket,
                    RecentDate = DateTime.Now,
                    IsDropped = false
                };
                m_server = client;

                IPEndPoint ipEndClient = (IPEndPoint)socket.RemoteEndPoint;
                SocketAsyncEventArgs sendEvevntArg = new SocketAsyncEventArgs();
                sendEvevntArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                sendEvevntArg.RemoteEndPoint = ipEndClient;
                sendEvevntArg.UserToken = client;
                m_sendEvevntArg = sendEvevntArg;
                if (!socket.SendAsync(sendEvevntArg))
                {
                    ProcessSend(sendEvevntArg);
                }
                SocketAsyncEventArgs receiveEventArg = new SocketAsyncEventArgs();
                receiveEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                receiveEventArg.RemoteEndPoint = ipEndClient;
                receiveEventArg.UserToken = client;
                m_receiveEventArg = receiveEventArg;
                //if (!socket.ReceiveAsync(receiveEventArg))
                //{
                //    ProcessReceive(receiveEventArg);
                //}
                
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success) return;
            NioClientDetail token = (NioClientDetail)e.UserToken;
            if (e.BytesTransferred > 0)
            {
                var receiveBuffer = e.Buffer;
                var buffer = new byte[e.BytesTransferred];
                Buffer.BlockCopy(receiveBuffer, 0, buffer, 0, e.BytesTransferred);
                Console.WriteLine(Encoding.UTF8.GetString(buffer));
                var willRaise =  token.Socket.ReceiveAsync(e);
                if (!willRaise)
                {
                    ProcessReceive(e);
                }
                SocketSend(e, "Welcome to my server");
            }
            else
            {
                //CloseClientSocket(e);
            }
        }
        public void SocketSend(String str)
        {
            NioClientDetail token = (NioClientDetail)m_sendEvevntArg.UserToken;
            string welcome = "Welcome to my server";
            var data = Encoding.UTF8.GetBytes(welcome);
            m_sendEvevntArg.SetBuffer(data, 0, data.Length);
            token.Socket.SendAsync(m_sendEvevntArg);
        }
        private void SocketSend(SocketAsyncEventArgs e, String str)
        {
            NioClientDetail token = (NioClientDetail)e.UserToken;
            string welcome = "Welcome to my server";
            var data = Encoding.UTF8.GetBytes(welcome);
            e.SetBuffer(data, 0, data.Length);
            bool willRaiseEvent = token.Socket.SendAsync(e);
            if (!willRaiseEvent)
            {
                //ProcessSend(e);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SocketSend(e, String.Empty);
                NioClientDetail token = (NioClientDetail)e.UserToken;
                bool willRaiseEvent = token.Socket.SendAsync(e);
                if (!willRaiseEvent)
                {
                    //ProcessSend(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            NioClientDetail token = e.UserToken as NioClientDetail;

            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) { }
            token.Socket.Close();
        }
    }
    public class NioClientDetail
    {
        /// <summary>
        /// 
        /// </summary>
        public Socket Socket { get; set; }
        /// <summary>
        /// 最近连接时间
        /// </summary>
        public DateTime RecentDate { get; set; }
        /// <summary>
        /// 是否掉线
        /// </summary>
        public Boolean IsDropped { get; set; }
    }
}
