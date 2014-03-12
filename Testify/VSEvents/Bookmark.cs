
namespace Leem.Testify
{
	// class for storing values associated with a bookmark
    public class Bookmark
    {
        public int Number { get; set; } // bookmark number
        public string FileName { get; set; } // name of the active document
        public int LineNumber { get; set; } // current line number (cursor position)
        public int ColumnNumber { get; set; } // current column number (cursor position)

        public Bookmark()
        {
			// assign default values
            Number = -1;
            FileName = string.Empty;
            LineNumber = 0;
            ColumnNumber = 0;
        }

        public Bookmark(string fileName, int lineNumber, int column, int number)
        {
            Number = number;
            FileName = fileName;
            LineNumber = lineNumber;
            ColumnNumber = column;
        }
    }
}
