﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace NIOSocketSolution
{
    /// <summary>
    /// 缓存管理,共用一个缓存字节池,避免大量的 字节创建,增加GC
    /// </summary>
    internal sealed class BufferManager
    {
        private int m_numBytes;
        private byte[] m_buffer;
        private Stack<int> m_freeIndexPool;
        private int m_currentIndex;
        private int m_bufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }
        /// <summary>
        /// 创建一个共享字节数组,避免每次新建产生大量GC
        /// </summary>
        public void InitBuffer()
        {
            m_buffer = new byte[m_numBytes];
        }

        /// <summary>
        /// 将共享通道设置到每一个SocketAsyncEventArgs上
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
