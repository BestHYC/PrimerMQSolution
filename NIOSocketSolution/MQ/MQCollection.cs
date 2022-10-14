using System;
using System.Collections.Generic;
using System.Text;

namespace NIOSocketSolution
{
    public class MQCollection
    {
        public static HashSet<AsyncUserToken> ALLToken = new HashSet<AsyncUserToken>();
        public static Dictionary<String, HashSet<AsyncUserToken>> dic = new Dictionary<string, HashSet<AsyncUserToken>>();
        public static void Add(String key, AsyncUserToken userToken)
        {
            if(!dic.TryGetValue(key, out var list))
            {
                list = new HashSet<AsyncUserToken>();
            }
            list.Add(userToken);
            ALLToken.Add(userToken);
        }
    }
    public class MQValueType
    {
        public String Key { get; set; }
        public String Operator { get; set; }
        public String Value { get; set; }
    }
}
