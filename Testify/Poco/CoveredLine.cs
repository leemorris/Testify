using System.Collections.Generic;

namespace Leem.Testify.Poco
{
    public class CoveredLine
    {
        public CoveredLine()
        {
            TestMethods = new HashSet<TestMethod>();

        }

        public CodeModule Module { get; set; }
        public CodeClass Class { get; set; }
        public CodeMethod Method { get; set; }

        public int LineNumber { get; set; }
        public bool IsCode { get; set; }
        private bool _IsCovered = false;
        public bool IsCovered { 
            get { 
                return _IsCovered; 
            } 
            set 
            {
                if (_IsCovered != value)
                {
                    _IsCovered = value; 
                }
                
            } 
        }
        public int CoveredLineId { get; set; }
        public bool IsSuccessful { get; set; }
        public int UnitTestId { get; set; }
        public string FileName { get; set; }

        public virtual ICollection<TestMethod> TestMethods { get; set; }

        public decimal BranchCoverage { get; set; }
        public bool IsBranch { get; set; }
    }
}