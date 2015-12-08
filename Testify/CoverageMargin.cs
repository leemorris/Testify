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
using log4net;
using System.Threading.Tasks;

namespace Leem.Testify
{
    internal class CoverageMargin : Border, IWpfTextViewMargin
    {
        public const string MarginName = "CoverageMargin";
        private readonly ILog _log = LogManager.GetLogger(typeof(CoverageMargin));
        // this is a pre-defined constant for code view
        // used to tell Visual Studio to specify the type of content the extension should be
        // associated with
        //public const string vsViewKindCode = "{7651A701-06E5-11D1-8EBD-00A0C90F26EA}";

        private const double Left = 1.0;
        private CodeMarkManager _codeMarkManager;
        private CoverageProvider _coverageProvider;
        private string _documentName;
        private readonly DTE _dte;
        private readonly IWpfTextViewHost _textViewHost;
        private Canvas _marginCanvas; // canvas object which is added to the margin to hold glyphs
        private List<CodeMark> _codeMarks;
        private bool _isDisposed;
        private const int _marginWidth = 18;
        private TestifyContext _context;


        public CoverageMargin(IWpfTextViewHost textViewHost, SVsServiceProvider serviceProvider,
            ICoverageProviderBroker coverageProviderBroker)
        {
           
            ITextDocument document;
            _textViewHost = textViewHost;

            //create a canvas to hold the margin UI and set its properties
            _marginCanvas = new Canvas();

            _dte = (DTE) serviceProvider.GetService(typeof (DTE));
            Task.Run(() => CreateCoverageMargin(serviceProvider, coverageProviderBroker));


        }

        private void CreateCoverageMargin(SVsServiceProvider serviceProvider, ICoverageProviderBroker coverageProviderBroker)
        {

            _documentName = CoverageProvider.GetFileName(_textViewHost.TextView.TextBuffer);

            _codeMarkManager = new CodeMarkManager();

            _coverageProvider = coverageProviderBroker.GetCoverageProvider(_textViewHost.TextView, _dte, serviceProvider, _context);

            _context = new TestifyContext(_coverageProvider.SolutionName.Replace(".sln",string.Empty));
            _coverageProvider.Context = _context;
            _textViewHost.TextView.LayoutChanged += TextViewLayoutChanged;

            _textViewHost.TextView.GotAggregateFocus += TextViewGotAggregateFocus;

            _textViewHost.TextView.ViewportHeightChanged += TextViewViewportHeightChanged;

            _textViewHost.TextView.TextBuffer.Changed += TextBufferChanged;
            _textViewHost.TextView.Closed += TextViewClosed;

            this.Dispatcher.Invoke((Action)(() =>
            {
                _marginCanvas.Background = Brushes.Transparent;

                ClipToBounds = true;
                Background = Brushes.Transparent;
                BorderBrush = Brushes.Transparent;

                Width = (_textViewHost.TextView.ZoomLevel / 100) * _marginWidth;

                BorderThickness = new Thickness(0.5);

                // add margin canvas to the children list
                Child = _marginCanvas;

                _codeMarks = GetAllCodeMarksForMargin(_context);

                UpdateCodeMarks(_coverageProvider.GetCoveredLines(_textViewHost.TextView, _context));

                

            }));

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
        


        private List<CodeMark> GetAllCodeMarksForMargin(TestifyContext context)
        {
            Dictionary<int, CoveredLine> coveredLines =
                _coverageProvider.GetCoveredLines(_textViewHost.TextView, context);

            var allCodeMarks = new List<CodeMark>();

            foreach (var line in coveredLines)
            {
                allCodeMarks.Add(new CodeMark
                {
                    LineNumber = line.Value.LineNumber,
                    FileName = line.Value.FileName//,
                    //TestMethods = line.Value.TestMethods.ToList()
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
            _log.DebugFormat("TextViewGotAggregateFocus - FIRED");
            _coverageProvider.RecreateCoverage((IWpfTextView) sender,_context);

            UpdateCodeMarks();
        }

        private void TextViewViewportHeightChanged(object sender, EventArgs e)
        {
            UpdateCodeMarks();
        }

        private void TextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // Only add to Queue when the File is SAVED, not when the 
           // TextBuffer changes. Otherwise, I have to SAVE the file and I'm not sure this is the right thing to do.
            // If the TextBuffer isn't saved, the Build and Test won't actually test the changes the user just made.

            //if (e.Changes.IncludesLineChanges)
            //{
            //    // Fire and Forget
            //    _log.DebugFormat("TextBufferChanged - Includes Line Changes");
            //  Task.Run(() => { RunTestsThatCoverCursor(); });
            //  _log.DebugFormat("TextBufferChanged - Continuing");
            //}
        }

        private void RunTestsThatCoverCursor()
        {
            TextPoint textPoint = GetCursorTextPoint();

            CodeElement codeElement = GetMethodFromTextPoint(textPoint);
            
            ProjectItem projectItem = _dte.ActiveDocument.ProjectItem;

            BuildAndRunTests(textPoint, codeElement, projectItem);
        }

        private async Task BuildAndRunTests(TextPoint textPoint, CodeElement codeElement, ProjectItem projectItem)
        {
            _log.DebugFormat("BuildAndRunTests - <{0}>", projectItem.ContainingProject.Name);
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
                    RunTestsThatCoverElement( textPoint, codeElement, projectItem);
                }
                else
                {
                    _log.DebugFormat("RunTestsThatCoverElement, Project - {0} Code Element {1}", projectItem.ContainingProject.FullName, codeElement.FullName);
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
                UpdateCodeMarksAsync(_coverageProvider.GetCoveredLines(_textViewHost.TextView, _context));
            }
        }

        private async Task UpdateCodeMarksAsync(Dictionary<int, CoveredLine> coveredLines)
        {
            UpdateCodeMarks(coveredLines);
        }

        private void UpdateCodeMarks(Dictionary<int, CoveredLine> coveredLines)
        {
            var sw = Stopwatch.StartNew();

            FileCodeModel fcm = _coverageProvider.GetFileCodeModel(_documentName);
            int apparentLineNumber = 0;
            double accumulatedHeight = 0.0;
            double minLineHeight = _textViewHost.TextView.TextViewLines.Min(x => x.Height);

            foreach (ITextViewLine textViewLine in _textViewHost.TextView.TextViewLines.ToList())
            {
                if (textViewLine.VisibilityState == VisibilityState.FullyVisible && coveredLines.Count > 0)
                {
                    apparentLineNumber++;
                    int lineNumber = textViewLine.Start.GetContainingLine().LineNumber;

                    accumulatedHeight += textViewLine.Height;
                    var coveredLine = new CoveredLine();

                    ITextSnapshotLine g =
                        _textViewHost.TextView.TextBuffer.CurrentSnapshot.Lines.FirstOrDefault(
                            x => x.LineNumber.Equals(lineNumber));

                    bool isCovered = coveredLines.TryGetValue(lineNumber + 1, out coveredLine);
                    //if (coveredLine != null)
                    //{
                    //    var isSuccessfulFromDatabase = coveredLine.IsSuccessful;
                    //    //coveredLine.IsCovered = coveredLine.TestMethods.Any();
                    //    var isSuccessful = coveredLine.IsCovered && coveredLine.TestMethods.All(x => x.IsSuccessful);
                    //    coveredLine.IsSuccessful = isSuccessful;
                       
                    //}

                    var text = g.Extent.GetText();

                    if (text.Trim().StartsWith("[Test")) 
                    {
                        //apparentLineNumber--;
                    }
                    if (g.Extent.IsEmpty == false && isCovered && text != "\t\t#endregion" )
                    {

                        Debug.WriteLine("Text for Line # " + (lineNumber + 1) + " = " + text);

                        //double yPos = (_textViewHost.TextView.ZoomLevel / 100) * (apparentLineNumber - 1) * _textViewHost.TextView.LineHeight + (.1 * _textViewHost.TextView.LineHeight); // GetYCoordinateForBookmark(coveredLine);
                        double yPos = (_textViewHost.TextView.ZoomLevel / 100) * ((accumulatedHeight - minLineHeight) + (.1 * _textViewHost.TextView.LineHeight)); // GetYCoordinateForBookmark(coveredLine);

                        if (coveredLine.Class.CodeModule.AssemblyName.EndsWith(".Test") || text.Contains("=>"))
                        {
                            // remove the branch icon from Unit Tests, the icon doesn't make sense
                            // lines containing Linq expressions are marked as Branches, lets remove that
                            coveredLine.IsBranch = false;
                        }
                            
                        var glyph = CreateCodeMarkGlyph(coveredLine, yPos, _context);

                        _marginCanvas.Children.Add(glyph);
                    }
                }
            }
            var pont = new SnapshotPoint(_textViewHost.TextView.TextSnapshot, 0);
            _log.DebugFormat("UpdateCodeMarks = {0} ms", sw.ElapsedMilliseconds);
        }

        private CodeMarkGlyph CreateCodeMarkGlyph(CoveredLine line, double yPos, TestifyContext context)
        {
            // create a glyph
            var glyph = new CodeMarkGlyph(_textViewHost.TextView, line, yPos, context);

            // position it
            Canvas.SetTop(glyph, yPos);

            Canvas.SetLeft(glyph, 0);

            if (line.TestMethods.Any())
            {
                var tooltip = new StringBuilder();
                tooltip.AppendFormat("Click to see Tests \nCovering Tests:\t {0}", line.TestMethods.Count);
                glyph.ToolTip = tooltip.ToString();
            }
    
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
        private double GetYCoordinateForBookmark(CoveredLine line)
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