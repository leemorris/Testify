using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Leem.Testify.Model;


namespace Leem.Testify
{

    public class LineCoverageInfo
    {
        public LineCoverageInfo()
        {
            TrackedMethods = new List<Poco.TrackedMethod>();
        }

        public Poco.CodeModule Module { get; set; }
        public Poco.CodeClass Class { get; set; }
        public Poco.CodeMethod Method { get; set; }
        public string ModuleName { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public int LineNumber { get; set; }
        public bool IsCode { get; set; }
        public bool IsCovered { get; set; }
        public string MetadataToken { get; set; }
        public List<Poco.TrackedMethod> TrackedMethods { get; set; }
        public List<Poco.UnitTest> UnitTests { get; set; }
    }
}
