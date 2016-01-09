using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify.Poco
{
    public class FolderPaths
    {
        public Folder Ancestor { get; set; }
        public Folder Descendant { get; set; }
        public int Depth { get; set; }
    }
}
