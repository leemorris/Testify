using System;
using System.Collections.Generic;

namespace Leem.Testify
{
    public class CodeMarkManager
    {
        // this object is passed from the Package class
        public delegate void CodeMarksEventHandler(object sender, EventArgs e);

        public const double CodeMarkGlyphSize = 16.0; // size of the glyph
        private Dictionary<int, CodeMark> _allCodeMarks = new Dictionary<int, CodeMark>();

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
                OnUpdate(EventArgs.Empty);
            }
        }

        // CodeMarks event
        public event CodeMarksEventHandler CodeMarksUpdated;

        protected virtual void OnUpdate(EventArgs args)
        {
            if (CodeMarksUpdated != null)
                CodeMarksUpdated(this, args); // fire the event
        }
    }
}