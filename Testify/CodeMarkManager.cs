using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE80;


namespace Leem.Testify
{
    public class CodeMarkManager
    {	

        private Dictionary<int, CodeMark> allCodeMarks = new Dictionary<int, CodeMark>();
        private DTE2 dte2; // an object of DTE2 type, used to navigate to unit test location
		// this object is passed from the Package class
        public const double CodeMarkGlyphSize = 16.0; // size of the glyph
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
        public delegate void CodeMarksEventHandler(object sender, EventArgs e);
		// BookmarksUpdated event
        public event CodeMarksEventHandler CodeMarksUpdated;

        protected virtual void OnUpdate(EventArgs args)
        {
            if (CodeMarksUpdated != null)
                CodeMarksUpdated(this, args); // fire the event
        }



        public void GotoUnitTest(int position)
        {
            // get the bookmark object out of the list
            CodeMark codeMark = CodeMarks[position];
            // get the project item object by using the file name and dte
            EnvDTE.ProjectItem document = dte2.Solution.FindProjectItem(codeMark.FileName);
            // activate the doucment (open if not already open)
          //  document.Open(BookmarkMargin.vsViewKindCode).Activate();
            // create a selection object
            EnvDTE.TextSelection selection = dte2.ActiveDocument.Selection as EnvDTE.TextSelection;
            // move to the start of the document
            selection.StartOfDocument();
            // move to the location specified by the line number and column number stored in bookmark
            selection.MoveToLineAndOffset(codeMark.LineNumber, 0);
        }
    }
}
