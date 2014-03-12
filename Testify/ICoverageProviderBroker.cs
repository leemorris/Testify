using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

namespace Leem.Testify
{
    interface ICoverageProviderBroker
    {
        CoverageProvider GetCoverageProvider(IWpfTextView buffer, EnvDTE.DTE dte, Microsoft.VisualStudio.Shell.SVsServiceProvider serviceProvider);
    }
}
