using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ProducerServer.NIOClient
{
    class Client
    {
        SocketAsyncEventArgsPool m_sendPool;
        private Int32 m_bufferSize;
        private event OnReceiveData ExecuteReceiveData;
        private Socket m_socket;
        public Client(Int32 bufferSize)
        {
            m_bufferSize = bufferSize;
            m_sendPool = SocketAsyncEventArgsPool.InstanceSend;
        }
        public void StartConnect(IPEndPoint endPoint)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs readWriteEventArg = new SocketAsyncEventArgs();
            readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Connection);
            readWriteEventArg.RemoteEndPoint = endPoint;
            socket.ConnectAsync(readWriteEventArg);
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
            Console.WriteLine("已成功链接服务器");
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
        public void SendAsync(String sendData)
        {
            byte[] body = Encoding.UTF8.GetBytes(sendData);
            byte[] len = BitConverter.GetBytes(body.Length);
            byte[] buffer = new byte[body.Length + 4];
            Array.Copy(len, buffer, 4);
            Array.Copy(body, 0, buffer, 4, body.Length);
            int size = body.Length + 4, lenSize = 0;
            while (size > 0)
            {
                SocketAsyncEventArgs writeEventArgs = m_sendPool.PopOrNew(CreateArg);
                int bufferSize = m_bufferSize;
                if (size < bufferSize) bufferSize = size;
                Array.Copy(buffer, lenSize, writeEventArgs.Buffer, 0, bufferSize);
                writeEventArgs.SetBuffer(0, bufferSize);
                lenSize += bufferSize;
                size -= bufferSize;
                if (!m_socket.SendAsync(writeEventArgs))
                {
                    this.ProcessSend(writeEventArgs);
                }
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
                CloseClientSocket();
            }
        }
        private void ExecuteSendAsyncCallBack(IAsyncResult result)
        {
            String sendData = ExecuteReceiveData.EndInvoke(result);
            SendAsync(sendData);
        }
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                m_sendPool.Push(e);
            }
            else
            {
                CloseClientSocket();
            }
        }

        private void CloseClientSocket()
        {
            m_socket.Close();
        }
    }
}
