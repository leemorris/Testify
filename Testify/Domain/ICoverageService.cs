using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Leem.Testify.Domain;
using Leem.Testify.Domain.DaoInterfaces;
using Leem.Testify.Domain.Model;

namespace Leem.Testify
{
    public interface ICoverageService
    {
        List<LineCoverageInfo> CoveredLines { get; set; }
        ITextDocument Document { get; set; }
        EnvDTE.DTE DTE { set; }
        IList<LineCoverageInfo> GetCoveredLinesFromCoverageSession(CoverageSession codeCoverage, string projectName);
        IList<CoveredLine> GetCoveredLinesForClass(string className);
        ITestifyQueries Queries { get; set; }
        string SolutionName { get; set; }
    }
}
