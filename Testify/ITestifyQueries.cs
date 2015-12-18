using ICSharpCode.NRefactory.TypeSystem;
using Leem.Testify.Model;
using Leem.Testify.Poco;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Leem.Testify
{
    public interface ITestifyQueries
    {
        event EventHandler<ClassChangedEventArgs> ClassChanged;
        void AddToTestQueue(string projectName);

        QueuedTest GetIndividualTestQueue(int testRunId);

        ProjectInfo GetProjectInfo(string uniqueName);

        ProjectInfo GetProjectInfoFromTestProject(string projectName);

        QueuedTest GetProjectTestQueue(int testRunId);

        void MaintainProjects(IList<Project> projects);

        void RemoveFromQueue(QueuedTest testQueueItem);

        Task<List<string>> SaveCoverageSessionResults(CoverageSession coverageSession, resultType testOutput, ProjectInfo projectInfo,
            List<string> individualTest);

        Dictionary<string, TestMethod> SaveUnitTestResults(resultType testOutput, Leem.Testify.Model.Module testModule, List<TrackedMethodMap> trackedMethodUnitTestMapper, List<string> changedUnitTestClasses, TestifyContext context);

        void SetAllQueuedTestsToNotRunning();

        CodeModule[] GetModules(TestifyContext context);

        IEnumerable<CodeClass> GetClasses(CodeModule module, TestifyContext context);

        IEnumerable<CodeMethod> GetMethods(CodeClass _class, TestifyContext context);

        void UpdateCodeMethodPath(TestifyContext context, CodeMethodInfo methodInfo, Dictionary<string, CodeMethod> codeMethodDictionary);

        string ConvertUnitTestFormatToFormatTrackedMethod(string testMethodName);

        CodeMethod GetMethod(string clickedMethodName);

        IEnumerable<TestMethod> GetUnitTestByName(string name);

        void AddTestsCoveringFileToTestQueue(string fileName, EnvDTE.Project project);

        void RemoveAllTestsFromQueue();
        string GetParameterList(IList<IUnresolvedParameter> unresolvedParameters);
    }
}