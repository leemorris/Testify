using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Leem.Testify.Domain.Model;


namespace Leem.Testify.Domain
{

    public class LineCoverageInfo
    {
        public LineCoverageInfo()
        {
            TrackedMethods = new List<Poco.TrackedMethod>();
        }
        public string Module { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }
        public int LineNumber { get; set; }
        public bool IsCode { get; set; }
        public bool IsCovered { get; set; }
        public string MetadataToken { get; set; }
        public List<Poco.TrackedMethod> TrackedMethods { get; set; }
        public List<Poco.UnitTest> UnitTests { get; set; }
    }
}
