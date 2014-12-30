using System.Collections.Generic;
using Leem.Testify.Poco;

namespace Leem.Testify
{
    public class CodeMark
    {
        public CodeMark()
        {
            FileName = string.Empty;
            UnitTests = new List<UnitTest>();
            LineNumber = 0;
        }

        //public CodeMark(string fileName, int lineNumber, List<UnitTest> tests)
        //{
        //    FileName = fileName;
        //    LineNumber = lineNumber;
        //    UnitTests = tests;
        //}

        public string FileName { get; set; } // name of the active document

        public int LineNumber { get; set; } // current line number (cursor position)

        public IList<UnitTest> UnitTests { get; set; }
    }
}