using System;
using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;

namespace Leem.Testify
{
    public class CodeMarkManager
    {
        // this object is passed from the Package class
        public delegate void CodeMarksEventHandler(object sender, EventArgs e);

        public const double CodeMarkGlyphSize = 16.0; // size of the glyph
        //private ICoverageService _coverageService;
        private Dictionary<int, CodeMark> _allCodeMarks = new Dictionary<int, CodeMark>();
        //private DTE2 dte2; // an object of DTE2 type, used to navigate to unit test location

        public CodeMarkManager()
        {
            CodeMarks = new Dictionary<int, CodeMark>();
        }

        private Dictionary<int, CodeMark> CodeMarks
        {
            get { return _allCodeMarks; }
            set
            {
                _allCodeMarks = value;
                // bookmarks are changed so fire the BookmarksUpdated event
                OnUpdate(EventArgs.Empty);
            }
        }

        // delegate for CodeMarks event

        // CodeMarks event
        public event CodeMarksEventHandler CodeMarksUpdated;

        protected virtual void OnUpdate(EventArgs args)
        {
            if (CodeMarksUpdated != null)
                CodeMarksUpdated(this, args); // fire the event
        }


        //public void GotoUnitTest(int position)
        //{
        //    CodeMark codeMark = CodeMarks[position];

        //    // get the project item object by using the file name and dte
        //    ProjectItem document = dte2.Solution.FindProjectItem(codeMark.FileName);

        //    // create a selection object
        //    var selection = dte2.ActiveDocument.Selection as TextSelection;

        //    // move to the start of the document
        //    selection.StartOfDocument();

        //    // move to the location specified by the line number and column number stored in bookmark
        //    selection.MoveToLineAndOffset(codeMark.LineNumber, 0);
        //}
    }
}