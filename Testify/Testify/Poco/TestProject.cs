namespace Leem.Testify.Poco
{
    using System.Collections.Generic;

    public class TestProject
    {
        public TestProject()
        {
            this.UnitTests = new HashSet<UnitTest>();
        }

        public string UniqueName { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public string OutputPath { get; set; }

        public string ProjectUniqueName { get; set; }

        public string AssemblyName { get; set; }

        public virtual Project Project { get; set; }

        public virtual ICollection<UnitTest> UnitTests { get; set; }
    }
}