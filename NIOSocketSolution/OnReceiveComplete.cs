using System;
using System.Collections.Generic;
using System.Text;

namespace ProducerServer.NIOClient
{
    /// <summary>  
    /// 接收到客户端的数据  
    /// </summary>  
    /// <param name="token">客户端</param>  
    /// <param name="buff">客户端数据</param>
    public delegate String OnReceiveComplete(String buff);
}
