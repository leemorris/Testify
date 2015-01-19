using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EnvDTE;
using Leem.Testify.Poco;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Task = System.Threading.Tasks.Task;

namespace Leem.Testify
{
    internal class CoverageMargin : Border, IWpfTextViewMargin
    {
        public const string MarginName = "CoverageMargin";

        // this is a pre-defined constant for code view
        // used to tell Visual Studio to specify the type of content the extension should be
        // associated with
        //public const string vsViewKindCode = "{7651A701-06E5-11D1-8EBD-00A0C90F26EA}";

        private const double Left = 1.0;
        private readonly CodeMarkManager _codeMarkManager;
        private readonly CoverageProvider _coverageProvider;
        private readonly string _documentName;
        private readonly DTE _dte;
        private readonly IWpfTextViewHost _textViewHost;
        private readonly Canvas _marginCanvas; // canvas object which is added to the margin to hold glyphs
        private List<CodeMark> _codeMarks;
        private bool _isDisposed;
        private const int _marginWidth = 18;


        public CoverageMargin(IWpfTextViewHost textViewHost, SVsServiceProvider serviceProvider,
            ICoverageProviderBroker coverageProviderBroker)
        {
            ITextDocument document;
            _textViewHost = textViewHost;

            _dte = (DTE) serviceProvider.GetService(typeof (DTE));

            _documentName = CoverageProvider.GetFileName(_textViewHost.TextView.TextBuffer);

            _codeMarkManager = new CodeMarkManager();

            _coverageProvider = coverageProviderBroker.GetCoverageProvider(_textViewHost.TextView, _dte, serviceProvider);

            _codeMarks = GetAllCodeMarksForMargin();

            _textViewHost.TextView.LayoutChanged += TextViewLayoutChanged;

            _textViewHost.TextView.GotAggregateFocus += TextViewGotAggregateFocus;

            _textViewHost.TextView.ViewportHeightChanged += TextViewViewportHeightChanged;

            _textViewHost.TextView.TextBuffer.Changed += TextBufferChanged;
            _textViewHost.TextView.Closed += TextViewClosed;

            //create a canvas to hold the margin UI and set its properties
            _marginCanvas = new Canvas();

            _marginCanvas.Background = Brushes.Transparent;

            ClipToBounds = true;
            Background = Brushes.Transparent;
            BorderBrush = Brushes.Transparent;

            Width = (_textViewHost.TextView.ZoomLevel / 100) * _marginWidth;

            BorderThickness = new Thickness(0.5);

            // add margin canvas to the children list
            Child = _marginCanvas;

            UpdateCodeMarks(_coverageProvider.GetCoveredLines(_textViewHost.TextView));

        }

        private void TextViewClosed(object sender, EventArgs e)
        {
            _coverageProvider.WasClosed = true;
        }

        public void Subscribe(TestifyQueries queries)
        {
            queries.ClassChanged += CoverageChanged;
        }

        private void CoverageChanged(object sender, ClassChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
        


        private List<CodeMark> GetAllCodeMarksForMargin()
        {
            ConcurrentDictionary<int, CoveredLinePoco> coveredLines =
                _coverageProvider.GetCoveredLines(_textViewHost.TextView);

            var allCodeMarks = new List<CodeMark>();

            foreach (var line in coveredLines)
            {
                allCodeMarks.Add(new CodeMark
                {
                    LineNumber = line.Value.LineNumber,
                    FileName = line.Value.FileName,
                    UnitTests = line.Value.UnitTests.Cast<UnitTest>().ToList()
                });
            }
            return allCodeMarks;
        }

        private void TextViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.VerticalTranslation || e.TranslatedLines.Any())
            {
                UpdateCodeMarks();
            }
        }

        private void TextViewGotAggregateFocus(object sender, EventArgs e)
        {
            _coverageProvider.RecreateCoverage((IWpfTextView) sender);

            UpdateCodeMarks();
        }

        private void TextViewViewportHeightChanged(object sender, EventArgs e)
        {
            UpdateCodeMarks();
        }

        private void TextBufferChanged(object sender, TextContentChangedEventArgs e)
        {

            if (e.Changes.IncludesLineChanges)
            {
                // Fire and Forget
              Task.Run(() => { RunTestsThatCoverCursor(); });
            }
        }

        private void RunTestsThatCoverCursor()
        {
            TextPoint textPoint = GetCursorTextPoint();

            CodeElement codeElement = GetMethodFromTextPoint(textPoint);

            ProjectItem projectItem = _dte.ActiveDocument.ProjectItem;

            BuildAndRunTests(textPoint, codeElement, projectItem);
        }

        private void BuildAndRunTests(TextPoint textPoint, CodeElement codeElement, ProjectItem projectItem)
        {
            if (projectItem != null && codeElement != null)
            {
                if (projectItem.ContainingProject.Name.Contains(".Test"))
                {
                    //This is a test project
                    var testQueue = new TestQueue
                    {
                        ProjectName = projectItem.ContainingProject.UniqueName,
                        IndividualTest = codeElement.FullName,
                        QueuedDateTime = DateTime.Now
                    };

                    _coverageProvider.Queries.AddToTestQueue(testQueue);

                    _dte.Solution.SolutionBuild.BuildProject("Debug", projectItem.ContainingProject.UniqueName, true);
                }
                else
                {
                  
                    _dte.Solution.SolutionBuild.BuildProject("Debug", projectItem.ContainingProject.FullName, true);

                    RunTestsThatCoverElement(textPoint, codeElement, projectItem);
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(MarginName);
        }

        private void UpdateCodeMarks()
        {
            // if we have any child in margin canvas then remove them
            if (_marginCanvas.Children.Count > 0)
            {
                _marginCanvas.Children.Clear();
            }

            if (_codeMarkManager != null)
            {
                UpdateCodeMarksAsync(_coverageProvider.GetCoveredLines(_textViewHost.TextView));
            }
        }

        private async Task UpdateCodeMarksAsync(ConcurrentDictionary<int, CoveredLinePoco> coveredLines)
        {
            UpdateCodeMarks(coveredLines);
        }

        private void UpdateCodeMarks(ConcurrentDictionary<int, CoveredLinePoco> coveredLines)
        {
            FileCodeModel fcm = _coverageProvider.GetFileCodeModel(_documentName);
            int apparentLineNumber = 0;

            foreach (ITextViewLine textViewLine in _textViewHost.TextView.TextViewLines.ToList())
            {
                if (textViewLine.VisibilityState == VisibilityState.FullyVisible && coveredLines.Count > 0)
                {
                    apparentLineNumber++;
                    int hj = textViewLine.Start.GetContainingLine().LineNumber;

                    var coveredLine = new CoveredLinePoco();

                    ITextSnapshotLine g =
                        _textViewHost.TextView.TextBuffer.CurrentSnapshot.Lines.FirstOrDefault(
                            x => x.LineNumber.Equals(hj));

                    bool isCovered = coveredLines.TryGetValue(hj + 1, out coveredLine);

                    if (g.Extent.IsEmpty == false && isCovered && g.Extent.GetText() != "\t\t#endregion")
                    {
                        Debug.WriteLine("Text for Line # " + (hj + 1) + " = " + g.Extent.GetText());

                        double yPos = (_textViewHost.TextView.ZoomLevel / 100) * (apparentLineNumber - 1) * _textViewHost.TextView.LineHeight + (.1 * _textViewHost.TextView.LineHeight); // GetYCoordinateForBookmark(coveredLine);

                        var glyph = CreateCodeMarkGlyph(coveredLine, yPos);

                        _marginCanvas.Children.Add(glyph);
                    }
                }
            }
            var pont = new SnapshotPoint(_textViewHost.TextView.TextSnapshot, 0);

        }

        private CodeMarkGlyph CreateCodeMarkGlyph(CoveredLinePoco line, double yPos)
        {
            // create a glyph
            var glyph = new CodeMarkGlyph(_textViewHost.TextView, line, yPos);

            // position it
            Canvas.SetTop(glyph, yPos);

            Canvas.SetLeft(glyph, 0);


            var tooltip = new StringBuilder();

            tooltip.AppendFormat("Covering Tests:\t {0}\n", line.UnitTests.Count);

            foreach (UnitTest test in line.UnitTests.OrderBy(x=>x.IsSuccessful))
            {
                tooltip.AppendFormat("{0}\n", test.TestMethodName);
            }

            glyph.ToolTip = tooltip.ToString();

            return glyph; // so we have the glyph now
        }


        // adjust y position for boundaries
        private double AdjustYCoordinateForBoundaries(double position)
        {
            double currentPosition = position; // current position

            if (currentPosition < CodeMarkManager.CodeMarkGlyphSize)
            {
                // set it to the top
                currentPosition -= CodeMarkManager.CodeMarkGlyphSize;
            }

            return currentPosition; // return the position
        }

        // get y position for this bookmark
        private double GetYCoordinateForBookmark(CoveredLinePoco line)
        {
            // calculate y position from line number with this bookmark
            return GetYCoordinateFromLineNumber(line.LineNumber);
        }


        // calculate y position from the line number
        private double GetYCoordinateFromLineNumber(int lineNumber)
        {
            int firstLineNumber = GetFirstVisibleLineNumber(_textViewHost.TextView);

            double lineHeight = _textViewHost.TextView.LineHeight;

            double yPosition = (lineNumber - firstLineNumber)*lineHeight;

            return Math.Max(yPosition, 0); // final position and return it
        }

        private int GetFirstVisibleLineNumber(IWpfTextView wpfTextView)
        {
            int firstLineNumber = wpfTextView.TextViewLines.FirstVisibleLine.Start.GetContainingLine().LineNumber + 1;

            return firstLineNumber;
        }

        private void RunTestsThatCoverElement(TextPoint textPoint, CodeElement codeElement, ProjectItem projectItem)
        {
            string projectName = projectItem.ContainingProject.UniqueName;

            string className = projectItem.Name;

            string methodName = codeElement.Name;

            int lineNumber = textPoint.Line;

            _coverageProvider.Queries.RunTestsThatCoverLine(projectName, className, methodName, lineNumber);
        }

        private CodeElement GetMethodFromTextPoint(TextPoint textPoint)
        {
            // Discover every code element containing the insertion point.
            string elems = "";
            const vsCMElement scopes = 0;
            foreach (vsCMElement scope in Enum.GetValues(scopes.GetType()))
            {
                CodeElement elem = textPoint.CodeElement[scope];
                if (elem != null)
                    elems += elem.Name +
                             " (" + scope + ")\n";
            }

            foreach (vsCMElement scope in Enum.GetValues(scopes.GetType()))
            {
                CodeElement elem = textPoint.CodeElement[vsCMElement.vsCMElementFunction];
                if (elem != null)
                {
                    return elem;
                }
            }

            return null;
        }


        private TextPoint GetCursorTextPoint()
        {
            TextPoint textPoint = null;
            try
            {
                var textSelection = _dte.ActiveDocument.Selection as TextSelection;
                textPoint = textSelection.ActivePoint;
            }
            catch (Exception ex)
            {
            }

            return textPoint;
        }


        private CodeElements GetCodeElementMembers(CodeElement objCodeElement)
        {
            CodeElements colCodeElements = default(CodeElements);


            if (objCodeElement is CodeNamespace)
            {
                colCodeElements = ((CodeNamespace) objCodeElement).Members;
            }
            else if (objCodeElement is CodeType)
            {
                colCodeElements = ((CodeType) objCodeElement).Members;
            }
            else if (objCodeElement is CodeFunction)
            {
                colCodeElements = ((CodeFunction) objCodeElement).Parameters;
            }

            return colCodeElements;
        }

        #region IWpfTextViewMargin Members

        /// <summary>
        ///     The <see cref="Sytem.Windows.FrameworkElement" /> that implements the visual representation
        ///     of the margin.
        /// </summary>
        public FrameworkElement VisualElement
        {
            // Since this margin implements Canvas, this is the object which renders
            // the margin.
            get
            {
                ThrowIfDisposed();
                return this;
            }
        }

        #endregion

        #region ITextViewMargin Members

        public double MarginSize
        {
            // Since this is a horizontal margin, its width will be bound to the width of the text view.
            // Therefore, its size is its height.
            get
            {
                ThrowIfDisposed();
                return ActualHeight;
            }
        }

        public bool Enabled
        {
            // The margin should always be enabled
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        /// <summary>
        ///     Returns an instance of the margin if this is the margin that has been requested.
        /// </summary>
        /// <param name="marginName">The name of the margin requested</param>
        /// <returns>An instance of EditorMargin4 or null</returns>
        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return (marginName == MarginName) ? this : null;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }

        #endregion
    }
}