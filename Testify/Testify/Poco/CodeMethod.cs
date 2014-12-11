using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify.Poco
{
    public class CodeMethod
    {

        public int CodeMethodId { get; set; }
        public CodeMethod()
        {
        }

        public CodeMethod(Model.Method method)
        {

            Name = method.Name;
            Summary = new Summary(method.Summary);
        }

        public CodeClass CodeClass { get; set; }

        public int? CodeClassId { get; set; } 
        public string Name { get; set; }
        public string FileName { get; set; }
        public virtual Summary Summary { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}
