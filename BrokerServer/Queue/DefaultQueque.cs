using BrokerServer.Message;
using NIOSocketSolution.Common;
using System;
using System.Collections.Concurrent;

namespace BrokerServer.Queue
{
    public class DefaultQueque
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// 是否持久化
        /// </summary>
        public Boolean Durable { get; set; }

        /*
         * 如果一个队列申明排他性,那么会对首次申明的队列可见, 并且在连接断开的时候自动删除
         * 注意
         * 1.此处是基于连接层次的, 即同一个socket,如果同一个连接的不同信道(Channel)也是可以访问排他队列
         * 2.首次是指名称的唯一性
         * 3.当申明了排他队列,连接释放的时候, 会被自动删除,即使是申明的 持久化或其他属性
         */
        /// <summary>
        /// 是否排他性
        /// </summary>
        public Boolean Exclusive { get; set; }
        /// <summary>
        /// 是否自动删除
        /// </summary>
        public Boolean AutoDelete { get; set; }
        /*
         一旦消息超过TTL时间, 那么此消息会变成死信(Dead Message),那么会放置到死信队列上,
        如果没有设置死信队列, 那么数据会被丢失
        对应的参数 是 x-message-ttl
         */
        /// <summary>
        /// Time to Live
        /// 单位:ms
        /// </summary>
        public Int32 TTL { get; set; }
        /*
         * 当队列处于处于休闲状态的时间,
         * 如若设置1000,那么当队列闲置时间超过1000ms,即1000ms内没任何消费者或者生产者那么此队列会被删除掉
         * 此处至于当前队列的使用时间比较,如果系统重启了,那么也会重新计算使用时间
         */
        /// <summary>
        /// 队列未使用的过期时间
        /// 单位:ms
        /// </summary>
        public Int32 Expires { get; set; }
        /// <summary>
        /// 队列最大长度
        /// 限定是 int型,即2的32次方
        /// </summary>
        public Int32 MaxLength { get; set; }
        /// <summary>
        /// 单次数据的字节长度最大值
        /// </summary>
        public Int32 MaxLengthBytes { get; set; }
        /// <summary>
        /// 死信队列交换器
        /// </summary>
        public String DeadLetterExchange { get; set; }
        /// <summary>
        /// 死信队列路由键
        /// </summary>
        public String DeadLetterRoutingKey { get; set; }
        /*
         * 优先级队列
         * 如果针对一个消费者,优先级高的队列优先被消费
         */
        /// <summary>
        /// 队列优先级
        /// </summary>
        public Int32 Priority { get; set; }
        /// <summary>
        /// 队列创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 队列修改时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
        private ConcurrentQueue<long> Queue = new ConcurrentQueue<long>();
        private ConcurrentBag<String> m_channelName = new ConcurrentBag<String>();
        /*队列由rabbit_amqqueue_process 和backing_queue 这两部分组成*/
        
        /*
         * 负责协议相关的消息处理,即接受生产者发布的消息,向消费者交付消息,
         * 处理消息的确认(包括生产端的confirm和消费端的ack)
         * 
         */
        /// <summary>
        /// 处理消息发送
        /// </summary>
        public virtual long Process(long nodeId)
        {

            return 0;
        }
        /// <summary>
        /// 消息存储的具体形式和引擎
        /// </summary>
        public virtual void Backing(long nodeId)
        {
            if (Durable)
            {
                if(MessageNodeCollection.TryGetValue(nodeId, out var node))
                {
                    LoggerHelper.Logger(Name, node.ToJson());
                }
            }
        }
    }
}
