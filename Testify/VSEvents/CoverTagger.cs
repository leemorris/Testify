using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using System.Diagnostics;
using log4net;
using System.Globalization;
using Microsoft.Win32;

namespace Leem.Testify
{
    internal class CoverTagger : ITagger<CoverTag>
    {
        private DTE _dte;
        private ITextBuffer _buffer;
        private IClassifier m_classifier;
        private SVsServiceProvider _serviceProvider;
        private int _lastVersionTagged;
        private CoverageService _coverageService;
        private ILog Log = LogManager.GetLogger(typeof(CoverTagger));
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        private CoverageProvider _coverageProvider;

        internal CoverTagger(IClassifier classifier, ITextBuffer buffer, SVsServiceProvider serviceProvider, ICoverageProviderBroker coverageProviderBroker)
        {
            Log.DebugFormat("Inside CoverTagger constructor");
            m_classifier = classifier;
            _buffer = buffer;
            _dte = (DTE)serviceProvider.GetService(typeof(DTE));

        //    _coverageProvider = coverageProviderBroker.GetCoverageProvider(buffer, _dte, serviceProvider );
            _serviceProvider = serviceProvider;
           
            ITextDocument document;
            _buffer.Properties.TryGetProperty(typeof(Microsoft.VisualStudio.Text.ITextDocument), out document);
            if (_dte != null) 
            {
                _coverageService = CoverageService.Instance;
                _coverageService.Document =  document;
                _coverageService.SolutionName=_dte.Solution.FullName;
            }

        }
    
 
        IEnumerable<ITagSpan<CoverTag>> ITagger<CoverTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            //if (_dte.ActiveDocument != null && _coverageService.Document != null && !_dte.ActiveDocument.Path.Contains(".test")) 
            //{
            //        foreach (SnapshotSpan span in spans)
            //    {
            //        var coveredLinesForThisDocument = _coverageProvider.GetCoveredLines(span);
            //        // figure out which line number we are actually on.
            //        var line = span.Snapshot.Lines.Where(x => x.Start.Position.Equals(span.Start.Position)).First().LineNumber;

            //        CoveredLine coveredLine;
   
            //        if (coveredLinesForThisDocument.TryGetValue(line + 1, out coveredLine))
            //        {
            //            foreach (ClassificationSpan classification in m_classifier.GetClassificationSpans(span))
            //            {

            //                if ( coveredLine.IsCode)
            //                {
            //                    if (coveredLine.IsSuccessful)
            //                    {
            //                        yield return new TagSpan<CoverTag>(span, new CoverTag(1)); // Green
            //                    }
            //                    else if (coveredLine.IsSuccessful == false)
            //                    {
            //                        yield return new TagSpan<CoverTag>(span, new CoverTag(3)); // Red
            //                    }
            //                    if ( coveredLine.IsCovered == false)
            //                    {
            //                        yield return new TagSpan<CoverTag>(span, new CoverTag(2)); // Orange
            //                    }

            //                }

            //            }
            //        }
            //        _lastVersionTagged = spans.First().Snapshot.Version.VersionNumber;
            //    }
            //}
            return null;

        }
    }
}
