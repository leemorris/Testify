using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

//using Microsoft.VisualStudio.Editor;

namespace Leem.Testify.UnitTestAdornment
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class UnitTestAdornmentFactory : IWpfTextViewCreationListener
    {
        private IVsUIShell uiShell;

        [Import]
        internal SVsServiceProvider serviceProvider = null;

        public UnitTestAdornmentFactory()
        {
            //uiShell = (IVsUIShell)ServiceProvider.GlobalProvider.GetService(typeof(IVsUIShell));
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            var manager = UnitTestAdornmentManager.Create(textView);
        }

        //public void Execute(IWpfTextView view, string blogUrl)
        //{
        //    //Add a UnitTestSelector adjacent to the CodeMarkGlyph.
        //    //Get the provider for the Unit Test adornments in the property bag of the view.
        //    UnitTestAdornmentManager manager = view.Properties.GetProperty<UnitTestAdornmentManager>(typeof(UnitTestAdornmentManager));
        //    manager.Colors = GetColorList();
        //    //Add the post adornment using the provider.
        //   // provider.Add(view.Selection.SelectedSpans[0], blogUrl);
        //}

        [Export(typeof(AdornmentLayerDefinition))]
        [Name("PostAdornmentLayer")]
        [Order(After = PredefinedAdornmentLayers.Text)]
        public AdornmentLayerDefinition postLayerDefinition;
    }
}