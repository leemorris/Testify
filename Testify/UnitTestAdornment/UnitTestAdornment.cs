using Microsoft.VisualStudio.Text;

namespace Leem.Testify.UnitTestAdornment
{
    public class UnitTestAdornment
    {
        public readonly ITrackingSpan Span;
        public readonly Poco.CoveredLinePoco CoveredLine;
        public readonly double YPosition;
        public UnitTestAdornment(SnapshotSpan span, Poco.CoveredLinePoco coveredLine, double yPos)
        {
            Span = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
            CoveredLine = coveredLine;
            YPosition = yPos;
        }

    }
}
