using NIOSocketSolution.Common;
using System;
using System.Collections.Concurrent;

namespace BrokerServer.Message
{
    /// <summary>
    /// 消息节点
    /// </summary>
    public struct MessageNode
    {
        /// <summary>
        /// 消息名
        /// 取值为yyyyMMddHHmmssfff+
        /// </summary>
        public long  ID { get; set; }
        /// <summary>
        /// 是否持久化
        /// </summary>
        public Boolean Durable { get; set; }
        /// <summary>
        /// Time to Live
        /// 单位:ms
        /// </summary>
        public Int32 TTL { get; set; }
        /// <summary>
        /// 消息体序列化后的数据
        /// </summary>
        public String Body { get; set; }
    }
    public class MessageNodeCollection
    {
        public static ConcurrentDictionary<long, MessageNode> Values = new ConcurrentDictionary<long, MessageNode>();
        private static Object m_lock = new object();
        public static long AddNode(MessageNode node)
        {
            long id = SnowFlake.Instance.NextId();
            node.ID = id;
            Values.TryAdd(id, node);
            return id;
        }
        public static bool TryGetValue(long id, out MessageNode node)
        {
            return Values.TryGetValue(id, out node);
        }
    }
}
