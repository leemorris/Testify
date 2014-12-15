using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace Leem.Testify.UnitTestAdornment
{
    partial class ThemeResourceDictionary : ResourceDictionary
    {
        public ThemeResourceDictionary()
        {
           // InitializeComponent();
        }
        public ThemeResourceDictionary(List<ThemeResourceKey> themeResourceKeys):this()
        {
            
        }
    }
}
