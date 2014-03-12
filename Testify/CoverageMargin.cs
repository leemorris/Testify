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

namespace Leem.Testify
{
    class CoverageMargin : Border, IWpfTextViewMargin
    {
        public const string MarginName = "CoverageMargin";

        private IWpfTextViewHost _textViewHost;
        private bool _isDisposed = false;
        private const double Left = 1.0;

        // this is a pre-defined constant for code view
        // used to tell Visual Studio to specify the type of content the extension should be
        // associated with
        public const string vsViewKindCode = "{7651A701-06E5-11D1-8EBD-00A0C90F26EA}";

        Canvas marginCanvas; // canvas object which is added to the margin to hold glyphs
        private CodeMarkManager _codeMarkManager;
        private CoverageProvider _coverageProvider;
        private DTE _dte;
        private List<CodeMark> _codeMarks;
        private string _documentName;
        private UnitTestService _unitTestService;
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
            this.Width = 18;
            this.BorderThickness = new Thickness(0.5);

			// add margin canvas to the children list
            this.Child = marginCanvas;

            UpdateCodeMarks(_coverageProvider.GetCoveredLines(_textViewHost.TextView));
            _textViewHost.TextView.TextBuffer.Properties.TryGetProperty(typeof(Microsoft.VisualStudio.Text.ITextDocument), out _document);
            _unitTestService = new UnitTestService(_dte, _dte.Solution.FullName, System.IO.Path.GetFileNameWithoutExtension(_dte.Solution.FullName));
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
                                                FileName = line.Value.Module,
                                                UnitTests = line.Value.UnitTests.Cast<Poco.UnitTest>().ToList()
                });
            }
            return allCodeMarks;
        }

        void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
           // _textView = _textViewHost.TextView;
            // layout have changed so update all glyphs
            // todo update the glyphs
            List<int> linesEdited;
            if (e.VerticalTranslation)
            {
                UpdateCodeMarks();
            }
            
            //
        }
        void TextView_GotAggregateFocus (object sender, EventArgs e)
        {

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
                vsCMElement kind = vsCMElement.vsCMElementFunction;
                var textPoint = GetCursorTextPoint();

                var codeElement = GetMethodFromTextPoint(textPoint);
                var projectItem = _dte.ActiveDocument.ProjectItem;
                // Fire and Forget
                System.Threading.Tasks.Task.Run(() => RunTestsThatCoverMethod(textPoint, codeElement, projectItem));
                //RunTestsThatCoverMethod(textPoint, codeElement, projectItem);

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
            //await System.Threading.Tasks.Task.Run(() =>
            //{
                UpdateCodeMarks(coveredLines);
            //});
        }

        private void UpdateCodeMarks(ConcurrentDictionary<int, Poco.CoveredLinePoco> coveredLines)
        {
            UpdateCodeMarks2(coveredLines);
            //var fcm = _coverageProvider.GetFileCodeModel(_documentName);
            //foreach (var line in coveredLines)
            //{
            //    // calculate y postion for this particular bookmark
            //    double yPos = GetYCoordinateForBookmark(line.Value);

            //    yPos = AdjustYCoordinateForBoundaries(yPos);

            //    CodeMarkGlyph glyph;

            //    var g =_textViewHost.TextView.TextBuffer.CurrentSnapshot.Lines.FirstOrDefault(x=>x.LineNumber.Equals(line.Key - 1));
            //    var lineNumber = g.End.GetContainingLine().LineNumber;
            //    if (g.Extent.IsEmpty == false) 
            //    { 
             
            //        //line.Value.IsCode = false; 
            //        glyph = CreateCodeMarkGlyph(line.Value, yPos);
            //        marginCanvas.Children.Add(glyph);
            //    }
            //}

        }

        private void UpdateCodeMarks2(ConcurrentDictionary<int, Poco.CoveredLinePoco> coveredLines)
        {
            var fcm = _coverageProvider.GetFileCodeModel(_documentName);
            //var firstLineNumber = _textViewHost.TextView.TextViewLines.FirstVisibleLine.Start.GetContainingLine().LineNumber + 1;
            //var lastLineNumber = _textViewHost.TextView.TextViewLines.LastVisibleLine.End.GetContainingLine().LineNumber + 1;
            //var first = _textViewHost.TextView.TextViewLines.FirstOrDefault(x => x.VisibilityState == Microsoft.VisualStudio.Text.Formatting.VisibilityState.FullyVisible);
            //var ii = first.Start.GetContainingLine();

            //for (var i=firstLineNumber; i < lastLineNumber ; i++)
            foreach (var textViewLine in _textViewHost.TextView.TextViewLines.ToList())
            {
                //var textViewLine = _textViewHost.TextView.TextViewLines[i];
                if (textViewLine.VisibilityState == Microsoft.VisualStudio.Text.Formatting.VisibilityState.FullyVisible && coveredLines.Count > 0)
                {
                    var hj = textViewLine.Start.GetContainingLine().LineNumber;
                    // calculate y postion for this particular bookmark
                    var coveredLine = new Poco.CoveredLinePoco();
               

                    var g = _textViewHost.TextView.TextBuffer.CurrentSnapshot.Lines.FirstOrDefault(x => x.LineNumber.Equals(hj));
                    var lineNumber = g.End.GetContainingLine().LineNumber;
                    var isCovered = coveredLines.TryGetValue(hj + 1, out coveredLine);
                    if (g.Extent.IsEmpty == false && isCovered)
                    {
                        Debug.WriteLine("Text for Line # "+ (hj+1)+" = " + g.Extent.GetText());
                        
                        double yPos = GetYCoordinateForBookmark(coveredLine);

                        yPos = AdjustYCoordinateForBoundaries(yPos);

                        CodeMarkGlyph glyph;
                        //line.Value.IsCode = false; 
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

        // create a bookmark glyph for numbered bookmarks
        private CodeMarkGlyph CreateCodeMarkGlyph(Leem.Testify.Poco.CoveredLinePoco line, double yPos)
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
            else
            {
            }
            foreach (var test in line.UnitTests)
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
        private double GetYCoordinateForBookmark(Poco.CoveredLinePoco line)
        {
            // calculate y position from line number with this bookmark
            return GetYCoordinateFromLineNumber(line.LineNumber);
        }
            

        // calculate y position from the line number
        private double GetYCoordinateFromLineNumber(int lineNumber)
        {
            var firstLineNumber = GetFirstVisibleLineNumber(_textViewHost.TextView);
            var lineHeight = _textViewHost.TextView.LineHeight;

            var yPosition = (lineNumber - firstLineNumber ) * lineHeight;

            return Math.Max(yPosition,0); // final position and return it
        }

        private int GetFirstVisibleLineNumber(IWpfTextView wpfTextView)
        {
            var firstLineNumber = wpfTextView.TextViewLines.FirstVisibleLine.Start.GetContainingLine().LineNumber + 1;
            var lastLineNumber = wpfTextView.TextViewLines.LastVisibleLine.End.GetContainingLine().LineNumber + 1;
            var first = wpfTextView.TextViewLines.FirstOrDefault(x => x.VisibilityState == Microsoft.VisualStudio.Text.Formatting.VisibilityState.FullyVisible);
            return firstLineNumber;
        }
        private async System.Threading.Tasks.Task RunTestsThatCoverMethod(TextPoint textPoint, CodeElement codeElement, ProjectItem projectItem)
        {
            await _unitTestService.RunTestsThatCoverLine(projectItem.ContainingProject.UniqueName, projectItem.Name, codeElement.Name, textPoint.Line);
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

        private CodeElement GetCodeElementAtTextPoint(vsCMElement codeElementKind, CodeElements codeElements, TextPoint textPoint)
        {

            //EnvDTE.CodeElement objCodeElement = default(EnvDTE.CodeElement);
            EnvDTE.CodeElement resultCodeElement = default(EnvDTE.CodeElement);
            EnvDTE.CodeElements colCodeElementMembers = default(EnvDTE.CodeElements);
            EnvDTE.CodeElement memberCodeElement = default(EnvDTE.CodeElement);


            if (codeElements != null)
            {

                foreach (EnvDTE.CodeElement element in codeElements)
                {

                    if (element.StartPoint.GreaterThan(textPoint))
                    {
                        // The code element starts beyond the point
                    }
                    else if (element.EndPoint.LessThan(textPoint))
                    {
                        // The code element ends before the point
                    }
                    else
                    {
                        // The code element contains the point
                        if (element.Kind == codeElementKind)
                        {
                            // Found
                            resultCodeElement = element;
                        }

                        // We enter in recursion, just in case there is an inner code element that also 
                        // satisfies the conditions, for example, if we are searching a namespace or a class
                        colCodeElementMembers = GetCodeElementMembers(element);

                        memberCodeElement = GetCodeElementAtTextPoint(codeElementKind, colCodeElementMembers, textPoint);

                        if ((memberCodeElement != null))
                        {
                            // A nested code element also satisfies the conditions
                            resultCodeElement = memberCodeElement;
                        }

                        break; // TODO: might not be correct. Was : Exit For

                    }

                }

            }

            return resultCodeElement;

        }

        private EnvDTE.CodeElements GetCodeElementMembers(CodeElement objCodeElement)
        {

            EnvDTE.CodeElements colCodeElements = default(EnvDTE.CodeElements);


            if (objCodeElement is EnvDTE.CodeNamespace)
            {
                colCodeElements = ((EnvDTE.CodeNamespace)objCodeElement).Members;


            }
            else if (objCodeElement is EnvDTE.CodeType)
            {
                colCodeElements = ((EnvDTE.CodeType)objCodeElement).Members;


            }
            else if (objCodeElement is EnvDTE.CodeFunction)
            {
                colCodeElements = ((EnvDTE.CodeFunction)objCodeElement).Parameters;

            }

            return colCodeElements;

        }

    }
}
