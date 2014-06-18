using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Leem.Testify;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager;
using Microsoft.VisualStudio.Text.Outlining;

namespace Leem.Testify
{
    class CoverageMargin : Border, IWpfTextViewMargin
    {
        public const string MarginName = "CoverageMargin";

        // this is a pre-defined constant for code view
        // used to tell Visual Studio to specify the type of content the extension should be
        // associated with
        public const string vsViewKindCode = "{7651A701-06E5-11D1-8EBD-00A0C90F26EA}";

        Canvas marginCanvas; // canvas object which is added to the margin to hold glyphs

        private IWpfTextViewHost _textViewHost;
        private bool _isDisposed = false;
        private const double Left = 1.0;
        private CodeMarkManager _codeMarkManager;
        private CoverageProvider _coverageProvider;
        private DTE _dte;
        private List<CodeMark> _codeMarks;
        private string _documentName;
        private ITextDocument _document;



        public CoverageMargin(IWpfTextViewHost textViewHost, SVsServiceProvider serviceProvider, ICoverageProviderBroker coverageProviderBroker)
        {
            _textViewHost = textViewHost;

            _dte = (DTE)serviceProvider.GetService(typeof(DTE));

            _documentName = CoverageProvider.GetFileName(_textViewHost.TextView.TextBuffer);

            _codeMarkManager = new CodeMarkManager();

            _coverageProvider = coverageProviderBroker.GetCoverageProvider(_textViewHost.TextView, _dte, serviceProvider);
      
            _codeMarks = GetAllCodeMarksForMargin(); 

            // subscribe to LayoutChanged event of text view, so we can change the
            // positions of the glyphs when the layout changes
            _textViewHost.TextView.LayoutChanged += new EventHandler<TextViewLayoutChangedEventArgs>(TextView_LayoutChanged);

            _textViewHost.TextView.GotAggregateFocus += new EventHandler(TextView_GotAggregateFocus);

			// subscribe to the ViewportHeightChanged, o we can change the);
			// positions of glyphs when the Viewport changes
            _textViewHost.TextView.ViewportHeightChanged += new EventHandler(TextView_ViewportHeightChanged);

            _textViewHost.TextView.TextBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(TextBuffer_Changed);

             //create a canvas to hold the margin UI and set its properties
            marginCanvas = new Canvas();

            marginCanvas.Background = Brushes.Transparent;
            
            this.ClipToBounds = true;
            this.Background = _textViewHost.TextView.Background;//. SolidColorBrush(Colors.LightGray);

            this.BorderBrush = _textViewHost.TextView.Background; //new SolidColorBrush(Colors.DarkGray);

            this.Width = 8;

            this.BorderThickness = new Thickness(0.5);

			// add margin canvas to the children list
            this.Child = marginCanvas;

            UpdateCodeMarks(_coverageProvider.GetCoveredLines(_textViewHost.TextView));

            _textViewHost.TextView.TextBuffer.Properties.TryGetProperty(typeof(Microsoft.VisualStudio.Text.ITextDocument), out _document);
        }

        public void Coverage_Changed(string className, string methodName)
        {
        }

        private List<CodeMark> GetAllCodeMarksForMargin()
        {
            var coveredLines = _coverageProvider.GetCoveredLines(_textViewHost.TextView);

            var allCodeMarks = new List<CodeMark>();

            foreach (var line in coveredLines)
            {

                allCodeMarks.Add(new CodeMark { LineNumber=line.Value.LineNumber,
                                                FileName = line.Value.Module.Name,
                                                UnitTests = line.Value.UnitTests.Cast<Poco.UnitTest>().ToList()
                });

            }
            return allCodeMarks;
        }

        void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            
            if (e.VerticalTranslation || e.TranslatedLines.Any())
            {

                UpdateCodeMarks();

            }

        }

        void TextView_GotAggregateFocus (object sender, EventArgs e)
        {

            _coverageProvider.RecreateCoverage((IWpfTextView)sender);

            this.UpdateCodeMarks();

        }

        void TextView_ViewportHeightChanged(object sender, EventArgs e)
        {

            this.UpdateCodeMarks();

        }

        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            List<int> linesEdited;

            if (e.Changes.IncludesLineChanges)
            {
                // Fire and Forget
                System.Threading.Tasks.Task.Run(() =>
                {
                    RunTestsThatCoverCursor();
                });

            }
        }

        private void RunTestsThatCoverCursor()
        {
            //Log
            vsCMElement kind = vsCMElement.vsCMElementFunction;
            var textPoint = GetCursorTextPoint();

            var codeElement = GetMethodFromTextPoint(textPoint);

            var projectItem = _dte.ActiveDocument.ProjectItem;

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
                    //This is a test project
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
            if (marginCanvas.Children.Count > 0)
            {

                marginCanvas.Children.Clear();

            }

            if (_codeMarkManager != null)
            {

                UpdateCodeMarksAsync(_coverageProvider.GetCoveredLines(_textViewHost.TextView));

            }
        }

        private async System.Threading.Tasks.Task UpdateCodeMarksAsync(ConcurrentDictionary<int, Poco.CoveredLinePoco> coveredLines) 
        {
            UpdateCodeMarks(coveredLines);
        }

        private void UpdateCodeMarks(ConcurrentDictionary<int, Poco.CoveredLinePoco> coveredLines)
        {

            var fcm = _coverageProvider.GetFileCodeModel(_documentName);
            int apparentLineNumber = 0;

            foreach (var textViewLine in _textViewHost.TextView.TextViewLines.ToList())
            {
///Todo calculate offset to account for lines above that are enclosed in a Region and not visible, 
////currently the Glyphs are offset down the screen by collapsed regions above


                if (textViewLine.VisibilityState == Microsoft.VisualStudio.Text.Formatting.VisibilityState.FullyVisible && coveredLines.Count > 0)
                {
                    apparentLineNumber++;
                    var hj = textViewLine.Start.GetContainingLine().LineNumber;

                    // calculate y postion for this particular bookmark
                    var coveredLine = new Poco.CoveredLinePoco();

                    var g = _textViewHost.TextView.TextBuffer.CurrentSnapshot.Lines.FirstOrDefault(x => x.LineNumber.Equals(hj));

                    var lineNumber = g.End.GetContainingLine().LineNumber;
                    var text = g.Extent.GetText();
                    var isCovered = coveredLines.TryGetValue(hj + 1, out coveredLine);

                    if (g.Extent.IsEmpty == false && isCovered && g.Extent.GetText() != "\t\t#endregion")
                    {
                        Debug.WriteLine("Text for Line # " + (hj + 1) + " = " + g.Extent.GetText());

                        double yPos = (apparentLineNumber - 1) * 16;// GetYCoordinateForBookmark(coveredLine);

                       // yPos = AdjustYCoordinateForBoundaries(yPos);

                        CodeMarkGlyph glyph;

                        glyph = CreateCodeMarkGlyph(coveredLine, yPos);

                        marginCanvas.Children.Add(glyph);
                    }
                }

            }

        }



        #region IWpfTextViewMargin Members

        /// <summary>
        /// The <see cref="Sytem.Windows.FrameworkElement"/> that implements the visual representation
        /// of the margin.
        /// </summary>
        public System.Windows.FrameworkElement VisualElement
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
                return this.ActualHeight;
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
        /// Returns an instance of the margin if this is the margin that has been requested.
        /// </summary>
        /// <param name="marginName">The name of the margin requested</param>
        /// <returns>An instance of EditorMargin4 or null</returns>
        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return (marginName == CoverageMargin.MarginName) ? (IWpfTextViewMargin)this : null;
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

      
        private CodeMarkGlyph CreateCodeMarkGlyph(Poco.CoveredLinePoco line, double yPos)
        {
            // create a glyph
            CodeMarkGlyph glyph = new CodeMarkGlyph(line);

            // position it
            Canvas.SetTop(glyph, yPos);

            Canvas.SetLeft(glyph, 0);

            // set tooltip with the information stored
            StringBuilder tooltip = new StringBuilder();

            tooltip.AppendFormat("Covering Tests:\t {0}\n", line.UnitTests.Count);

            var isBroken = !line.UnitTests.Any(x => x.IsSuccessful);

            if (isBroken)
            {
                foreach (var test in line.UnitTests.Where(x=>x.IsSuccessful.Equals(false)))
                {
                    tooltip.AppendFormat("{0}\n", System.IO.Path.GetFileName(test.TestMethodName));
                }
            }

            foreach (var test in line.UnitTests)
            {
                tooltip.AppendFormat("{0}\n", test.TestMethodName);
            }

            glyph.ToolTip = tooltip.ToString();

            return glyph; // so we have the glyph now
        }


        private int GetFirstVisibleLineNumber(IWpfTextView wpfTextView)
        {
            var firstLineNumber = wpfTextView.TextViewLines.FirstVisibleLine.Start.GetContainingLine().LineNumber + 1;

            var lastLineNumber = wpfTextView.TextViewLines.LastVisibleLine.End.GetContainingLine().LineNumber + 1;

            var first = wpfTextView.TextViewLines.FirstOrDefault(x => x.VisibilityState == Microsoft.VisualStudio.Text.Formatting.VisibilityState.FullyVisible);
            
            return firstLineNumber;
        }

        private void RunTestsThatCoverElement(TextPoint textPoint, CodeElement codeElement, ProjectItem projectItem)
        {
            var projectName = projectItem.ContainingProject.UniqueName;

            var className = projectItem.Name;

            var methodName = codeElement.Name;

            var lineNumber = textPoint.Line;

            _coverageProvider.Queries.RunTestsThatCoverLine(projectName, className, methodName, lineNumber);
        }

        private CodeElement GetMethodFromTextPoint(TextPoint textPoint)
        {
            // Discover every code element containing the insertion point.
            string elems = "";
            vsCMElement scopes = 0;
            foreach (vsCMElement scope in Enum.GetValues(scopes.GetType()))
            {
                CodeElement elem = textPoint.get_CodeElement(scope);
                if (elem != null)
                    elems += elem.Name +
                        " (" + scope.ToString() + ")\n";
            }

            foreach (vsCMElement scope in Enum.GetValues(scopes.GetType()))
            {
                CodeElement elem = textPoint.get_CodeElement(vsCMElement.vsCMElementFunction);
                if (elem != null)
                {
                    return elem;
                }

            }

            return null;
        }


        private EnvDTE.TextPoint GetCursorTextPoint()
        {
            TextPoint textPoint = null;
            try
            {
                var textSelection = _dte.ActiveDocument.Selection as TextSelection;
                textPoint = textSelection.ActivePoint;
            }
            catch (System.Exception ex)
            {
            }

            return textPoint;
        }

    }
}
