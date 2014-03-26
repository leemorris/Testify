using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Leem.Testify;

using Leem.Testify.Model;

namespace Leem.Testify
{
    public interface ICoverageService
    {
        List<LineCoverageInfo> CoveredLines { get; set; }

        ITextDocument Document { get; set; }

        EnvDTE.DTE DTE { set; }

        IList<LineCoverageInfo> GetCoveredLinesFromCoverageSession(CoverageSession codeCoverage, string projectName);

        ITestifyQueries Queries { get; set; }

        string SolutionName { get; set; }
    }
}
