using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Leem.Testify.VSEvents
{
    [ContentType("code")]
    [Export(typeof(IWpfTextViewCreationListener))]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    class TextViewCreationListener : IWpfTextViewCreationListener 
    {
        [Import]
        internal SVsServiceProvider serviceProvider = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            
            new Formatter(textView, serviceProvider);
        }
    }
}
