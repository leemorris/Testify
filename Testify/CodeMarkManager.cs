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
            CodeMarks = new Dictionary<int, CodeMark>();
        }

        // delegate for CodeMarks event
        public delegate void CodeMarksEventHandler(object sender, EventArgs e);

        // CodeMarks event
        public event CodeMarksEventHandler CodeMarksUpdated;

        protected virtual void OnUpdate(EventArgs args)
        {
            if (CodeMarksUpdated != null)
                CodeMarksUpdated(this, args); // fire the event
        }



        public void GotoUnitTest(int position)
        {

            CodeMark codeMark = CodeMarks[position];

            // get the project item object by using the file name and dte
            EnvDTE.ProjectItem document = dte2.Solution.FindProjectItem(codeMark.FileName);

            // create a selection object
            EnvDTE.TextSelection selection = dte2.ActiveDocument.Selection as EnvDTE.TextSelection;

            // move to the start of the document
            selection.StartOfDocument();

            // move to the location specified by the line number and column number stored in bookmark
            selection.MoveToLineAndOffset(codeMark.LineNumber, 0);
        }
    }
}
