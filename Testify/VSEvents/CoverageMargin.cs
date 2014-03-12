using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Leem.Testify;
using System.Linq;

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

        public CoverageMargin(IWpfTextViewHost textViewHost, SVsServiceProvider serviceProvider, ICoverageProviderBroker coverageProviderBroker)
        {
            _textViewHost = textViewHost;

            _dte = (DTE)serviceProvider.GetService(typeof(DTE));

            _codeMarkManager = new CodeMarkManager();

            _coverageProvider = coverageProviderBroker.GetCoverageProvider(_textViewHost.TextView, _dte, serviceProvider);
            _codeMarks = GetAllCodeMarksForMargin(); 
            // subscribe to LayoutChanged event of text view, so we can change the
            // positions of the glyphs when the layout changes
            _textViewHost.TextView.LayoutChanged += new EventHandler<TextViewLayoutChangedEventArgs>(TextView_LayoutChanged);
			// subscribe to the ViewportHeightChanged, o we can change the
			// positions of glyphs when the Viewport changes
            _textViewHost.TextView.ViewportHeightChanged += new EventHandler(TextView_ViewportHeightChanged);

             //create a canvas to hold the margin UI and set its properties
            marginCanvas = new Canvas();
            marginCanvas.Background = Brushes.Transparent;
            
            this.ClipToBounds = true;
            this.Background = new SolidColorBrush(Colors.LightGray);
            this.BorderBrush = new SolidColorBrush(Colors.DarkGray);
            this.Width = 18;
            this.BorderThickness = new Thickness(0.5);

			// add margin canvas to the children list
            this.Child = marginCanvas;

			// let's update bookmarks now, this actually checks if we have any bookmarks in the 
			// Bookmarks dictionary of the manager, and then creates glyphs for each one of them
            UpdateCodeMarks(_coverageProvider.GetCoveredLines(_textViewHost.TextView));

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
            UpdateCodeMarks();
        }

        void TextView_ViewportHeightChanged(object sender, EventArgs e)
        {
			// viewport height have changed so update all bookmarks

            this.UpdateCodeMarks();
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
                //marginCanvas.Children.Clear();
            }

            if (_codeMarkManager != null)
            {
                //foreach (CodeMark codeMark in _coverageManager.CodeMarks.Values)
                //{
					// create a bookmark glyph for each bookmark present in the bookmarks list
                UpdateCodeMarks(_coverageProvider.GetCoveredLines(_textViewHost.TextView));
                //}
            }
        }

        private void UpdateCodeMarks(ConcurrentDictionary<int, Poco.CoveredLine> coveredLines)
        {
            foreach (var line in coveredLines)
            {
                // calculate y postion for this particular bookmark
                double yPos = GetYCoordinateForBookmark(line.Value);
                //// modify the y position for boundaries, if the position goes less than zero
                //// or if it is more than the maximum value of the viewport
                yPos = AdjustYCoordinateForBoundaries(yPos);
                //// modify the y position for existing bookmarks
                //// if there is another bookmark on the calculated postition then
                //// find the next available slot in the margin
                yPos = AdjustYCoordinateForExistingBookmarks(yPos);
                CodeMarkGlyph glyph;
                // create a glyph depending on the bookmark type
               // if (line.Value.UnitTestId != BookmarkManager.HelpBookmarkNumber)
                //{
                    // this is a bookmark glyph
                //_textView.TextBuffer.CurrentSnapshot
                glyph = CreateCodeMarkGlyph(line.Value, yPos);
                //}
                //else
                //{
                    // this one is help glyph
                 //   glyph = CreateHelpGlyph(codeMark);
                //}
                // add it to the margin canvas
                marginCanvas.Children.Add(glyph);
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
        private CodeMarkGlyph CreateCodeMarkGlyph(Poco.CoveredLine line, double yPos)
        {
            // create a glyph
            CodeMarkGlyph glyph = new CodeMarkGlyph(line, _codeMarkManager);

            // position it
            Canvas.SetTop(glyph, line.LineNumber);
            Canvas.SetLeft(glyph, 0);

            // set tooltip with the information stored
            StringBuilder tooltip = new StringBuilder();
            tooltip.AppendFormat("Bookmark\t: {0}\n", line.UnitTestId);
          //  tooltip.AppendFormat("File name\t: {0}\n", line.FileName);
            tooltip.AppendFormat("Line number\t: {0}\n", line.LineNumber);
          //  tooltip.AppendFormat("Column number\t: {0}", line.ColumnNumber);

            glyph.ToolTip = tooltip.ToString();

            return glyph; // so we have the glyph now
        }

        // create a glyph for showing help
        private CodeMarkGlyph CreateHelpGlyph(CodeMark codeMark)
        {
            // create the glyph
            CodeMarkGlyph glyph = new CodeMarkGlyph(codeMark.UnitTests);

            // calculate the y position for this one
            // change is this will always placed around the mid point of the margin
            double yPos = GetYCoordinateFromLineNumber(_textViewHost.TextView.TextSnapshot.LineCount / 2);
            // adjust for other problems
            yPos = AdjustYCoordinateForBoundaries(yPos);
            yPos = AdjustYCoordinateForExistingBookmarks(yPos);

            // position it
            Canvas.SetTop(glyph, yPos);
            Canvas.SetLeft(glyph, Left);

            // set tooltip to the help
            StringBuilder tooltip = new StringBuilder();
            tooltip.AppendFormat("\tNumbered Bookmarks\n");
            tooltip.AppendFormat("Create bookmark\t: Ctrl+Alt+<Number>\n");
            tooltip.AppendFormat("Go to bookmark\t: Ctrl+Alt+<Number>\n");
            tooltip.AppendFormat("\t\t: Left click on bookmark\n");
            tooltip.AppendFormat("Delete bookmark\t: Right click on bookmark\n");
            tooltip.AppendFormat("Clear bookmarks\t: Ctrl+Alt+Backspace");

            glyph.ToolTip = tooltip.ToString();

            return glyph; // got the help glyph
        }

        // adjust y position for boundaries
        private double AdjustYCoordinateForBoundaries(double position)
        {
            double currentPosition = position; // current position
            double viewPortHeight = Math.Ceiling(_textViewHost.TextView.ViewportHeight); // viewport height
            // check the lower boundary (towards bottom of the window)
            if (currentPosition > (viewPortHeight - BookmarkManager.BookmarkGlyphSize))
            {
                // reduce viewport height by the size of glyph
                currentPosition = viewPortHeight - BookmarkManager.BookmarkGlyphSize;
            }
            // check the upper boundary (towards top of the window)
            else if (currentPosition < BookmarkManager.BookmarkGlyphSize)
            {
                // set it to the top
                currentPosition = 0.0;
            }
            else
            {
                // otherwise, place the mid point of the glyph at this y position
                currentPosition -= (BookmarkManager.BookmarkGlyphSize / 2);
            }

            return currentPosition; // return the position
        }

        // get y position for this bookmark
        private double GetYCoordinateForBookmark(Poco.CoveredLine line)
        {
            // calculate y position from line number with this bookmark
            return GetYCoordinateFromLineNumber(line.LineNumber);
        }

        // try to find next available slot on the margin
        // by default we will try to find next available slot towards bottom of the window
        // once we reach to the maximum then we will try to find a slot towards top of the margin
        // the second parameter is used to recurse the function towards bottom or top
        // initially we pass the multiplier is +1 and we keep updating the y position by the size of glyph
        // multiplied by the multiplier, once we have reached the bottom (maximum value) then we change the 
        // multiplier to -1 and this we we keep reducing the y position by the size of the glyph
        public double FindNextAvailableYCoordinate(double position, int multiplier)
        {
            double currentPosition = position;

            // compare with all elements in the canvas
            foreach (UIElement item in marginCanvas.Children)
            {
                // get the top position of this element
                double topOfThisElement = Canvas.GetTop(item);

                // check if this top position is clashing with the bookmark glyph's current position
                if (Math.Abs(currentPosition - topOfThisElement) < BookmarkManager.BookmarkGlyphSize)
                {
                    // see if multiplier is more than 0, means we are finding next available slot towards bottom
                    if (multiplier > 0)
                    {
                        // did we reached the maximum value of the viewport
                        if (currentPosition >= Math.Ceiling(_textViewHost.TextView.ViewportHeight - BookmarkManager.BookmarkGlyphSize))
                        {
                            // not yet, so keep finding value towards bottom, notice the multiplier is +ve
                            return FindNextAvailableYCoordinate(Math.Ceiling(_textViewHost.TextView.ViewportHeight - BookmarkManager.BookmarkGlyphSize), -1);
                        }
                        else
                        {
                            // yes, we reached the end, so let's find a value towards the top, change the mulitplier to -ve
                            return FindNextAvailableYCoordinate(currentPosition + BookmarkManager.BookmarkGlyphSize, 1);
                        }
                    }
                    else // multiplier is -ve, means we are finding next slot towards top
                    {
                        // check if we have reached the top of the window
                        if (currentPosition < BookmarkManager.BookmarkGlyphSize)
                        {
                            // yes, so let's try to find next slot towards bottom now, changed multiplier to +ve
                            return FindNextAvailableYCoordinate(0.0, 1);
                        }
                        else
                        {
                            // not yet, we are still looking for slot towards the bottom of the window
                            return FindNextAvailableYCoordinate(currentPosition - BookmarkManager.BookmarkGlyphSize, -1);
                        }
                    }
                }
            }

            return currentPosition; // finally we have the position
        }


        // adjust y position for existing bookmarks
        private double AdjustYCoordinateForExistingBookmarks(double position)
        {
            // try to find next availabe slot on the margin
            return FindNextAvailableYCoordinate(position, 1);
        }

        // calculate y position from the line number
        private double GetYCoordinateFromLineNumber(int lineNumber)
        {
            // get total number of lines
            int totalLines = _textViewHost.TextView.TextSnapshot.LineCount;
            // calculate the ration by line number divided by total number of lines
            double ratio = (double)lineNumber / (double)totalLines;
            // multiply the ration with the viewport height to get the corrosponding y positions
            double yPos = ratio * _textViewHost.TextView.ViewportHeight;
            return Math.Ceiling(yPos); // final position and return it
        }
    }
}
