using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Leem.Testify
{
    public class TestQueue
    {
        public int TestQueueId { get; set; }
        public string ProjectName { get; set; }
        public string IndividualTest { get; set; }
        public int TestRunId { get; set; }
        public DateTime QueuedDateTime { get; set; }
    }
}
