using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Windows;

namespace Leem.Testify.UnitTestAdornment
{
    partial class ThemeResourceDictionary : ResourceDictionary
    {
        public ThemeResourceDictionary()
        {
            // InitializeComponent();
        }

        public ThemeResourceDictionary(List<ThemeResourceKey> themeResourceKeys)
            : this()
        {
        }
    }
}