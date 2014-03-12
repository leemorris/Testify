using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Leem.Testify
{
    public class CoverGlyphFactory : IGlyphFactory
    {
        const double m_glyphSize = 3.0;
        public CoverGlyphFactory( )
        {

        }

        public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        {
            if (tag == null || !(tag is CoverTag))
            {
                return null;
            }

            System.Windows.Shapes.Rectangle rectangle = new Rectangle();
            rectangle.Height = line.Height;
            rectangle.Width = m_glyphSize ;

            CoverTag coverTag = (CoverTag)tag;
            if (coverTag.Color == 1) 
            {
                rectangle.Fill = Brushes.Green;
            }
            else if (coverTag.Color == 2) 
            {
                rectangle.Fill = Brushes.Orange;
            }
            else if (coverTag.Color == 3)
            {
                rectangle.Fill = Brushes.Red;
            }
            return rectangle;
        }


    }
}
