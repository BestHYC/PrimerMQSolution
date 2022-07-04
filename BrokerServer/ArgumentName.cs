using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerServer
{
    /// <summary>
    /// 队列中固有的名词补充
    /// </summary>
    public static class QueueArgumentName
    {
        /// <summary>
        /// 消息的过期时间
        /// </summary>
        public const String TTL = "x-message-ttl";
        /// <summary>
        /// 队列的过期时间
        /// </summary>
        public const String Expire = "x-expires";
        /// <summary>
        /// 队列的最大长度
        /// </summary>
        public const String MaxLength = "x-max-length";
        /// <summary>
        /// 单次消息的最大长度
        /// </summary>
        public const String MaxLengthBytes = "x-max-length-bytes";
        /// <summary>
        /// 死信队列交换器
        /// </summary>
        public const String DeadLetterExchange = "x-dead-letter-exchange";
        /// <summary>
        /// 死信队列路由键
        /// </summary>
        public const String DeadLetterRoutingKey = "x-dead-letter-routing-key";
        /// <summary>
        /// 队列消息优先级
        /// </summary>
        public const String MaxPriority = "x-max-priority";
    }

}
