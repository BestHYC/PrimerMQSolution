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
    public class Server
    {
        private int m_numConnections; 
        private int m_receiveBufferSize;
        private BufferManager m_bufferManager;
        private const int opsToPreAlloc = 2;
        private Socket listenSocket;
        private SocketAsyncEventArgsPool m_receivePool;
        private SocketAsyncEventArgsPool m_sendPool;
        private int m_numConnectedSockets;
        private Semaphore m_maxNumberAcceptedClients;
        public event OnReceiveComplete OnReceiveComplete;

        public Server(int numConnections, int receiveBufferSize)
        {
            m_numConnectedSockets = 0;
            m_numConnections = numConnections;
            m_receiveBufferSize = receiveBufferSize;

            m_bufferManager = new BufferManager(receiveBufferSize * numConnections * opsToPreAlloc,
                receiveBufferSize);
            m_receivePool = new SocketAsyncEventArgsPool(numConnections);
            m_sendPool = new SocketAsyncEventArgsPool(numConnections);
            m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
        }
        public void Init()
        {
            m_bufferManager.InitBuffer();
            for (int i = 0; i < m_numConnections; i++)
            {
                SocketAsyncEventArgs readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                readWriteEventArg.UserToken = new AsyncUserToken();
                m_bufferManager.SetBuffer(readWriteEventArg);
                m_receivePool.Push(readWriteEventArg);
                SocketAsyncEventArgs writeAtg = new SocketAsyncEventArgs();
                writeAtg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                m_bufferManager.SetBuffer(readWriteEventArg);
                m_sendPool.Push(writeAtg);
            }
        }
        public void Start(IPEndPoint localEndPoint, IPEndPoint ipEnd = null)
        {
            if(m_sendPool.Count == 0)
            {
                Init();
            }
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            listenSocket.Connect(ipEnd);
            listenSocket.Listen(100);
            StartAccept(null);
        }
        public void Connect(IPEndPoint ipEnd)
        {
            var result = listenSocket.BeginConnect(ipEnd, null, null);
            listenSocket.EndConnect(result);
        }
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                acceptEventArg.AcceptSocket = null;
            }

            m_maxNumberAcceptedClients.WaitOne();
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref m_numConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
                m_numConnectedSockets);

            SocketAsyncEventArgs readEventArgs = m_receivePool.Pop();
            ((AsyncUserToken)readEventArgs.UserToken).Socket = e.AcceptSocket;

            // As soon as the client is connected, post a receive to the connection
            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(readEventArgs);
            }
            StartAccept(e);
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

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0)
            {
                //读取数据  
                token.Buffer.AddArgsByte(e);
                while (token.Buffer.Count > 4)
                {
                    //判断包的长度  
                    byte[] lenBytes = token.Buffer.GetRange(0, 4).ToArray();
                    int packageLen = BitConverter.ToInt32(lenBytes, 0);
                    if (packageLen == 0) break;
                    if (packageLen > token.Buffer.Count - 4) break;
                    //包够长时,则提取出来,交给后面的程序去处理  
                    byte[] rev = token.Buffer.GetRange(4, packageLen).ToArray();
                    String result = UnicodeEncoding.Unicode.GetString(rev);
                    //从数据池中移除这组数据  
                    token.Buffer.RemoveRange(0, packageLen + 4);
                    //将数据包交给后台处理,这里你也可以新开个线程来处理.加快速度.  
                    if (OnReceiveComplete != null) 
                    {
                        Task.Run(() =>
                        {
                            return OnReceiveComplete.Invoke(result);
                        }).ContinueWith(result =>
                        {
                            ExecuteSendAsyncCallBack(token, result.Result);
                        });
                    }
                    //这里API处理完后,并没有返回结果,当然结果是要返回的,却不是在这里, 这里的代码只管接收.  
                    //若要返回结果,可在API处理中调用此类对象的SendMessage方法,统一打包发送.不要被微软的示例给迷惑了.  
                };
                //继续接收. 为什么要这么写,请看Socket.ReceiveAsync方法的说明  
                if (!token.Socket.ReceiveAsync(e))
                    this.ProcessReceive(e);
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void ExecuteSendAsyncCallBack(AsyncUserToken token, String sendData)
        {
            if (String.IsNullOrWhiteSpace(sendData)) return;
            byte[] body = Encoding.UTF8.GetBytes(sendData);
            byte[] len = BitConverter.GetBytes(body.Length);
            SocketAsyncEventArgs sendEventArgs = m_sendPool.Pop();
            Array.Copy(len, 0, sendEventArgs.Buffer, sendEventArgs.Offset, len.Length);
            int size = body.Length, lenSize = 0, bufferSize = m_receiveBufferSize - len.Length;
            if (size <= bufferSize)
            {
                bufferSize = size;
            }
            Array.Copy(body, lenSize, sendEventArgs.Buffer, sendEventArgs.Offset + len.Length, bufferSize);
            lenSize += bufferSize;
            size -= bufferSize;
            if (!token.Socket.SendAsync(sendEventArgs))
            {
                this.ProcessSend(sendEventArgs);
            }
            while (true)
            {
                if (size <= 0) break;
                sendEventArgs = m_sendPool.Pop();
                if (size <= m_receiveBufferSize)
                {
                    bufferSize = size;
                }
                else
                {
                    bufferSize = m_receiveBufferSize;
                }
                Array.Copy(body, lenSize, sendEventArgs.Buffer, sendEventArgs.Offset, bufferSize);
                sendEventArgs.SetBuffer(sendEventArgs.Offset, bufferSize);
                size -= bufferSize;
                lenSize += bufferSize;
                if (!token.Socket.SendAsync(sendEventArgs))
                {
                    this.ProcessSend(sendEventArgs);
                }
            }
        }
        private void SendWrapper(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            bool willRaiseEvent = token.Socket.SendAsync(e);
            if (!willRaiseEvent)
            {
                ProcessSend(e);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                m_sendPool.Push(e);
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) { }
            token.Socket.Close();

            Interlocked.Decrement(ref m_numConnectedSockets);

            m_receivePool.Push(e);
            m_maxNumberAcceptedClients.Release();
            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", m_numConnectedSockets);
        }
    }
}
