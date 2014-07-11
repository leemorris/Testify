using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Leem.Testify.Poco
{
    using System;
    using System.Collections.Generic;

    public class CoveredLinePoco
    {
        public CoveredLinePoco()
        {
            this.TrackedMethods = new HashSet<TrackedMethod>();
            this.UnitTests = new HashSet<UnitTest>();
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

        public virtual ICollection<TrackedMethod> TrackedMethods { get; set; }
        public virtual ICollection<UnitTest> UnitTests { get; set; }
    }
}
