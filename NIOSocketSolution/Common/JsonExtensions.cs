using Newtonsoft.Json;
using System;
namespace NIOSocketSolution.Common
{
    public static class JsonExtensions
    {
        public static String ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
        public static T ToObject<T>(this String str)
        {
            if (str == null) return default(T);
            return JsonConvert.DeserializeObject<T>(str);
        }
    }
}
