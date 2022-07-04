using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace NIOSocketSolution
{
    /// <summary>
    /// 服务接收到Socket请求完成
    /// </summary>
    /// <param name="e"></param>
    public delegate void OnAcceptComplete(AsyncUserToken token);
    /// <summary>
    /// 客户端与服务端连接完成
    /// </summary>
    /// <param name="e"></param>
    public delegate void OnConnectComplete(AsyncUserToken token);
    /// <summary>  
    /// 接收到客户端的数据  
    /// </summary>  
    /// <param name="token">客户端</param>  
    /// <param name="buff">客户端数据</param>
    public delegate String OnReceiveComplete(AsyncUserToken token, String buff);
}
