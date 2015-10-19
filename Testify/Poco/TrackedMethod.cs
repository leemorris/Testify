using System.Collections.Generic;

namespace Leem.Testify.Poco
{
    public class TrackedMethod
    {

        public TrackedMethod()
        {
            CoveredLines = new HashSet<CoveredLine>();
            UnitTests = new HashSet<UnitTest>();
        }

        public int TrackedMethodId { get; set; }
        public CodeModule CodeModule { get; set; }
        public int UniqueId { get; set; }
        public string Name { get; set; }
        public string Strategy { get; set; }

        public string FileName { get; set; }

        public virtual ICollection<CoveredLine> CoveredLines { get; set; }
        public virtual ICollection<UnitTest> UnitTests { get; set; }
        public int MetadataToken { get; set; }

        public string NameInUnitTestFormat
        {
            get
            {
                // Convert This:
                // System.Void UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest::TestIt()
                // Into This:
                // UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest.TestIt
                if (string.IsNullOrEmpty(Name))
                {
                    return string.Empty;
                }
                int locationOfSpace = Name.IndexOf(' ') + 1;
                int locationOfParen = Name.IndexOf('(');
                string testMethodName = Name.Substring(locationOfSpace, locationOfParen - locationOfSpace);
                testMethodName = testMethodName.Replace("::", ".");
                return testMethodName;
            }
        }
    }
}