using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BrokerServer.Common
{
    /// <summary>
    /// 这里为了展示就随意写Logger日志记录了
    /// MQ底层的日志记录会好很多
    /// </summary>
    public static class LoggerHelper
    {
        /// <summary>
        /// 每个queue 间隔 1分钟创建新的文件
        /// </summary>
        private static DateTime m_time = DateTime.Now;
        private static String m_path = $"{System.Environment.CurrentDirectory}/Back";
        public static void Logger(String queueName, String body)
        {
            File.AppendAllText($"{m_path}/{queueName}/{DateTime.Now.ToString("yyyyMMddHHmm")}.txt", body);
        }
        private static ConcurrentBag<String> m_all = new ConcurrentBag<string>();
        public static void Logger(String body)
        {
            File.AppendAllText($"{m_path}/{DateTime.Now.ToString("yyyyMMddHHmm")}.txt", body);
        }
    }
}
