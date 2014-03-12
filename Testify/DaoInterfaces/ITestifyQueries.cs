using System;
using System.Linq;
using System.Collections.Generic;
using Leem.Testify.Domain;
using Leem.Testify.Domain.Model;
using System.Threading.Tasks;
using Leem.Testify.Poco;

namespace Leem.Testify.Domain.DaoInterfaces
{
    public interface ITestifyQueries
    {
        IList<CoveredLine> GetCoveredLines(string className);
        ProjectInfo GetProjectInfoFromTestProject(string projectName);

        ProjectInfo GetProjectInfo(string uniqueName);
        System.Linq.IQueryable<Project> GetProjects();
        IList<TestProject> GetTestProjects();
        void MaintainProjects(IList<Project> projects);
        Task<List<string>> SaveCoverageSessionResults(CoverageSession coverageSession, ProjectInfo projectInfo, List<string> individualTest);
        void SaveUnitTest(UnitTest test);
        void UpdateTrackedMethods(IList<Poco.TrackedMethod> trackedMethods);

        void SaveUnitTestResults(resultType testOutput);

        List<string> GetUnitTestsThatCoverLines(string className, string methodName, int lineNumber);
        List<Poco.UnitTest> GetUnitTestByName(string name);
  //      List<string> SaveResults(CoverageSession coverageSession, resultType testOutput, ProjectInfo projectInfo, List<string> individualTests);
    }
}
