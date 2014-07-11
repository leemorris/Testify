using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify.Poco
{
    public class CodeClass
    {
        public int CodeClassId { get; set; }

        public CodeClass()
        {
            this.Methods = new HashSet<CodeMethod>();
        }

        public CodeClass(Model.Class codeClass)
        {
            this.Methods = new HashSet<CodeMethod>();
            Name = codeClass.FullName;
            Summary = new Summary(codeClass.Summary);
        }
        public virtual CodeModule CodeModule { get; set; }
        public virtual ICollection<CodeMethod> Methods { get; set; }

        public string Name { get; set; }
        public virtual Summary Summary { get; set; }
    }
}
