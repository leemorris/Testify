using System;
using Leem.Testify.Domain;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;

namespace Leem.Testify.VSEvents
{
    interface ICoverageProviderBroker
    {
        CoverageProvider GetCoverageProvider(ITextBuffer buffer, EnvDTE.DTE dte, Microsoft.VisualStudio.Shell.SVsServiceProvider serviceProvider);
    }
}
