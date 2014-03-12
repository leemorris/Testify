using System.Collections.Generic;


namespace Leem.Testify
{
	// class for storing values associated with a bookmark
    public class CodeMark
    {
        //public int UnitTestId { get; set; }
        //public string TestName { get; set; } // bookmark number
        public string FileName { get; set; } // name of the active document
        public int LineNumber { get; set; } // current line number (cursor position)
        public IList<UnitTest> UnitTests { get; set; } // current line number (cursor position)
        //public int ColumnNumber { get; set; } // current column number (cursor position)

        public CodeMark()
        {
			// assign default values
            //UnitTestId = 0;
            FileName = string.Empty;
            UnitTests = new List<UnitTest>();
            FileName = string.Empty;
            LineNumber = 0;
            //ColumnNumber = 0;
        }

        public CodeMark(string fileName, int lineNumber, List<UnitTest> tests)
        {
            //UnitTestId = number;
            FileName = fileName;
            LineNumber = lineNumber;
            UnitTests = tests;
        }
    }
}
