using Leem.Testify.Model;
using EntityFramework.Triggers;

namespace Leem.Testify.Poco 
{
    public class CodeMethod : ITriggerable
    {
        public CodeMethod()
        {
        }

        public CodeMethod(Method method)
        {
            Name = method.Name;
            Summary = new Summary(method.Summary);
        }

        public int CodeMethodId { get; set; }

        public CodeClass CodeClass { get; set; }

        public int? CodeClassId { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public virtual Summary Summary { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}