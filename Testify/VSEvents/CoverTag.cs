using Microsoft.VisualStudio.Text.Editor;

namespace Leem.Testify.VSEvents
{
    public class CoverTag : IGlyphTag
    {
        public int Color { get; set; }
        public CoverTag(int color)
        {
            Color = color;
        }

    }
}
