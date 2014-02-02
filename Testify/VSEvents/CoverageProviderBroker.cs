using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Leem.Testify.Domain;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Composition;
using EnvDTE;

namespace Leem.Testify.VSEvents
{

    [Export(typeof(ICoverageProviderBroker))]
    public class CoverageProviderBroker : ICoverageProviderBroker
    {

         private Dictionary<ITextBuffer,CoverageProvider> dictionary;
         private DataLayer.TestifyQueries _testifyQueries;

         [ImportingConstructor]
         public CoverageProviderBroker(SVsServiceProvider serviceProvider)
         {
             dictionary = new Dictionary<ITextBuffer, CoverageProvider>();
             var dte = (DTE)serviceProvider.GetService(typeof(DTE));
             _testifyQueries = new DataLayer.TestifyQueries(dte.Solution.FullName);
         }
        ICoverageService coverageService = new CoverageService();
        public CoverageProvider GetCoverageProvider(ITextBuffer buffer, EnvDTE.DTE dte, SVsServiceProvider serviceProvider)
        {
            CoverageProvider provider;
            if (dictionary.TryGetValue(buffer, out provider))
            {
                return provider;
            }
            else 
            {
                provider = new CoverageProvider(buffer, dte, serviceProvider, _testifyQueries);
                dictionary.Add(buffer,provider);
               // provider.VerifyProjects();
            }

            return provider;
        }
    }
}
