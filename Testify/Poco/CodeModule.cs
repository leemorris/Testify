using System.Collections.Generic;
using Leem.Testify.Model;

namespace Leem.Testify.Poco
{
    public class CodeModule
    {
        public CodeModule()
        {
            Classes = new HashSet<CodeClass>();
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
        public string Name { get; set; }
        public string FileName { get; set; }
        public string AssemblyName { get; set; }
        public virtual Summary Summary { get; set; }
    }
}