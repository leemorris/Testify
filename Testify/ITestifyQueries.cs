using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.TypeSystem;
using Leem.Testify.Model;
using Leem.Testify.Poco;

namespace Leem.Testify
{
    public interface ITestifyQueries
    {
        void AddToTestQueue(string projectName);

        QueuedTest GetIndividualTestQueue(int testRunId);

        ProjectInfo GetProjectInfo(string uniqueName);

        ProjectInfo GetProjectInfoFromTestProject(string projectName);

        QueuedTest GetProjectTestQueue(int testRunId);

        void MaintainProjects(IList<Project> projects);

        void RemoveFromQueue(QueuedTest testQueueItem);

        Task<List<string>> SaveCoverageSessionResults(CoverageSession coverageSession, ProjectInfo projectInfo,
            List<string> individualTest);

        void SaveUnitTestResults(resultType testOutput);

        void SetAllQueuedTestsToNotRunning();

        CodeModule[] GetModules();

        IEnumerable<CodeClass> GetClasses(CodeModule module);

        IEnumerable<CodeMethod> GetMethods(CodeClass _class);

        void UpdateCodeMethodPath(CodeMethodInfo methodInfo);

        void UpdateMethods(IUnresolvedTypeDefinition fileClass, IEnumerable<IUnresolvedMethod> methods, string fileName);

        CodeMethod GetMethod(string clickedMethodName);
        IEnumerable<UnitTest> GetUnitTestByName(string name); /*
                Task<CodeModule[]> GetSummaries();

                string GetProjectFilePathFromMethod(string name);

                string GetProjectFilePathFromClass(string name);
          
                void UpdateCodeClassPath(string className, string path, int line, int column);
                IVsTextView GetIVsTextView(string filePath);
                IWpfTextView GetWpfTextView(IVsTextView vTextView);
                Task RunTestsThatCoverLine(string projectName, string className, string methodName, int lineNumber);
                void UpdateTrackedMethods(IEnumerable<TrackedMethod> trackedMethods);
                void SaveUnitTest(UnitTest test);
                IList<TestProject> GetTestProjects();
                System.Linq.IQueryable<Project> GetProjects();
                List<string> GetUnitTestsThatCoverLines(string className, string methodName, int lineNumber);
                void AddToTestQueue(TestQueue testQueue);

                IEnumerable<Poco.CoveredLinePoco> GetCoveredLines(TestifyContext context, string className);
        */
    }
}