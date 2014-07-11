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
            _summary = new Poco.Summary();
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
            set { _summary.NumSequencePoints = value; }
        }

        public int NumBranchPoints
        {
            get { return _summary.NumBranchPoints; }
            set { _summary.NumBranchPoints = value; }
        }

        public decimal SequenceCoverage
        {
            get { return _summary.SequenceCoverage; }
            set { _summary.SequenceCoverage = value; }
        }

        public int VisitedBranchPoints
        {
            get { return _summary.VisitedBranchPoints; }
            set { _summary.VisitedBranchPoints = value; }
        }

        public int VisitedSequencePoints
        {
            get { return _summary.VisitedSequencePoints; }
            set { _summary.VisitedSequencePoints = value; }
        }

        public decimal BranchCoverage
        {
            get { return _summary.BranchCoverage; }
            set { _summary.BranchCoverage = value; }
        }
    }
}
