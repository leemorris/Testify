using System.Collections.Generic;

namespace Leem.Testify
{
    public class CodeMark
    {
        public string FileName { get; set; } // name of the active document

        public int LineNumber { get; set; } // current line number (cursor position)

        public IList<Poco.UnitTest> UnitTests { get; set; }

        public CodeMark()
        {
            FileName = string.Empty;
            UnitTests = new List<Poco.UnitTest>();
            LineNumber = 0;
        }

        public CodeMark(string fileName, int lineNumber, List<Poco.UnitTest> tests)
        {
            FileName = fileName;
            LineNumber = lineNumber;
            UnitTests = tests;
        }
    }
}