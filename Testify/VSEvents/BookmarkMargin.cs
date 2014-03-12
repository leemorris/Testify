using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text.Editor;

namespace Leem.Testify
{
    class BookmarkMargin : Border, IWpfTextViewMargin
    {
		// this is a pre-defined constant for code view
		// used to tell Visual Studio to specify the type of content the extension should be
		// associated with
        public const string vsViewKindCode = "{7651A701-06E5-11D1-8EBD-00A0C90F26EA}";
		// a constant for the name of the margin
        public const string MarginName = "Bookmark Margin";
		// another constant to store the left position of all the bookmarks
		// this is used while placing the bookmark glyphs on the margin canvas
        private const double Left = 1.0;

        // The IWpfTextView that our margin will be attached to
        private IWpfTextView textView;

        // A flag stating whether this margin has been disposed
        private bool isDisposed = false;

        Canvas marginCanvas; // canvas object which is added to the margin to hold glyphs

		// an instance of the bookmark manager for holding all bookmarks associated with this margin
        private BookmarkManager bookmarkManager;

		// creates a margin for a given IWpfTextView
        public BookmarkMargin(IWpfTextView textViewParam, BookmarkManager bookmarkManagerParam)
        {
            // Set the IWpfTextView
            textView = textViewParam;

			// subscribe to LayoutChanged event of text view, this is necessary as we have to change the
			// positions of the bookmarks according to the change in layout
            textView.LayoutChanged += new EventHandler<TextViewLayoutChangedEventArgs>(TextView_LayoutChanged);

			// associated bookmark manager
            this.bookmarkManager = bookmarkManagerParam;

			// subscribe to BookmarksUpdated event of the manager
			// this will be fired whenever a new bookmark is added or removed
			// so that we can update the bookmark with the latest information
            this.bookmarkManager.BookmarksUpdated += new BookmarkManager.BookmarksUpdatedEventHandler(BookmarkManager_BookmarksUpdated);
			// subscribe to the ViewportHeightChanged, this is also necessary because we need to change the
			// positions of bookmarks as per change in Viewport
            textView.ViewportHeightChanged += new EventHandler(TextView_ViewportHeightChanged);

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
            UpdateBookmarks();
        }

        void BookmarkManager_BookmarksUpdated(object sender, EventArgs args)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new DispatcherOperationCallback
            (
				// an anonymous delegate to call UpdateBookmarks
				// this is called whenever there is a change in bookmarks list
                delegate
                {
                    this.UpdateBookmarks();
                    return null;
                }
            ), null);
        }

        private void UpdateBookmarks()
        {
			// if we have any child in margin canvas then remove them
            if (marginCanvas.Children.Count > 0)
            {
                marginCanvas.Children.Clear();
            }

            if (bookmarkManager != null)
            {
                foreach (Bookmark bookmark in bookmarkManager.Bookmarks.Values)
                {
					// create a bookmark glyph for each bookmark present in the bookmarks list
                    UpdateBookmark(bookmark);
                }
            }
        }

        private void UpdateBookmark(Bookmark bookmark)
        {
			// calculate y postion for this particular bookmark
            double yPos = GetYCoordinateForBookmark(bookmark);
			// modify the y position for boundaries, if the position goes less than zero
			// or if it is more than the maximum value of the viewport
            yPos = AdjustYCoordinateForBoundaries(yPos);
			// modify the y position for existing bookmarks
			// if there is another bookmark on the calculated postition then
			// find the next available slot in the margin
            yPos = AdjustYCoordinateForExistingBookmarks(yPos);
            CodeMarkGlyph glyph;
			// create a glyph depending on the bookmark type
            if (bookmark.Number != BookmarkManager.HelpBookmarkNumber)
            {
				// this is a bookmark glyph
               // glyph = CreateBookmarkGlyph(bookmark, yPos);
            }
            else
            {
				// this one is help glyph
                //glyph = CreateHelpGlyph(bookmark);
            }
			// add it to the margin canvas
           // marginCanvas.Children.Add(glyph);
        }

		// get y position for this bookmark
        private double GetYCoordinateForBookmark(Bookmark bookmark)
        {
			// calculate y position from line number with this bookmark
            return GetYCoordinateFromLineNumber(bookmark.LineNumber);
        }

		// adjust y position for boundaries
        private double AdjustYCoordinateForBoundaries(double position)
        {
            double currentPosition = position; // current position
            double viewPortHeight = Math.Ceiling(textView.ViewportHeight); // viewport height
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

		// adjust y position for existing bookmarks
        private double AdjustYCoordinateForExistingBookmarks(double position)
        {
			// try to find next availabe slot on the margin
            return FindNextAvailableYCoordinate(position, 1);
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
                        if (currentPosition >= Math.Ceiling(textView.ViewportHeight - BookmarkManager.BookmarkGlyphSize))
                        {
							// not yet, so keep finding value towards bottom, notice the multiplier is +ve
                            return FindNextAvailableYCoordinate(Math.Ceiling(textView.ViewportHeight - BookmarkManager.BookmarkGlyphSize), -1);
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

		// create a bookmark glyph for numbered bookmarks
        private CodeMarkGlyph CreateBookmarkGlyph(Bookmark bookmark, double yPos)
        {
          //  // create a glyph
          ////  CodeMarkGlyph glyph = new CodeMarkGlyph(bookmark.Number, bookmarkManager);

          //  // position it
          //  Canvas.SetTop(glyph, yPos);
          //  Canvas.SetLeft(glyph, Left);

          //  // set tooltip with the information stored
          //  StringBuilder tooltip = new StringBuilder();
          //  tooltip.AppendFormat("Bookmark\t: {0}\n", bookmark.Number);
          //  tooltip.AppendFormat("File name\t: {0}\n", bookmark.FileName);
          //  tooltip.AppendFormat("Line number\t: {0}\n", bookmark.LineNumber);
          //  tooltip.AppendFormat("Column number\t: {0}", bookmark.ColumnNumber);

          //  glyph.ToolTip = tooltip.ToString();

          //  return glyph; // so we have the glyph now
            return new CodeMarkGlyph();
        }

        //// create a glyph for showing help
        //private CodeMarkGlyph CreateHelpGlyph(Bookmark bookmark)
        //{
        //    // create the glyph
        //    CodeMarkGlyph glyph = new CodeMarkGlyph(bookmark.Number);

        //    // calculate the y position for this one
        //    // change is this will always placed around the mid point of the margin
        //    double yPos = GetYCoordinateFromLineNumber(this.textView.TextSnapshot.LineCount / 2);
        //    // adjust for other problems
        //    yPos = AdjustYCoordinateForBoundaries(yPos);
        //    yPos = AdjustYCoordinateForExistingBookmarks(yPos);

        //    // position it
        //    Canvas.SetTop(glyph, yPos);
        //    Canvas.SetLeft(glyph, Left);

        //    // set tooltip to the help
        //    StringBuilder tooltip = new StringBuilder();
        //    tooltip.AppendFormat("\tNumbered Bookmarks\n");
        //    tooltip.AppendFormat("Create bookmark\t: Ctrl+Alt+<Number>\n");
        //    tooltip.AppendFormat("Go to bookmark\t: Ctrl+Alt+<Number>\n");
        //    tooltip.AppendFormat("\t\t: Left click on bookmark\n");
        //    tooltip.AppendFormat("Delete bookmark\t: Right click on bookmark\n");
        //    tooltip.AppendFormat("Clear bookmarks\t: Ctrl+Alt+Backspace");

        //    glyph.ToolTip = tooltip.ToString();

        //    return glyph; // got the help glyph
        //}

        void TextView_ViewportHeightChanged(object sender, EventArgs e)
        {
			// viewport height have changed so update all bookmarks
            this.UpdateBookmarks();
        }

        void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
			// layout have changed so update all bookmarks
            UpdateBookmarks();
        }

		// calculate y position from the line number
        private double GetYCoordinateFromLineNumber(int lineNumber)
        {
			// get total number of lines
            int totalLines = this.textView.TextSnapshot.LineCount;
			// calculate the ration by line number divided by total number of lines
            double ratio = (double)lineNumber / (double)totalLines;
			// multiply the ration with the viewport height to get the corrosponding y positions
            double yPos = ratio * textView.ViewportHeight;
            return Math.Ceiling(yPos); // final position and return it
        }

		// members of IWpfTextViewMargin
        public FrameworkElement VisualElement
        {
            get
            {
				// if the margin is not disposed then return this margin object 
				// otherwise throw an exception
                ThrowExceptionIfAlreadyDisposed();
                return this;
            }
        }

        public bool Enabled
        {
            get
            {
				// if the margin is not disposed then return this enabled property 
				// otherwise throw an exception
                ThrowExceptionIfAlreadyDisposed();
                return true;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
			// return the bookmark margin name if VS is looking for this margin
            return (marginName == BookmarkMargin.MarginName) ? (IWpfTextViewMargin) this : null;
        }

        public double MarginSize
        {
            get
            {
				// if the margin is not disposed then return this size of the margin 
				// otherwise throw an exception
                ThrowExceptionIfAlreadyDisposed();
                return this.ActualWidth;
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
				// finalize this margin
                GC.SuppressFinalize(this);
                isDisposed = true;
            }
        }

        private void ThrowExceptionIfAlreadyDisposed()
        {
			// throw an exception if it is disposed
            if (isDisposed)
                throw new ObjectDisposedException(BookmarkMargin.MarginName);
        }
    }
}
