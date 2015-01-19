using System.Collections.Generic;

namespace Leem.Testify.Poco
{
    public class CoveredLinePoco
    {
        public CoveredLinePoco()
        {
            TrackedMethods = new HashSet<TrackedMethod>();
            UnitTests = new HashSet<UnitTest>();
        }

        public CodeModule Module { get; set; }
        public CodeClass Class { get; set; }
        public CodeMethod Method { get; set; }

        public int LineNumber { get; set; }
        public bool IsCode { get; set; }
        public bool IsCovered { get; set; }
        public int CoveredLineId { get; set; }
        public bool IsSuccessful { get; set; }
        public int UnitTestId { get; set; }
        public string FileName { get; set; }

        public virtual ICollection<TrackedMethod> TrackedMethods { get; set; }
        public virtual ICollection<UnitTest> UnitTests { get; set; }

        public bool IsBranch { get; set; }
    }
}