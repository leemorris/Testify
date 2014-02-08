using System;
using System.Linq;
using System.Collections.Generic;
using Leem.Testify;
using Leem.Testify.Domain.Model;
using System.Threading.Tasks;

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
        void UpdateTrackedMethods(IList<TrackedMethod> trackedMethods);

        void SaveUnitTestResults(resultType testOutput);

        List<string> GetUnitTestsThatCoverLines(string className, string methodName, int lineNumber);
    }
}
