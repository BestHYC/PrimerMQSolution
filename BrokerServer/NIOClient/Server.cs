using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace BrokerServer.NIOClient
{
    public class Server
    {
        private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously
        private int m_receiveBufferSize;// buffer size to use for each socket I/O operation
        BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        Socket listenSocket;            // the socket used to listen for incoming connection requests
                                        // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        SocketAsyncEventArgsPool m_readWritePool;
        int m_totalBytesRead;           // counter of the total # bytes received by the server
        int m_numConnectedSockets;      // the total number of clients connected to the server
        Semaphore m_maxNumberAcceptedClients;
        private event OnReceiveData ReceiveClientData;

        public Server(int numConnections, int receiveBufferSize)
        {
            m_totalBytesRead = 0;
            m_numConnectedSockets = 0;
            m_numConnections = numConnections;
            m_receiveBufferSize = receiveBufferSize;
            
            m_bufferManager = new BufferManager(receiveBufferSize * numConnections * opsToPreAlloc,
                receiveBufferSize);

            m_readWritePool = new SocketAsyncEventArgsPool(numConnections);
            m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
        }

        public void Init()
        {
            m_bufferManager.InitBuffer();

            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < m_numConnections; i++)
            {
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                readWriteEventArg.UserToken = new AsyncUserToken();

                m_bufferManager.SetBuffer(readWriteEventArg);

                m_readWritePool.Push(readWriteEventArg);
            }
        }

        public void Start(IPEndPoint localEndPoint)
        {
            Init();
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            listenSocket.Listen(100);

            StartAccept(null);
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

            SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
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
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    //读取数据  
                    byte[] data = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);
                    lock (token.Buffer)
                    {
                        token.Buffer.AddRange(data);
                    }
                    do
                    {
                        //判断包的长度  
                        byte[] lenBytes = token.Buffer.GetRange(0, 8).ToArray();
                        int packageLen = BitConverter.ToInt32(lenBytes, 0);
                        if (packageLen > token.Buffer.Count - 8)
                        {   //长度不够时,退出循环,让程序继续接收  
                            break;
                        }

                        //包够长时,则提取出来,交给后面的程序去处理  
                        byte[] rev = token.Buffer.GetRange(8, packageLen).ToArray();
                        //从数据池中移除这组数据  
                        lock (token.Buffer)
                        {
                            token.Buffer.RemoveRange(0, packageLen + 8);
                        }
                        //将数据包交给后台处理,这里你也可以新开个线程来处理.加快速度.  
                        if (ReceiveClientData != null)
                            ReceiveClientData(token, rev);
                        //这里API处理完后,并没有返回结果,当然结果是要返回的,却不是在这里, 这里的代码只管接收.  
                        //若要返回结果,可在API处理中调用此类对象的SendMessage方法,统一打包发送.不要被微软的示例给迷惑了.  
                    } while (token.Buffer.Count > 8);
                    bool willRaiseEvent = token.Socket.SendAsync(e);
                    if (!willRaiseEvent)
                    {
                        ProcessSend(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                bool willRaiseEvent = token.Socket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
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

            m_readWritePool.Push(e);

            m_maxNumberAcceptedClients.Release();
            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", m_numConnectedSockets);
        }
    }
        /// <summary>  
                /// 接收到客户端的数据  
                /// </summary>  
                /// <param name="token">客户端</param>  
                /// <param name="buff">客户端数据</param>  
        public delegate void OnReceiveData(AsyncUserToken token, byte[] buff);
    public class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> m_pool;

        public SocketAsyncEventArgsPool(int capacity)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        public SocketAsyncEventArgs Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        public int Count
        {
            get { return m_pool.Count; }
        }
    }
    /// <summary>
    /// 应用的是官方提供的文档, 请c#文档上搜索 BufferManager即可
    /// </summary>
    public class BufferManager
    {
        int m_numBytes;                 // the total number of bytes controlled by the buffer pool
        byte[] m_buffer;                // the underlying byte array maintained by the Buffer Manager
        Stack<int> m_freeIndexPool;     //
        int m_currentIndex;
        int m_bufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }

        // Allocates buffer space used by the buffer pool
        public void InitBuffer()
        {
            // create one big large buffer and divide that
            // out to each SocketAsyncEventArg object
            m_buffer = new byte[m_numBytes];
        }

        // Assigns a buffer from the buffer pool to the
        // specified SocketAsyncEventArgs object
        //
        // <returns>true if the buffer was successfully set, else false</returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
            }
            else
            {
                if ((m_numBytes - m_bufferSize) < m_currentIndex)
                {
                    return false;
                }
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            return true;
        }

        // Removes the buffer from a SocketAsyncEventArg object.
        // This frees the buffer back to the buffer pool
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
    public class AsyncUserToken
    {
        public IPAddress IPAddress { get; set; }
        public EndPoint Remote { get; set; }
        public Socket Socket { get; set; }
        public DateTime ConnectTime { get; set; }
        public List<Byte> Buffer { get; set; }
        public String UserName { get; set; }
        public AsyncUserToken()
        {
            Buffer = new List<byte>();
        }

    }
}
