using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Leem.Testify
{
    //// Tells what type of export this class provides, which is IWpfTextViewMarginProvider in our case 
    //[Export(typeof(IWpfTextViewMarginProvider))]
    //// Tells the name of the export provided, which is BookmarkMargin.MarginName (a constant) in our case
    //[Name(BookmarkMargin.MarginName)]

    //// Tells MEF to order/arrange multiple instances of the extension
    //[Order(Before = PredefinedMarginNames.Glyph)]
    //// This attribute tells the name of the container (pre-defined constant), in our case it is PredefinedMarginNames.Right.
    //// Other options can be Left, Right, Top, Bottom, ScrollBar, ZoomControl, LineNumber, Spacer, Selection, Glyph etc
    //[MarginContainer(PredefinedMarginNames.Left)]
    //// Declares an association of extension with a particular type of content, which is code in our case
    //[ContentType("code")]
    //// Specifies what type of view the extension should be associated, in our case it is Document.
    //// Other options can be Editable, Debuggable, Zoomable etc
    //[TextViewRole(PredefinedTextViewRoles.Document)]

    //// a factory object to create an instance of the margin
    //internal sealed class MarginFactory : IWpfTextViewMarginProvider
    //{
    //    public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
    //    {
    //        // create an instance of the manager and associate it with this bookmark margin
    //        BookmarkManager bookmarkManager = textViewHost.TextView.Properties.GetOrCreateSingletonProperty<BookmarkManager>
    //            (delegate {return new BookmarkManager();});

    //        // create an object of the bookmark margin and associate the TextView and bookmark manager with it
    //        return new BookmarkMargin(textViewHost.TextView, bookmarkManager);
    //    }
    //} 
}
