using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify
{
    using System;
    using System.Collections.Generic;

    public class Project
    {
        public Project()
        {
            this.TestProjects = new HashSet<Poco.TestProject>();
        }

        public string UniqueName { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string AssemblyName { get; set; }

        public virtual ICollection<Poco.TestProject> TestProjects { get; set; }
    }
}

