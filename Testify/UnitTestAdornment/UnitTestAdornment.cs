using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Leem.Testify.UnitTestAdornment
{
    public class UnitTestAdornment
    {
        public readonly ITrackingSpan Span;
        public readonly Poco.CoveredLinePoco CoveredLine;
        public double YPosition;
        public UnitTestAdornment(SnapshotSpan span, Poco.CoveredLinePoco coveredLine, double yPos)
        {
            this.Span = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
            this.CoveredLine = coveredLine;
            this.YPosition = yPos;
        }

    }
}
