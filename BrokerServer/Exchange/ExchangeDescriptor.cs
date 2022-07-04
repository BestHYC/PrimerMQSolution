using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerServer.Exchange
{
    public class ExchangeDescriptor
    {
        public ExchangeTypeEnum Type { get; set; }
        public String Name { get; set; }
    }
    public enum ExchangeTypeEnum
    {
        Fanout, Direct,Headers,Topic
    }
}
