using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify
{
    public class ProjectInfo
    {
        public Poco.TestProject TestProject {get; set; }
        public string ProjectName { get; set; }
        public string ProjectAssemblyName { get; set; }
    }
}
