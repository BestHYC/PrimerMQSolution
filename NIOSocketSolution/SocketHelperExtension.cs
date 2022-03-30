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
            int len = args.Offset + args.BytesTransferred;
            for (int i = args.Offset; i < len; i++)
            {
                list.Add(args.Buffer[i]);
            }
        }
    }
}
