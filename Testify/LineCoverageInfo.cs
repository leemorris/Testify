using System.Collections.Generic;
using System.Text;
using Leem.Testify.Poco;
using System.Linq;

namespace Leem.Testify
{
    public class LineCoverageInfo
    {
        public LineCoverageInfo()
        {
            TestMethods = new List<TestMethod>();
        }

        public CodeModule Module { get; set; }

        public CodeClass Class { get; set; }

        public CodeMethod Method { get; set; }

        public string ModuleName { get; set; }

        public string ClassName { get; set; }

        public string MethodName { get; set; }

        public int LineNumber { get; set; }

        public bool IsCode { get; set; }

        public bool IsCovered { get; set; }

        public List<TestMethod> TestMethods { get; set; }

         public string FileName { get; set; }

        public bool IsBranch { get; set; }


    }
    public static class LineCoverageInfoExtensions
    {
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> items,
                                    int numOfParts)
        {
            int i = 0;
            return items.GroupBy(x => i++ % numOfParts);
        }
    }
}
