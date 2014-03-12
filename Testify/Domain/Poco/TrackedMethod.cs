using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify.Poco
{
    public class TrackedMethod
    {
        public TrackedMethod()
        {
            this.CoveredLines = new HashSet<CoveredLine>();
            this.UnitTests = new HashSet<UnitTest>();
        }

        public int UniqueId { get; set; }
        public string Name { get; set; }
        public string Strategy { get; set; }
        public int UnitTestId { get; set; }

        public virtual ICollection<CoveredLine> CoveredLines { get; set; }
        public virtual ICollection<UnitTest> UnitTests { get; set; }
        public int MetadataToken { get; set; }
        public string NameInUnitTestFormat 
        { get 
            {
                // Convert This:
                // System.Void UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest::TestIt()
                // Into This:
                // UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest.TestIt
                if (string.IsNullOrEmpty(this.Name))
                {
                    return string.Empty;
                }
                else
                {
                    int locationOfSpace = this.Name.IndexOf(' ') + 1;
                    int locationOfParen = this.Name.IndexOf('(');
                    var testMethodName = this.Name.Substring(locationOfSpace, locationOfParen - locationOfSpace);
                    testMethodName = testMethodName.Replace("::", ".");
                    return testMethodName;
                }
            }
        }
    }
}
