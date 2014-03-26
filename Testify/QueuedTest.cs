using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
