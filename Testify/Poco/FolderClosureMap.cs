using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify.Poco
{
    public class FolderClosureMap
    {
        public virtual ICollection<Folder> Folders { get; set; }
        public virtual ICollection<FolderClosure> FolderClosures { get; set; } 
    }
}
