using ICSharpCode.NRefactory.TypeSystem;
using Leem.Testify.Model;
using Leem.Testify.Poco;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        Task<List<string>> SaveCoverageSessionResults(CoverageSession coverageSession, resultType testOutput, ProjectInfo projectInfo,
            List<string> individualTest);

        List<string> SaveUnitTestResults(resultType testOutput, Module testModule, List<TrackedMethodMap> trackedMethodUnitTestMap);

        void SetAllQueuedTestsToNotRunning();

        CodeModule[] GetModules();

        IEnumerable<CodeClass> GetClasses(CodeModule module);

        IEnumerable<CodeMethod> GetMethods(CodeClass _class);

        void UpdateCodeMethodPath(TestifyContext context, CodeMethodInfo methodInfo, Dictionary<string, CodeMethod> codeMethodDictionary);

        string ConvertUnitTestFormatToFormatTrackedMethod(string testMethodName);

        void RemoveMissingClasses(Module moduleModule, List<string> currentClassNames, List<TrackedMethodMap> trackedMethodUnitTestMapper);

        void RemoveMissingMethods(Module moduleModule, List<string> currentMethodNames, List<TrackedMethodMap> trackedMethodUnitTestMapper);

        CodeMethod GetMethod(string clickedMethodName);

        IEnumerable<TestMethod> GetUnitTestByName(string name);

        void AddTestsCoveringFileToTestQueue(string fileName, EnvDTE.Project project);

        void RemoveAllTestsFromQueue();
        string GetParameterList(IList<IUnresolvedParameter> unresolvedParameters);
    }
}