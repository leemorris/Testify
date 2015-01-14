using EnvDTE;

namespace Leem.Testify
{
    public interface ICoverageService
    {
        DTE DTE { set; }

        ITestifyQueries Queries { get; set; }

        string SolutionName { get; set; }
    }
}