using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;

namespace Leem.Testify
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(CoverageMargin.MarginName)]
    [Order(Before = PredefinedMarginNames.Glyph)]
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
            GetIWpfTextViewHost();

            return new CoverageMargin(textViewHost, serviceProvider, coverageProviderBroker);
        }

        // finds the instance of IWpfTextViewHost associated with this margin
        public IWpfTextViewHost GetIWpfTextViewHost()
        {
            // get an instance of IVsTextManager
            IVsTextManager txtMgr = (IVsTextManager)GetService(typeof(SVsTextManager));
            IVsTextView vTextView = null;
            int mustHaveFocus = 1;
            // get the active view from the TextManager
            txtMgr.GetActiveView(mustHaveFocus, null, out vTextView);

            // cast as IVsUSerData
            IVsUserData userData = vTextView as IVsUserData;
            if (userData == null)
            {
                Trace.WriteLine("No text view is currently open");
                return null;
            }

            IWpfTextViewHost viewHost;
            object holder;
            // get the IWpfTextviewHost using the predefined guid for it
            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            userData.GetData(ref guidViewHost, out holder);
            // convert to IWpfTextviewHost
            viewHost = (IWpfTextViewHost)holder;
            return viewHost;
        }
    }
}
