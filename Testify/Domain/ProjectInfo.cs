using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify.Domain
{
    public class ProjectInfo
    {
        public TestProject TestProject {get; set; }
        public string ProjectName { get; set; }
        public string ProjectAssemblyName { get; set; }
    }
}
