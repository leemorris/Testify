using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Leem.Testify.VSEvents
{
    internal static class TestifyClassificationDefinition
    {
        /// <summary>
        /// Defines the "Testify" classification type.
        /// </summary>
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("CoverGlyph")]
        internal static ClassificationTypeDefinition TestifyType = null;
    }
}
