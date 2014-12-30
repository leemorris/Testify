using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

namespace Leem.Testify
{
    public interface ICoverageProviderBroker
    {
        CoverageProvider GetCoverageProvider(IWpfTextView buffer, DTE dte, SVsServiceProvider serviceProvider);
    }
}