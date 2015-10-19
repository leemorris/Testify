using System;

namespace Leem.Testify.Poco
{
    public class TestQueue
    {
        public int TestQueueId { get; set; }
        public string ProjectName { get; set; }
        public string IndividualTest { get; set; }
        public int TestRunId { get; set; }
        public DateTime QueuedDateTime { get; set; }
        public DateTime? TestStartedDateTime { get; set; }
        public virtual TestMethod TestMethod { get; set; }
        public int Priority { get; set; }
    }
}