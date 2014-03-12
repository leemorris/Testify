using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify
{
    public partial class TrackedMethod
    {
        // This property is not persisted to the database,
        // because it is regenerated on each build and therefore is not unique across builds.
        public int MetadataToken { get; set; }
    }
}
