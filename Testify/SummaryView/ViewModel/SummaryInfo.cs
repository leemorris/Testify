using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify.SummaryView.ViewModel
{
    public class SummaryInfo
    {
        private Poco.Summary _summary;

        public SummaryInfo()
        {

        }
        public SummaryInfo(Poco.Summary summary, string name)
        {
            _summary = summary;
            Name = name;
        }

        public string Name{get; set;}


        public int NumSequencePoints
        {
            get { return _summary.NumSequencePoints; }
        }

        public int NumBranchPoints
        {
            get { return _summary.NumBranchPoints; }
        }

        public decimal SequenceCoverage
        {
            get { return _summary.SequenceCoverage; }
        }

        public int VisitedBranchPoints
        {
            get { return _summary.VisitedBranchPoints; }
        }

        public int VisitedSequencePoints
        {
            get { return _summary.VisitedSequencePoints; }
        }

        public decimal BranchCoverage
        {
            get { return _summary.BranchCoverage; }
        }
    }
}
