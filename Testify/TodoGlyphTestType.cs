using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Leem.Testify.VSEvents
{
    internal static class TodoGlyphTestClassificationDefinition
    {
        /// <summary>
        /// Defines the "TodoGlyphTest" classification type.
        /// </summary>
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("CoverGlyph")]
        internal static ClassificationTypeDefinition TodoGlyphTestType = null;
    }
}
