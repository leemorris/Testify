using EntityFramework.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Leem.Testify.Poco
{
    public class FolderClosure : ITriggerable 
    {

        public int FolderClosureId { get; set; }
        public int AncestorId { get; set; }
        public int DescendantId { get; set; }

        public int Depth { get; set; }

      
       


    }
}
