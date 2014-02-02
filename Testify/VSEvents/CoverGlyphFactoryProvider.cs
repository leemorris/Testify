using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Leem.Testify.VSEvents
{
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("CoverGlyph")]
    [Order(After = "VsTextMarker")]
    [ContentType("code")]
    [TagType(typeof(CoverTag))]
    internal sealed class CoverGlyphFactoryProvider : IGlyphFactoryProvider
    {
        public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
        {
            return new CoverGlyphFactory();
        }

     
    }

}
