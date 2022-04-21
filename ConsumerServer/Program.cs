using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsumerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Convert.ToInt32(1000 / 7));
            using (HttpHelperPool httpHelper = new HttpHelperPool(10, "xxxxx"))
            {
            }
            Console.ReadLine();
        }
    }

    public class HttpHelperPool : IDisposable
    {
        ConcurrentStack<HttpClient> m_pool;
        private Int32 m_maxNum = 0;
        private Int32 m_num = 0;
        public HttpHelperPool(Int32 maxNum, String hostUrl)
        {
            m_pool = new ConcurrentStack<HttpClient>();
            m_maxNum = maxNum;
            Preprocess(hostUrl);
        }
        private void Preprocess(String hostUrl)
        {
            Boolean isHttps = false;
            if (hostUrl.Substring(0, 5) == "https")
            {
                isHttps = true;
            }
            var client = Pop(isHttps);
            var result = client.SendAsync(new HttpRequestMessage()
            {
                Method = new HttpMethod("HEAD"),
                RequestUri = new Uri(hostUrl + "/")
            }).Result.IsSuccessStatusCode;
            Push(client);
        }
        private Object m_lock = new object();
        private HttpClient Pop(Boolean isHttps)
        {
            lock (m_lock)
            {
                HttpClient client = null;
                if (m_pool.Count == 0)
                {
                    if (m_num > m_maxNum)
                    {
                        return client;
                    }
                    m_num++;
                    client = new HttpClient();
                    client.DefaultRequestHeaders.Connection.Add("keep-alive");
                    if (isHttps)
                    {
                        //ServicePointManager.DefaultConnectionLimit = 200;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                    }
                    return client;
                }
                else
                {
                    if (m_pool.TryPop(out client))
                    {
                        return client;
                    }
                }
                return null;
            }
        }
        private void Push(HttpClient client)
        {
            m_pool.Push(client);
        }

        public string Post(string url, string postData = null, Dictionary<string, string> headers = null, string contentType = "application/json", int timeOut = 30)
        {
            String result = String.Empty;
            postData = postData ?? "";
            Boolean isHttps = false;
            if (url.Substring(0, 5) == "https")
            {
                isHttps = true;
            }
            HttpClient client = null;

            while (client == null)
            {
                client = Pop(isHttps);
                if(client == null)
                {
                    Thread.Sleep(10);
                }
            }
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                HttpRequestMessage message = new HttpRequestMessage();
                message.Content = new StringContent(postData, Encoding.UTF8);
                if (contentType != null)
                    message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                foreach (var header in headers)
                {
                    message.Headers.Add(header.Key, header.Value);
                }
                message.Method = HttpMethod.Post;
                message.RequestUri = new Uri(url);
                HttpResponseMessage response = client.SendAsync(message).Result;
                result = response.Content.ReadAsStringAsync().Result;
                sw.Stop();
                Console.WriteLine($"耗时:{sw.ElapsedMilliseconds}");
                
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Push(client);
            }
            return result;
        }
        public void Dispose()
        {
            Console.WriteLine("执行Dispose");
            foreach (var item in m_pool)
            {
                item.Dispose();
            }
        }
    }
}
