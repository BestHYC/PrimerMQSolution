using System;
using System.Collections.Generic;
using System.Text;

namespace NIOSocketSolution.RedisCache
{
    public class RedisValueType
    {
        public String Key { get; set; }
        public String Operator { get; set; }
        public String Value { get; set; }
    }
    public class RedisValueCollection
    {
        public static Dictionary<String, String> Collection = new Dictionary<string, string>();
    }
}
