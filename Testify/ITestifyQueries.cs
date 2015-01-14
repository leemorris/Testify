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

        Task<List<string>> SaveCoverageSessionResults(CoverageSession coverageSession, resultType testOutput, ProjectInfo projectInfo,
            List<string> individualTest);

        void SaveUnitTestResults(resultType testOutput, Module testModule);

        void SetAllQueuedTestsToNotRunning();

        CodeModule[] GetModules();

        IEnumerable<CodeClass> GetClasses(CodeModule module);

        IEnumerable<CodeMethod> GetMethods(CodeClass _class);

        void UpdateCodeMethodPath(CodeMethodInfo methodInfo);

        void UpdateMethods(IUnresolvedTypeDefinition fileClass, IEnumerable<IUnresolvedMethod> methods, string fileName);

        CodeMethod GetMethod(string clickedMethodName);

        IEnumerable<UnitTest> GetUnitTestByName(string name); 
    }
}