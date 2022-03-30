using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace NIOSocketSolution
{
    public static class SocketHelperExtension
    {
        public static void AddArgsByte(this List<Byte> list, SocketAsyncEventArgs args)
        {
            for(int i = args.Offset; i < args.BytesTransferred; i++)
            {
                list.Add(args.Buffer[i]);
            }
        }
    }
}
