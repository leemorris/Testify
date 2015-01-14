using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Leem.Testify
{
    [Export(typeof (IWpfTextViewMarginProvider))]
    [Name(CoverageMargin.MarginName)]
    [Order(After = PredefinedMarginNames.Glyph)]
    [MarginContainer(PredefinedMarginNames.Left)]
    [ContentType("code")] //Show this margin for all text-based types
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class MarginFactory : IWpfTextViewMarginProvider
    {
        [Import] internal ICoverageProviderBroker CoverageProviderBroker;

        [Import] internal SVsServiceProvider ServiceProvider;


        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            CodeMarkManager codeMarkManager = textViewHost.TextView.Properties.GetOrCreateSingletonProperty<CodeMarkManager>
                (delegate { return new CodeMarkManager(); });

            return new CoverageMargin(textViewHost, ServiceProvider, CoverageProviderBroker);
        }
    }
}