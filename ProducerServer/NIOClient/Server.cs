using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ProducerServer.NIOClient
{
    public class Server
    {
        private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously
        private int m_receiveBufferSize;// buffer size to use for each socket I/O operation
        BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        Socket listenSocket;            // the socket used to listen for incoming connection requests
                                        // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        SocketAsyncEventArgsPool m_receivePool;
        SocketAsyncEventArgsPool m_sendPool;
        int m_numConnectedSockets;      // the total number of clients connected to the server
        Semaphore m_maxNumberAcceptedClients;
        private event OnReceiveData ExecuteReceiveData;

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
                byte[] data = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);
                token.Buffer.AddRange(data);
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
                    if (ExecuteReceiveData != null)
                        ExecuteReceiveData.BeginInvoke(token, result, ExecuteSendAsyncCallBack, token);
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
        private void ExecuteSendAsyncCallBack(IAsyncResult result)
        {
            AsyncUserToken token = (AsyncUserToken)result.AsyncState;
            String sendData = ExecuteReceiveData.EndInvoke(result);
            byte[] body = Encoding.UTF8.GetBytes(sendData);
            byte[] len = BitConverter.GetBytes(body.Length);
            byte[] buffer = new byte[body.Length + 4];
            Array.Copy(len, buffer, 4);
            Array.Copy(body, 0, buffer, 4, body.Length);
            int size = body.Length + 4, lenSize = 0;
            while (size > 0)
            {
                SocketAsyncEventArgs writeEventArgs = m_sendPool.Pop();
                int bufferSize = m_receiveBufferSize;
                if (size < bufferSize) bufferSize = size;
                Array.Copy(buffer, lenSize, writeEventArgs.Buffer, 0, bufferSize);
                lenSize += bufferSize;
                size -= bufferSize;
                if (!token.Socket.SendAsync(writeEventArgs))
                {
                    this.ProcessSend(writeEventArgs);
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
    /// <summary>  
    /// 接收到客户端的数据  
    /// </summary>  
    /// <param name="token">客户端</param>  
    /// <param name="buff">客户端数据</param>
    public delegate String OnReceiveData(AsyncUserToken token, String buff);
    public class SocketAsyncEventArgsPool
    {
        private Stack<SocketAsyncEventArgs> m_pool;
        public static SocketAsyncEventArgsPool InstanceSend = new SocketAsyncEventArgsPool();
        public SocketAsyncEventArgsPool(int maxNum = 10)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(maxNum);
        }
        private Int32 m_pushLock = 0;
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
            while (Interlocked.CompareExchange(ref m_pushLock, 1, 0) == 0)
            {
                Thread.Sleep(1);
            }
            m_pool.Push(item);
            Volatile.Write(ref m_pushLock, 0);
        }
        private Int32 m_popLock = 0;
        public SocketAsyncEventArgs Pop()
        {
            while (Interlocked.CompareExchange(ref m_popLock, 1, 0) == 0)
            {
                Thread.Sleep(1);
            }
            SocketAsyncEventArgs item = null;
            while (true)
            {
                while (Interlocked.CompareExchange(ref m_pushLock, 1, 0) == 0)
                {
                    Thread.Sleep(1);
                }
                if (m_pool.Count != 0)
                {
                    item = m_pool.Pop();
                }
                Volatile.Write(ref m_pushLock, 0);
                if (item != null) break;
                Thread.Sleep(1);
            }
            Volatile.Write(ref m_popLock, 0);
            return item;
        }
        public SocketAsyncEventArgs PopOrNew(Func<SocketAsyncEventArgs> func)
        {
            while (Interlocked.CompareExchange(ref m_popLock, 1, 0) == 0)
            {
                Thread.Sleep(1);
            }
            SocketAsyncEventArgs item = null;
            while (Interlocked.CompareExchange(ref m_pushLock, 1, 0) == 0)
            {
                Thread.Sleep(1);
            }
            if (m_pool.Count != 0)
            {
                item = m_pool.Pop();
            }
            else
            {
                if(func == null)
                {
                    item = new SocketAsyncEventArgs();
                }
                else
                {
                    item = func();
                }
                m_pool.Push(item);
            }
            Volatile.Write(ref m_pushLock, 0);
            Volatile.Write(ref m_popLock, 0);
            return item;
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
