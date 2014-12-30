using EnvDTE;

namespace Leem.Testify
{
    public interface ICoverageService
    {
        //List<LineCoverageInfo> CoveredLines { get; set; }

        //ITextDocument Document { get; set; }

        DTE DTE { set; }

        //IList<LineCoverageInfo> GetCoveredLinesFromCoverageSession(CoverageSession codeCoverage, string projectName);

        ITestifyQueries Queries { get; set; }

        string SolutionName { get; set; }
    }
}