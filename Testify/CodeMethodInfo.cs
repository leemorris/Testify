using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify
{
    public class CodeMethodInfo
    {
        public String RawMethodName { get; set; }
        public String FileName { get; set; }
        public int Line  { get; set; }
        public int Column { get; set; }
    }
}
