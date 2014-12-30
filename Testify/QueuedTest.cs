using System;
using System.Collections.Generic;

namespace Leem.Testify
{
    public class QueuedTest
    {
        public string ProjectName { get; set; }
        public List<string> IndividualTests { get; set; }
        public int TestRunId { get; set; }
        public DateTime TestStartTime { get; set; }
    }
}