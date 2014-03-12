using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;


namespace VertexVerveInc.NumberedBookmarks
{
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidNumberedBookmarksPkgString)]
    public sealed class NumberedBookmarksPackage : Package
    {
        public NumberedBookmarksPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null)
            {
                // add commands for adding all bookmarks
                // in each one of them I have used an anonymous method to call AddBookmark method
                CommandID menuCommandBookmark0 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark0);
                MenuCommand subItemBookmark0 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(0); }), menuCommandBookmark0);
                mcs.AddCommand(subItemBookmark0);

                CommandID menuCommandBookmark1 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark1);
                MenuCommand subItemBookmark1 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(1); }), menuCommandBookmark1);
                mcs.AddCommand(subItemBookmark1);

                CommandID menuCommandBookmark2 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark2);
                MenuCommand subItemBookmark2 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(2); }), menuCommandBookmark2);
                mcs.AddCommand(subItemBookmark2);

                CommandID menuCommandBookmark3 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark3);
                MenuCommand subItemBookmark3 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(3); }), menuCommandBookmark3);
                mcs.AddCommand(subItemBookmark3);

                CommandID menuCommandBookmark4 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark4);
                MenuCommand subItemBookmark4 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(4); }), menuCommandBookmark4);
                mcs.AddCommand(subItemBookmark4);

                CommandID menuCommandBookmark5 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark5);
                MenuCommand subItemBookmark5 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(5); }), menuCommandBookmark5);
                mcs.AddCommand(subItemBookmark5);

                CommandID menuCommandBookmark6 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark6);
                MenuCommand subItemBookmark6 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(6); }), menuCommandBookmark6);
                mcs.AddCommand(subItemBookmark6);

                CommandID menuCommandBookmark7 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark7);
                MenuCommand subItemBookmark7 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(7); }), menuCommandBookmark7);
                mcs.AddCommand(subItemBookmark7);

                CommandID menuCommandBookmark8 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark8);
                MenuCommand subItemBookmark8 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(8); }), menuCommandBookmark8);
                mcs.AddCommand(subItemBookmark8);

                CommandID menuCommandBookmark9 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark9);
                MenuCommand subItemBookmark9 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(9); }), menuCommandBookmark9);
                mcs.AddCommand(subItemBookmark9);

                // add command for Clearing Bookmarks
                // again I have opted for an anonymous method
                CommandID menuCommandClearBookmark = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdClearBookmarks);
                MenuCommand subItemClearBookmark = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { ClearAllBookmarks(); }), menuCommandClearBookmark);
                mcs.AddCommand(subItemClearBookmark);
            }
        }

        // remove all bookmarks from the margin
        private void ClearAllBookmarks()
        {
            // get the instance associated with this margin
            BookmarkManager bookmarkManager = GetBookMarkManager();
            // remove all bookmarks from the manager
            bookmarkManager.ClearAllBookmarks();
        }

        // add a bookmark if it is not present otherwise
        // move to the specified bookmark location
        private void AddOrMoveToBookmark(int bookmarkNumber)
        {
            // get the instance of bookmark manager
            BookmarkManager bookmarkManager = GetBookMarkManager();
            if (!bookmarkManager.Bookmarks.ContainsKey(bookmarkNumber))
            {
                // the bookmark does not exist so add it
                AddBookmark(bookmarkNumber);
            }
            else
            {
                // the bookmark is already there, so move to its location
                bookmarkManager.GotoBookmark(bookmarkNumber);
            }
        }

        private void AddBookmark(int bookmarkNumber)
        {
            // get an instance of bookmark manager
            BookmarkManager bookmarkManager = GetBookMarkManager();
            // get the currently active document
            string documentName = GetDocumentName();
            // get current line number (cursor position)
            int lineNumber = GetLineNumber();
            // get current column number (cursor position)
            int columnNumber = GetColumnNumber();
            // if some values are invalid, don't add the bookmark
            if (string.IsNullOrEmpty(documentName) || lineNumber <= 0 || columnNumber <= 0)
            {
                return;
            }

            // everything is fine so let's add the bookmark by using AddBookmark of the bookmark manager
            bookmarkManager.AddBookmark(bookmarkNumber, documentName, lineNumber, columnNumber, GetDTE2());
        }

        private string GetDocumentName()
        {
            // get the DTE2 object
            DTE2 dte2 = GetDTE2();
            if (dte2 == null)
            {
                return string.Empty;
            }

            // get the ActiveDocument name from DTE2 object
            return dte2.ActiveDocument.Name;
        }

        private int GetLineNumber()
        {
            // get the DTE2 object
            DTE2 dte2 = GetDTE2();
            if (dte2 == null)
            {
                return 0;
            }

            // get currently active cursor location
            VirtualPoint point = dte2.ActiveDocument.Selection.ActivePoint;
            return point.Line; // get the line number from the location
        }

        private int GetColumnNumber()
        {
            // get the DTE2 object
            DTE2 dte2 = GetDTE2();
            if (dte2 == null)
            {
                return 0;
            }

            // get currently active cursor position
            VirtualPoint point = dte2.ActiveDocument.Selection.ActivePoint;
            return point.DisplayColumn; // get the column number from the location
        }

        private DTE2 GetDTE2()
        {
            // get the instance of DTE
            DTE dte = (DTE)GetService(typeof(DTE));
            // cast it as DTE2, historical reasons
            DTE2 dte2 = dte as DTE2;

            if (dte2 == null)
            {
                return null;
            }

            return dte2;
        }

        // finds the instance of IWpfTextViewHost associated with this margin
        private IWpfTextViewHost GetIWpfTextViewHost()
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

        private BookmarkManager GetBookMarkManager()
        {
            // get an instance of the associated IWpfTextViewHost
            IWpfTextViewHost viewHost = GetIWpfTextViewHost();
            if (viewHost == null)
            {
                return null;
            }

            // try to get the associated bookmark manager from the IWpfTextView otherwise create a new instance for it
            BookmarkManager bookmarkManager = viewHost.TextView.Properties.GetOrCreateSingletonProperty<BookmarkManager>
                (delegate { return new BookmarkManager(); });

            return bookmarkManager;
        }
    }
}
