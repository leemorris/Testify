using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Diagnostics;
using Task = System.Threading.Tasks.Task;

namespace Leem.Testify
{
    [TestFixture]
    public class TestifyQueriesTest
    {
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


    }
}
