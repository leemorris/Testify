using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE80;

namespace Leem.Testify
{
    public class BookmarkManager
    {
		// constant to identify a help bookmark from other bookmarks
        public const int HelpBookmarkNumber = 99;
        public const double BookmarkGlyphSize = 16.0; // size of the glyph

		// a list of all the bookmark numbers with associated bookmark objects
        private Dictionary<int, Bookmark> allBookmarks = new Dictionary<int, Bookmark>();
        private DTE2 dte2; // an object of DTE2 type, used to navigate to bookmark location
		// this object is passed from the Package class

        public Dictionary<int, Bookmark> Bookmarks
        {
            get
            {
                return allBookmarks;
            }
            set
            {
                allBookmarks = value;
				// bookmarks are changed so fire the BookmarksUpdated event
                OnUpdate(EventArgs.Empty);
            }
        }

        public BookmarkManager()
        {
			// initialize the dictionary
            Bookmarks = new Dictionary<int, Bookmark>();
			// add a bookmark for help
			// we just pass the bookmark number and rest of the things are empty/null/zero values
            AddBookmark(BookmarkManager.HelpBookmarkNumber, string.Empty, 0, 0, null);
        }

		// delagate for BookmarksUpdated event
        public delegate void BookmarksUpdatedEventHandler(object sender, EventArgs e);
		// BookmarksUpdated event
        public event BookmarksUpdatedEventHandler BookmarksUpdated;

        protected virtual void OnUpdate(EventArgs args)
        {
            if (BookmarksUpdated != null)
                BookmarksUpdated(this, args); // fire the event
        }

        public void AddBookmark(int position, string fileName, int lineNumber, int columnNumber, DTE2 dte)
        {
            if (Bookmarks.ContainsKey(position))
                return; // bookmark is already present, don't do anything

			// we are adding a bookmark for the first time
			// check if DTE2 is null, if it is then store the object of dte passed from the package
			// for later reference
            if (dte != null)
            {
                dte2 = dte;
            }
			// create a new bookmark from the information passed
            Bookmark bookmark = new Bookmark(fileName, lineNumber, columnNumber, position);
			// add the bookmark
            allBookmarks.Add(position, bookmark);
			// fire the BookmarksUpdated event
            OnUpdate(EventArgs.Empty);
        }

        public void ClearAllBookmarks()
        {
			// we don't have any bookmarks yet, so don't do anything
            if (allBookmarks.Count == 0)
                return;

			// get the help bookmark object, so that we can remove all bookmarks first
			// and then can add it back
            Bookmark helpBookmark = Bookmarks[BookmarkManager.HelpBookmarkNumber];
            // remvoe all bookmarks
            allBookmarks.Clear();
            // add help bookmark again
            Bookmarks.Add(BookmarkManager.HelpBookmarkNumber, helpBookmark);
            OnUpdate(EventArgs.Empty); // fire the BookmarksUpdated event
        }

        public override string ToString()
        {
			// override the ToString, can be used as a utility to check the contents of the manager
            StringBuilder result = new StringBuilder();
            foreach (KeyValuePair<int, Bookmark> item in Bookmarks)
            {
                result.AppendFormat("Key: {0} Bookmark: [FileName: {1}, LineNumber: {2}]\n", item.Key, item.Value.FileName, item.Value.LineNumber);
            }
            return result.ToString();
        }

        public void RemoveBookmark(int position)
        {
			// if the bookmark is not present then don't do anything
            if (!Bookmarks.ContainsKey(position))
            {
                return;
            }

			// remove the designated bookmark
            Bookmarks.Remove(position);
            OnUpdate(EventArgs.Empty); // fire the BookmarksUpdated event
        }

        public void GotoBookmark(int position)
        {
			// get the bookmark object out of the list
            Bookmark bookmark = Bookmarks[position];
			// get the project item object by using the file name and dte
            EnvDTE.ProjectItem document = dte2.Solution.FindProjectItem(bookmark.FileName);
			// activate the doucment (open if not already open)
            document.Open(BookmarkMargin.vsViewKindCode).Activate();
			// create a selection object
            EnvDTE.TextSelection selection = dte2.ActiveDocument.Selection;
			// move to the start of the document
            selection.StartOfDocument();
			// move to the location specified by the line number and column number stored in bookmark
            selection.MoveToLineAndOffset(bookmark.LineNumber, bookmark.ColumnNumber);
        }
    }
}
