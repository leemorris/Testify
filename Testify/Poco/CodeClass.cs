using System.Collections.Generic;
using Leem.Testify.Model;

namespace Leem.Testify.Poco
{
    public class CodeClass
    {
        public CodeClass()
        {
            Methods = new HashSet<CodeMethod>();
        }

        public CodeClass(Class codeClass)
        {
            Methods = new HashSet<CodeMethod>();
            Name = codeClass.FullName;
            Summary = new Summary(codeClass.Summary);
        }

        public int CodeClassId { get; set; }

        public virtual CodeModule CodeModule { get; set; }
        public virtual ICollection<CodeMethod> Methods { get; set; }

        public string Name { get; set; }
        public virtual Summary Summary { get; set; }

        public string FileName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}