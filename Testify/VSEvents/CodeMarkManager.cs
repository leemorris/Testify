using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE80;
using Leem.Testify;

namespace Leem.Testify
{
    public class CodeMarkManager
    {	
        // constant to identify a help bookmark from other bookmarks
        public const int HelpBookmarkNumber = 99;
        public const double BookmarkGlyphSize = 16.0; // size of the glyph

		// a list of all the bookmark numbers with associated bookmark objects
        private Dictionary<int, CodeMark> allCodeMarks = new Dictionary<int, CodeMark>();
        private DTE2 dte2; // an object of DTE2 type, used to navigate to bookmark location
		// this object is passed from the Package class

        private ICoverageService _coverageService;

        public Dictionary<int, CodeMark> CodeMarks
        {
            get
            {
                return allCodeMarks;
            }
            set
            {
                allCodeMarks = value;
				// bookmarks are changed so fire the BookmarksUpdated event
                OnUpdate(EventArgs.Empty);
            }
        }

        public CodeMarkManager()
        {
			// initialize the dictionary
            CodeMarks = new Dictionary<int, CodeMark>();

           
            //// add a bookmark for help
            //// we just pass the bookmark number and rest of the things are empty/null/zero values
            //AddBookmark(CoverageManager.HelpBookmarkNumber, string.Empty, 0, 0, null);
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
            if (CodeMarks.ContainsKey(position))
                return; // bookmark is already present, don't do anything

            // we are adding a bookmark for the first time
            // check if DTE2 is null, if it is then store the object of dte passed from the package
            // for later reference
            if (dte != null)
            {
                dte2 = dte;
            }
            // create a new bookmark from the information passed
            CodeMark codeMark = new CodeMark();
            // add the bookmark
            allCodeMarks.Add(position, codeMark);
            // fire the BookmarksUpdated event
            OnUpdate(EventArgs.Empty);
        }

        public void GotoUnitTest(int position)
        {
            // get the bookmark object out of the list
            CodeMark codeMark = CodeMarks[position];
            // get the project item object by using the file name and dte
            EnvDTE.ProjectItem document = dte2.Solution.FindProjectItem(codeMark.FileName);
            // activate the doucment (open if not already open)
            document.Open(BookmarkMargin.vsViewKindCode).Activate();
            // create a selection object
            EnvDTE.TextSelection selection = dte2.ActiveDocument.Selection;
            // move to the start of the document
            selection.StartOfDocument();
            // move to the location specified by the line number and column number stored in bookmark
            selection.MoveToLineAndOffset(codeMark.LineNumber, 0);
        }
    }
}
