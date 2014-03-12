using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;


namespace Leem.Testify
{
    public class UnCoverGlyphFactory : IGlyphFactory
    {
        const double m_glyphSize = 10.0;
        public UnCoverGlyphFactory()
        {
        }


        public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        {

            if (tag == null || !(tag is CoverTag))
            {
                return null;
            }

            System.Windows.Shapes.Ellipse ellipse = new Ellipse();
            ellipse.Height = m_glyphSize;
            ellipse.Width = m_glyphSize;
            ellipse.Fill = Brushes.Orange;
            return ellipse;
        }


    }
}
