using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Composition;
using EnvDTE;
using Microsoft.VisualStudio.Text.Editor;

namespace Leem.Testify.VSEvents
{

    [Export(typeof(ICoverageProviderBroker))]
    public class CoverageProviderBroker : ICoverageProviderBroker
    {

        private Dictionary<IWpfTextView, CoverageProvider> dictionary;
         private TestifyQueries _testifyQueries;

         [ImportingConstructor]
         public CoverageProviderBroker(SVsServiceProvider serviceProvider)
         {
             dictionary = new Dictionary<IWpfTextView, CoverageProvider>();
             var dte = (DTE)serviceProvider.GetService(typeof(DTE));
             _testifyQueries = new TestifyQueries(dte.Solution.FullName);
         }
        ICoverageService coverageService = new CoverageService();
        public CoverageProvider GetCoverageProvider(IWpfTextView textView, EnvDTE.DTE dte, SVsServiceProvider serviceProvider)
        {
            CoverageProvider provider;
            if (dictionary.TryGetValue(textView, out provider))
            {
                return provider;
            }
            else 
            {
                provider = new CoverageProvider(textView, dte, serviceProvider, _testifyQueries);
                dictionary.Add(textView, provider);
               // provider.VerifyProjects();
            }

            return provider;
        }
    }
}
