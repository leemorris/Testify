using Leem.Testify.Model;
using System.Collections.Generic;
using EntityFramework.Triggers;

namespace Leem.Testify.Poco
{
    public class CodeModule : ITriggerable
    {
        public CodeModule()
        {
            Classes = new HashSet<CodeClass>();
            TestMethods = new HashSet<TestMethod>();
        }

        public CodeModule(Module module)
        {
            Classes = new HashSet<CodeClass>();
            Name = module.ModuleName;
            Summary = new Summary(module.Summary);
            AssemblyName = module.AssemblyName;
        }

        public int CodeModuleId { get; set; }

        public virtual ICollection<CodeClass> Classes { get; set; }
        public virtual ICollection<TestMethod> TestMethods { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public string AssemblyName { get; set; }
        public virtual Summary Summary { get; set; }
    }
}