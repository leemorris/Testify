using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;
using System;
using Microsoft.VisualStudio.Editor;

namespace Leem.Testify
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(CoverageMargin.MarginName)]
    [Order(After = PredefinedMarginNames.Glyph)]
    [MarginContainer(PredefinedMarginNames.Left)]
    [ContentType("code")] //Show this margin for all text-based types
    [TextViewRole(PredefinedTextViewRoles.Document)]

    internal sealed class MarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        internal ICoverageProviderBroker coverageProviderBroker;

        [Import]
        internal SVsServiceProvider serviceProvider = null;

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            // create an instance of the manager and associate it with this bookmark margin
            CodeMarkManager codeMarkManager = textViewHost.TextView.Properties.GetOrCreateSingletonProperty<CodeMarkManager>
                (delegate { return new CodeMarkManager(); });

            return new CoverageMargin(textViewHost, serviceProvider, coverageProviderBroker);
        }


    }
}
