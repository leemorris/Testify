using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leem.Testify
{
    public class CoverageChangedEventArgs : EventArgs
    {
        public bool DisplaySequenceCoverage { get; set; }
    }

}
