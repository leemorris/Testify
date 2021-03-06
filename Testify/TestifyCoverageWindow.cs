﻿using Leem.Testify.SummaryView;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Composition;

namespace Leem.Testify
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("36c4a332-1b9b-49ce-9e45-da8bd399092c")]
    public class TestifyCoverageWindow : ToolWindowPane
    {
        private IVsUIShell5 _shell5;

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public TestifyCoverageWindow() :
            base(null)
        {
            Initialize();
            // Set the window title reading it from the resources.
            this.Caption = Resources.ToolWindowCodeCoverage;
            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;

            
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.

            var themeRespourceKey = new ThemeResourceKey(new System.Guid("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d"), "ToolWindowBackground", 0);
            //IVsUIShell5 shell5 = (IVsUIShell5)GetService(typeof(SVsUIShell));
            //var themeColor = VsColors.GetThemedWPFColor(_shell5, themeRespourceKey);
            //var colorBrush = new System.Windows.Media.SolidColorBrush(themeColor);
            //var handle = base.Window.Handle;
            //System.Windows.Forms.Control someControl = System.Windows.Forms.Control.FromHandle(handle);
            //someControl.BackColor = System.Drawing.Color.FromArgb(themeColor.A,themeColor.R,themeColor.G,themeColor.B);
            base.Content = new SummaryViewControl(this, _shell5);
           
        }

        protected override void Initialize()
        {
            _shell5 = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell5;

        }
    }
}