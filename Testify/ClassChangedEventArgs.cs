using System.Collections.Generic;
using System;

namespace Leem.Testify
{
    public class ClassChangedEventArgs: EventArgs
    {
        public List<string> ChangedClasses { get; set; }
    }
}