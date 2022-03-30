using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NIOSocketSolution
{
    public class SocketAsyncEventArgsPool
    {
        private Stack<SocketAsyncEventArgs> m_pool;
        /// <summary>
        /// 全局公用一个发送连接池,避免大量创建SocketAsyncEventArgsPool及字节对象信息
        /// 注意 此处可以用 共用连接池,但是会限定发送Socket的数量,后期自己取舍
        /// </summary>
        public static SocketAsyncEventArgsPool InstanceSend = new SocketAsyncEventArgsPool();
        public SocketAsyncEventArgsPool(int maxNum = 10)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(maxNum);
        }
        private Int32 m_pushLock = 0;
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) { throw new ArgumentNullException("SocketAsyncEventArgsPool cannot be null"); }
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
                if (func == null)
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
}
