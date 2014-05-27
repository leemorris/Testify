using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify.Poco
{
    public class CodeModule
    {
        public int CodeModuleId { get; set; }
    
        public CodeModule()
        {
            this.Classes = new HashSet<CodeClass>();
        }

        public CodeModule(Model.Module module)
        {
            this.Classes = new HashSet<CodeClass>();
            Name = module.ModuleName;
            Summary = new Summary(module.Summary);
        }
        public virtual ICollection<CodeClass> Classes { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public string AssemblyName { get; set; }
        public virtual Summary Summary { get; set; }
    }
}
