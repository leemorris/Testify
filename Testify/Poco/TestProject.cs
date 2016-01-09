using System.Collections.Generic;
using EntityFramework.Triggers;

namespace Leem.Testify.Poco
{
    public class TestProject : ITriggerable
    {
        public TestProject()
        {
            TestMethods = new HashSet<TestMethod>();
        }

        public string UniqueName { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public string OutputPath { get; set; }

        public string ProjectUniqueName { get; set; }

        public string AssemblyName { get; set; }

        public virtual Project Project { get; set; }

        public virtual ICollection<TestMethod> TestMethods { get; set; }
    }
}