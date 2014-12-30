using System;
using System.Text;

namespace Leem.Testify
{
    public class CodeMethodInfo
    {
        public string RawMethodName { get; set; }
        public String FileName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}