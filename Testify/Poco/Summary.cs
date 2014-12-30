namespace Leem.Testify.Poco
{
    public class Summary
    {
        public Summary(Model.Summary summary)
        {
            BranchCoverage = summary.BranchCoverage;
            SequenceCoverage = summary.BranchCoverage;

            MaxCyclomaticComplexity = summary.MaxCyclomaticComplexity;
            MinCyclomaticComplexity = summary.MinCyclomaticComplexity;

            NumBranchPoints = summary.NumBranchPoints;
            NumSequencePoints = summary.NumSequencePoints;

            VisitedBranchPoints = summary.VisitedBranchPoints;
            VisitedSequencePoints = summary.VisitedSequencePoints;
        }

        public Summary()
        {
            // TODO: Complete member initialization
        }

        public int SummaryId { get; set; }
        public int NumSequencePoints { get; set; }
        public int VisitedSequencePoints { get; set; }
        public int NumBranchPoints { get; set; }
        public int VisitedBranchPoints { get; set; }
        public decimal SequenceCoverage { get; set; }
        public decimal BranchCoverage { get; set; }
        public int MaxCyclomaticComplexity { get; set; }
        public int MinCyclomaticComplexity { get; set; }
    }
}