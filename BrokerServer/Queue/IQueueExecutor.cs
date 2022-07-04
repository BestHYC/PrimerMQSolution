using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerServer.Queue
{
    public interface IQueueExecutor
    {
        long Process();
        void Backing();
    }
}
