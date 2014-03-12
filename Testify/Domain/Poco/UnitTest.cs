namespace Leem.Testify.Poco
{
    using System;
    using System.Collections.Generic;

    public class UnitTest
    {
        public UnitTest()
        {
            this.TrackedMethods = new HashSet<TrackedMethod>();
            this.CoveredLines = new HashSet<CoveredLine>();
        }

        public int UnitTestId { get; set; }
        public string TestProjectUniqueName { get; set; }
        public bool IsSuccessful { get; set; }
        public string TestMethodName { get; set; }
        public Nullable<int> NumberOfAsserts { get; set; }
        public bool Executed { get; set; }
        public string Result { get; set; }
        public string AssemblyName { get; set; }
        public string LastRunDatetime { get; set; }
        public DateTime? LastSuccessfulRunDatetime { get; set; }
        public string TestDuration { get; set; }
        public string LineNumber { get; set; }
        //public Nullable<int> CoveredLineCoveredLineId { get; set; }
        public int MetadataToken { get; set; }
        public virtual TestProject TestProject { get; set; }
        public virtual ICollection<TrackedMethod> TrackedMethods { get; set; }
        public virtual ICollection<CoveredLine> CoveredLines { get; set; }

    }
}