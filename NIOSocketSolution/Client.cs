using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NIOSocketSolution
{
    public class Client
    {
        SocketAsyncEventArgsPool m_sendPool;
        private readonly Int32 m_bufferSize;
        public event OnReceiveComplete OnReceiveComplete;
        private List<byte> m_buffer;
        private Socket m_socket;
        public Client(Int32 bufferSize)
        {
            m_bufferSize = bufferSize;
            m_sendPool = SocketAsyncEventArgsPool.InstanceSend;
            m_buffer = new List<byte>(bufferSize);
        }
        public void StartConnect(IPEndPoint endPoint)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs connectEventArg = new SocketAsyncEventArgs();
            connectEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Connection);
            connectEventArg.RemoteEndPoint = endPoint;
            socket.ConnectAsync(connectEventArg);
            m_socket = socket;
        }
        public SocketAsyncEventArgs CreateArg()
        {
            SocketAsyncEventArgs readWriteEventArg = new SocketAsyncEventArgs();
            readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            var bytes = new byte[m_bufferSize];
            readWriteEventArg.SetBuffer(bytes, 0, m_bufferSize);
            return readWriteEventArg;
        }
        void IO_Connection(object sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine($"已成功链接服务器,{((IPEndPoint)e.RemoteEndPoint).AddressFamily}");
            ProcessConnection(e);
        }
        private void ProcessConnection(SocketAsyncEventArgs e)
        {
            SocketAsyncEventArgs sendEventArgs = m_sendPool.PopOrNew(CreateArg);
            bool willRaiseEvent = m_socket.ReceiveAsync(sendEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(sendEventArgs);
            }
        }
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
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
        /// 异步方式发送消息,每次会首先取缓存的SocketAsyncEventArgs,如果没,则创建新的
        /// 注意此处没共享字节,因为SocketAsyncEventArgs本身就是缓存,并且没有限制此对象数量(可加)
        /// </summary>
        /// <param name="sendData"></param>
        public void SendAsync(String sendData)
        {
            if (String.IsNullOrWhiteSpace(sendData)) return;
            byte[] body = Encoding.UTF8.GetBytes(sendData);
            byte[] len = BitConverter.GetBytes(body.Length);
            SocketAsyncEventArgs sendEventArgs = m_sendPool.PopOrNew(CreateArg);
            Array.Copy(len, 0, sendEventArgs.Buffer, sendEventArgs.Offset, len.Length);
            int size = body.Length, lenSize = 0, bufferSize = m_bufferSize - len.Length;
            if (size <= bufferSize)
            {
                bufferSize = size;
            }
            Array.Copy(body, lenSize, sendEventArgs.Buffer, len.Length, bufferSize);
            lenSize += bufferSize;
            size -= bufferSize;
            sendEventArgs.SetBuffer(0, bufferSize + len.Length);
            if (!m_socket.SendAsync(sendEventArgs))
            {
                this.ProcessSend(sendEventArgs);
            }
            while (true)
            {
                if (size <= 0) break;
                sendEventArgs = m_sendPool.PopOrNew(CreateArg);
                if (size <= m_bufferSize)
                {
                    bufferSize = size;
                }
                else
                {
                    bufferSize = m_bufferSize;
                }
                Array.Copy(body, lenSize, sendEventArgs.Buffer, sendEventArgs.Offset, bufferSize);
                sendEventArgs.SetBuffer(0, bufferSize);
                size -= bufferSize;
                lenSize += bufferSize;
                if (!m_socket.SendAsync(sendEventArgs))
                {
                    this.ProcessSend(sendEventArgs);
                }
            }
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0)
            {
                m_buffer.AddArgsByte(e);
                while (m_buffer.Count > 4)
                {
                    //判断包的长度  
                    byte[] lenBytes = m_buffer.GetRange(0, 4).ToArray();
                    int packageLen = BitConverter.ToInt32(lenBytes, 0);
                    if (packageLen > m_buffer.Count - 4) break;
                    //包够长时,则提取出来,交给后面的程序去处理  
                    byte[] rev = m_buffer.GetRange(4, packageLen).ToArray();
                    String result = Encoding.UTF8.GetString(rev);
                    //从数据池中移除这组数据  
                    m_buffer.RemoveRange(0, packageLen + 4);
                    //将数据包交给后台处理,这里你也可以新开个线程来处理.加快速度.  
                    //另外beginInvoke 在.net core 3.1平台不允许操作了
                    if (OnReceiveComplete != null)
                    {
                        Task.Run(() =>
                        {
                            return OnReceiveComplete.Invoke(result);
                        }).ContinueWith(result =>
                        {
                            ExecuteSendAsyncCallBack(result.Result);
                        });
                    }
                    //这里API处理完后,并没有返回结果,当然结果是要返回的,却不是在这里, 这里的代码只管接收.  
                    //若要返回结果,可在API处理中调用此类对象的SendMessage方法,统一打包发送.不要被微软的示例给迷惑了.  
                };
                //继续接收. 为什么要这么写,请看Socket.ReceiveAsync方法的说明  
                if (!m_socket.ReceiveAsync(e))
                    this.ProcessReceive(e);
            }
            else
            {
                CloseClientSocket();
            }
        }
        private void ExecuteSendAsyncCallBack(String sendData)
        {
            SendAsync(sendData);
        }
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            m_sendPool.Push(e);
            if (e.SocketError == SocketError.Success)
            {
                Console.WriteLine("发送消息成功");
            }
            else
            {
                CloseClientSocket();
            }
        }

        private void CloseClientSocket()
        {
            Console.WriteLine("发送消息失败");
            m_socket.Close();
        }
    }
}
