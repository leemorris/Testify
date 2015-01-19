using System.Collections.Generic;
using System.Text;
using Leem.Testify.Poco;

namespace Leem.Testify
{
    public class LineCoverageInfo
    {
        public LineCoverageInfo()
        {
            TrackedMethods = new List<TrackedMethod>();
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

        public List<TrackedMethod> TrackedMethods { get; set; }

        public List<UnitTest> UnitTests { get; set; }

        public string FileName { get; set; }

        public bool IsBranch { get; set; }
    }
}