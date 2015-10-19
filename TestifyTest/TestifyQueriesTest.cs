using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Diagnostics;
using Task = System.Threading.Tasks.Task;
using log4net;

namespace Leem.Testify
{
    [TestFixture]
    public class TestifyQueriesTest
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TestifyQueriesTest));

        [TestCase("Quad.QuadMed.QMedClinicalTools.Domain.Services.AccountRequestService", 20000)]
        [TestCase("Quad.QuadMed.QMedClinicalTools.Domain.Test.Services.WellnessRecordValueServiceTest", 20000)]
        public void GetCoveredLinesTimeTest(string classname, int milliSeconds)
        {
            // Arrange
            var sw = Stopwatch.StartNew();
            var queries = TestifyQueries.Instance;

            // Act
            using (var context = new TestifyContext("C:\\WIP\\QMedClinicalTools\\QMedClinicalTools.sln"))
            {
                var lines = queries.GetCoveredLines(context, classname).ToList();
            }
            sw.Stop();

            // Assert
            Assert.LessOrEqual(sw.ElapsedMilliseconds,milliSeconds);
        
        }
        [Test]
        public void GetCoveredLines()
        {
            // Arrange
            var sw = Stopwatch.StartNew();
            var queries = TestifyQueries.Instance;

            // Act
            using (var context = new TestifyContext(@"C:\WIP\UnitTestExperiment"))
            {
                context.Database.Log = L => Log.Debug(L);
                var lines = queries.GetCoveredLines(context, "UnitTestExperiment.Domain.DosomethingElse").ToList();
                var newUnitTest = new Poco.TestMethod { AssemblyName="Assembly",TestMethodName="TestMethodName",LineNumber=123,Result="Yippee!"};
                lines.First().TestMethods.Add(newUnitTest);
                lines.First().TestMethods.Remove(lines.First().TestMethods.Last());
                context.SaveChanges();
                lines = queries.GetCoveredLines(context, "UnitTestExperiment.Domain.DosomethingElse").ToList();

            // Assert
            Assert.NotNull(lines);
            }
        }
    }
}
