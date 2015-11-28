using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Leem.Testify.VSEvents
{
    [Export(typeof(ICoverageProviderBroker))]
    public class CoverageProviderBroker : ICoverageProviderBroker
    {
        private Dictionary<string, CoverageProvider> dictionary;
        private TestifyQueries _testifyQueries;

        [ImportingConstructor]
        public CoverageProviderBroker(SVsServiceProvider serviceProvider)
        {
            dictionary = new Dictionary<string, CoverageProvider>();
            var dte = (DTE)serviceProvider.GetService(typeof(DTE));
            _testifyQueries = TestifyQueries.Instance;
        }

        private ICoverageService coverageService = new CoverageService();

        public CoverageProvider GetCoverageProvider(IWpfTextView textView, EnvDTE.DTE dte, SVsServiceProvider serviceProvider, TestifyContext context)
        {
            CoverageProvider provider;
            var filename = CoverageProvider.GetFileName(textView.TextBuffer);
            if (dictionary.TryGetValue(filename, out provider))
            {
                return provider;
            }
            else
            {
                provider = new CoverageProvider(textView, dte, serviceProvider, _testifyQueries, context);
                dictionary.Add(filename, provider);
            }

            return provider;
        }
    }
}