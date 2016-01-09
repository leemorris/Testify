using System.Collections.Generic;
using EntityFramework.Triggers;

namespace Leem.Testify.Poco
{
    public class Project : ITriggerable
    {
        public Project()
        {
            TestProjects = new HashSet<TestProject>();
        }

        public string UniqueName { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string AssemblyName { get; set; }

        public virtual ICollection<TestProject> TestProjects { get; set; }
        public virtual ICollection<TestMethod> TestMethods { get; set; }

        public string SourceControlVersion { get; set; }
    }
}