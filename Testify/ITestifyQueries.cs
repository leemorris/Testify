using System;
using System.Linq;
using System.Collections.Generic;
using Leem.Testify.Model;
using System.Threading.Tasks;
using Leem.Testify.Poco;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Editor;
using ICSharpCode.NRefactory.TypeSystem;

namespace Leem.Testify
{
    public interface ITestifyQueries
    {
        void AddToTestQueue(string projectName);

        void AddToTestQueue(TestQueue testQueue);

        IEnumerable<Poco.CoveredLinePoco> GetCoveredLines(TestifyContext context, string className);

        QueuedTest GetIndividualTestQueue(int testRunId);

    

        ProjectInfo GetProjectInfo(string uniqueName);

        ProjectInfo GetProjectInfoFromTestProject(string projectName);
        System.Linq.IQueryable<Project> GetProjects();

        QueuedTest GetProjectTestQueue(int testRunId);

        IList<TestProject> GetTestProjects();

        List<string> GetUnitTestsThatCoverLines(string className, string methodName, int lineNumber);

        void MaintainProjects(IList<Project> projects);

        void RemoveFromQueue(QueuedTest testQueueItem);

        Task RunTestsThatCoverLine(string projectName, string className, string methodName, int lineNumber);

        Task<List<string>> SaveCoverageSessionResults(CoverageSession coverageSession, ProjectInfo projectInfo, List<string> individualTest);
        
        void SaveUnitTest(UnitTest test);

        void SaveUnitTestResults(resultType testOutput);

        void SetAllQueuedTestsToNotRunning();

        void UpdateTrackedMethods(IList<Poco.TrackedMethod> trackedMethods);

        CodeModule[] GetModules();

        CodeClass[] GetClasses(CodeModule _module);

        CodeMethod[] GetMethods(CodeClass _class);

        Task<CodeModule[]> GetSummaries();

        string GetProjectFilePathFromMethod(string name);

        string GetProjectFilePathFromClass(string name);

        IVsTextView GetIVsTextView(string filePath);
        IWpfTextView GetWpfTextView(IVsTextView vTextView);

        void UpdateCodeClassPath(string className, string path, int line, int column);

        void UpdateCodeMethodPath(string methodName, string path, int line, int column);

        void UpdateMethods(IUnresolvedTypeDefinition fileClass, IEnumerable<IUnresolvedMethod> methods, string fileName);
    }
}
