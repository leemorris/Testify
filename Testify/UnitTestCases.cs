using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify
{
    public class TrackedMethodMap
    {
        public string TrackedMethodName { get; set; }
        public string CoverageSessionName { get; set; }
        public List<MethodInfo> MethodInfos { get; set; }
        
        
    }
}
