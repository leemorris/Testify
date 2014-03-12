using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using log4net;
using System.Globalization;

namespace Leem.Testify
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(CoverTag))]
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    class CoverTaggerProvider : ITaggerProvider
    {
        private ILog Log = LogManager.GetLogger(typeof(CoverTaggerProvider));

        [Import]
        internal IClassifierAggregatorService AggregatorService;

        [Import]
        internal SVsServiceProvider serviceProvider = null;

        [Import]
        internal ICoverageProviderBroker coverageProviderBroker;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            //System.Windows.Forms.MessageBox.Show(string.Format(CultureInfo.CurrentCulture, "Inside CreateTagger", this.ToString()));
            Log.DebugFormat("Inside CreateTagger");
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            return new CoverTagger(AggregatorService.GetClassifier(buffer), buffer, serviceProvider, coverageProviderBroker) as ITagger<T>;
        }


    }
}
