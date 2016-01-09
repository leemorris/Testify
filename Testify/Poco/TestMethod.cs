using System;
using System.Collections.Generic;
using EntityFramework.Triggers;

namespace Leem.Testify.Poco
{
    public class TestMethod : ITriggerable
    {
        public TestMethod()
        {
            CoveredLines = new HashSet<CoveredLine>();
        }

        ////        public TestMethod(Model.TrackedMethod trackedMethod, UnitTest unitTest) : this()
        ////        {
        ////            //CodeModule = trackedMethod.CodeModule;
        ////            UniqueId = (int)trackedMethod.UniqueId;
        ////            Name = trackedMethod.Name;
        //////CoveredLines = trackedMethod.CoveredLines;
        ////            TestProjectUniqueName = unitTest.TestProjectUniqueName;
        ////            FilePath = unitTest.FilePath;
        ////            IsSuccessful = unitTest.IsSuccessful;
        ////            TestMethodName = unitTest.TestMethodName;
        ////            NumberOfAsserts = unitTest.NumberOfAsserts;
        ////            Executed = unitTest.Executed;
        ////            Result = unitTest.Result;
        ////            AssemblyName = unitTest.AssemblyName;
        ////            LastRunDatetime = unitTest.LastRunDatetime;
        ////            LastSuccessfulRunDatetime = unitTest.LastSuccessfulRunDatetime;
        ////            TestDuration = unitTest.TestDuration;
        ////            LineNumber = unitTest.LineNumber;
        ////            TestProject = unitTest.TestProject;
        ////        }

        public int TestMethodId { get; set; }
        public CodeModule CodeModule { get; set; }
        public int UniqueId { get; set; }
        public string Name { get; set; }
        public string Strategy { get; set; }

        public string FileName { get; set; }

        public virtual ICollection<CoveredLine> CoveredLines { get; set; }

        public int MetadataToken { get; set; }
        public string TestProjectUniqueName { get; set; }
        public string FilePath { get; set; }

        public bool IsSuccessful { get; set; }

        public string TestMethodName { get; set; }

        public int? NumberOfAsserts { get; set; }

        public bool Executed { get; set; }

        public string Result { get; set; }

        public string AssemblyName { get; set; }

        public string LastRunDatetime { get; set; }

        public DateTime? LastSuccessfulRunDatetime { get; set; }

        public string TestDuration { get; set; }

        public int LineNumber { get; set; }

        public int FailureLineNumber { get; set; }
        public string FailureMessage { get; set; }
        public virtual TestProject TestProject { get; set; }
    }
}