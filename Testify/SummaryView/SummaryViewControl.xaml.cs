using EnvDTE;
using EnvDTE80;
using Leem.Testify.SummaryView.ViewModel;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Resources;

namespace Leem.Testify.SummaryView
{
    public partial class SummaryViewControl// : UserControl
    {
        private ITestifyQueries _queries;
        private TestifyCoverageWindow _parent;
        private bool wasCalledForMethod;
        //private readonly ILog Log = LogManager.GetLogger(typeof(SummaryViewControl));
        private TestifyContext _context;
        private CoverageViewModel _coverageViewModel;
        private SynchronizationContext _uiContext;
        private System.Windows.Media.SolidColorBrush _brush;
        private System.Windows.Media.Color _backgroundColor;
        private readonly IVsUIShell5 _vsUIShell5;
        public Dictionary<string, Bitmap> IconCache;

        public SummaryViewControl(TestifyCoverageWindow parent,IVsUIShell5 shell )
        {
            InitializeComponent();
            _parent = parent;
            _queries = TestifyQueries.Instance;
            _uiContext = SynchronizationContext.Current;
            IconCache = new Dictionary<string, Bitmap>();
            IconCache = GetIcons(shell);
            if (TestifyQueries.SolutionName != null)
            {
                _context = new TestifyContext(TestifyQueries.SolutionName);

                BuildCoverageViewModel();
 
              
            }
            else
            {
                base.DataContext = new SummaryViewModel(_context, IconCache);
            }
        }
        public System.Windows.Media.Color BackgroundColor { get; set; }
        private void BuildCoverageViewModel()
        {
            _coverageViewModel = GetSummaries(_context);
            _coverageViewModel.UiContext = _uiContext;

            if (_coverageViewModel.Modules.Count > 0)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    base.DataContext = _coverageViewModel;
                }));
            }
            else
            {
                base.Content = "Waiting for Solution to be Built";
            }
        }

        private CoverageViewModel GetSummaries(TestifyContext context)
        {
            Poco.CodeModule[] modules = _queries.GetModules(context);
            var coverageViewModel = new CoverageViewModel(modules, context, _uiContext, IconCache);

            return coverageViewModel;
        }

        private void ItemDoubleClicked(object sender, RoutedEventArgs e)
        {
            // This event is raised multiple times, if the user double -clicks on a Method, this is fired for Method, Class and Module
            // if the user double-clicks on a Class, this is fired for the Class and Module
            _queries = TestifyQueries.Instance;
            string filePath = string.Empty;
            int line = 0;
            int column = 0;
            string type = ((HeaderedItemsControl)(e.Source)).Header.ToString();
            string name = string.Empty;
            EnvDTE.Window openDocumentWindow;
            var clickedMethodName = string.Empty;

            var dte = TestifyPackage.GetGlobalService(typeof(DTE)) as DTE2;

            if (dte.ActiveDocument != null)
            {
                if (type == "Leem.Testify.SummaryView.ViewModel.MethodViewModel")
                {
                    wasCalledForMethod = true; // set flag so we know this event fired for a Method
                    clickedMethodName = ((MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FullName;

                    filePath = ((MethodViewModel)(((HeaderedItemsControl)(e.Source)).Header)).FileName;
                    if (filePath == null)
                    {
                        filePath = (((ClassViewModel)(((Leem.Testify.SummaryView.ViewModel.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)((e.Source))).Header)).Parent))._class).FileName;
                    }
                    line = ((MethodViewModel)(((HeaderedItemsControl)(e.Source)).Header)).Line;
                    line = line > 1 ? line-- : 1;
                    column = ((MethodViewModel)(((HeaderedItemsControl)(e.Source)).Header)).Column;
                    column = column > 1 ? column-- : 1;
                }
                if (type == "Leem.Testify.SummaryView.ViewModel.ClassViewModel" && wasCalledForMethod == false)
                {
                    // If event wasn't fired for a Method, then we can navigate to the class
                    clickedMethodName = ((ClassViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Name;

                    filePath = ((ClassViewModel)(((HeaderedItemsControl)(e.Source)).Header)).FileName;
                    line = ((ClassViewModel)(((HeaderedItemsControl)(e.Source)).Header)).Line - 1;
                    column = 1;
                }
                if (type == "Leem.Testify.SummaryView.ViewModel.ModuleViewModel")
                {
                    // This event is fired for the Module as the last step. Re-set the Method flag and do nothing else.
                    wasCalledForMethod = false;
                }
            }

            if (!string.IsNullOrEmpty(filePath) && filePath != string.Empty && !dte.ItemOperations.IsFileOpen(filePath))
            {
                openDocumentWindow = dte.ItemOperations.OpenFile(filePath);
                if (openDocumentWindow != null)
                {
                    openDocumentWindow.Activate();
                }
                else
                {
                    for (var i = 1; i == dte.Documents.Count; i++)
                    {
                        if (dte.Documents.Item(i).Name == name)
                        {
                            openDocumentWindow = dte.Documents.Item(i).ProjectItem.Document.ActiveWindow;
                        }
                    }
                }
            }
            else
            {
                for (var i = 1; i <= dte.Windows.Count; i++)
                {
                    var window = dte.Windows.Item(i);
                    if (window.Document != null && window.Document.FullName.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        openDocumentWindow = window;
                        openDocumentWindow.Activate();
                        var selection = window.Document.DTE.ActiveDocument.Selection as TextSelection;
                        selection.StartOfDocument();
                        selection.MoveToLineAndOffset(line, column, true);

                        selection.SelectLine();

                        continue;
                    }
                }
            }
        }

        public void Connect(int connectionId, object target)
        {
            throw new NotImplementedException();
        }

        private Dictionary<string, Bitmap> GetIcons(IVsUIShell5 shell5)
        {
            const string COLOR_NAME = "ToolWindowBackground";
            const string GUID_COLOR_TABLE_ENVIRONMENT_CATEGORY = "624ed9c3-bdfd-41fa-96c3-7c824ea32e3d";
            uint backgroundColor = shell5.GetThemedColor(new System.Guid(GUID_COLOR_TABLE_ENVIRONMENT_CATEGORY), COLOR_NAME, 0);
            var toolwindowbackground = VsColors.ToolWindowBackgroundKey;
            // Assume that the transparent color is almost green. It can be any color
            var almostGreenColor = System.Drawing.Color.FromArgb(0, 254, 0);
            var pinkish = System.Drawing.Color.FromArgb(255, 0, 255);
            ResourceManager resourceManager = new ResourceManager ("Leem.Testify.Resources", GetType ().Assembly);

            var testifyPath = Path.GetDirectoryName(typeof(TestifyPackage).Assembly.Location);

            // Project Icons

            
            var cSharpProjectIcon = (Bitmap)resourceManager.GetObject("CSharpProject");
            var vbProjectIcon = (Bitmap)resourceManager.GetObject("VBProject");
            var cSharpFileIcon = (Bitmap)resourceManager.GetObject("CSharpFile");
            var vbFileIcon = (Bitmap)resourceManager.GetObject("VBFile");
            var folderIcon = (Bitmap)resourceManager.GetObject("Folder");

            if (backgroundColor == 4280689957)//4294309365  = FFF5F5F5
            {
                cSharpProjectIcon = InvertBitmaps(cSharpProjectIcon, shell5, backgroundColor, pinkish);
                vbProjectIcon = InvertBitmaps(vbProjectIcon, shell5, backgroundColor, pinkish);
                cSharpFileIcon = InvertBitmaps(cSharpFileIcon, shell5, backgroundColor, pinkish);
                vbFileIcon = InvertBitmaps(vbFileIcon, shell5, backgroundColor, pinkish);
                folderIcon = InvertBitmaps(folderIcon, shell5, backgroundColor, pinkish);
        }

            IconCache.Add("C#Project", cSharpProjectIcon);
            IconCache.Add("VbProject", vbProjectIcon);
            IconCache.Add("C#File", cSharpFileIcon);
            IconCache.Add("VbFile", vbFileIcon);
            IconCache.Add("Folder", folderIcon);
           
            return IconCache;
        }

        private Bitmap InvertBitmaps(Bitmap bitmap, IVsUIShell5 shell5, uint backgroundColor, System.Drawing.Color transparentColor)
        {
            // Get the unthemed input bitmap.
            // Note: Change the modifer from private to public
            Bitmap inputBitmap = null;

            inputBitmap = new Bitmap(bitmap);

                // Get the themed output bitmap
            var outputBitmap = GetInvertedBitmap(shell5, inputBitmap, transparentColor, backgroundColor);
            //}


            inputBitmap.Dispose();
            return outputBitmap;
        }

        private unsafe Bitmap GetInvertedBitmap(IVsUIShell5 shell5, Bitmap inputBitmap,
                 System.Drawing.Color transparentColor, uint backgroundColor)
        {
            Bitmap outputBitmap = null;
            byte[] outputBytes = null;
            Rectangle rect;
            System.Drawing.Imaging.BitmapData bitmapData = null;
            IntPtr sourcePointer;
            int length = 0;

            //try
            //{
            outputBitmap = new Bitmap(inputBitmap);

            outputBitmap.MakeTransparent(transparentColor);
           // if (backgroundColor == 4280689957)
           // {
                outputBitmap.MakeTransparent(System.Drawing.Color.FromArgb(255, 245, 245, 245));
                outputBitmap.MakeTransparent(System.Drawing.Color.FromArgb(255, 0, 0, 0));
            //}
            //else { }
            rect = new Rectangle(0, 0, outputBitmap.Width, outputBitmap.Height);

            bitmapData = outputBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, outputBitmap.PixelFormat);

            sourcePointer = bitmapData.Scan0;

            length = (Math.Abs(bitmapData.Stride) * outputBitmap.Height);

            outputBytes = new byte[length];

            Marshal.Copy(sourcePointer, outputBytes, 0, length);

                shell5.ThemeDIBits((UInt32)outputBytes.Length, outputBytes, (UInt32)outputBitmap.Width,
                (UInt32)outputBitmap.Height, true, backgroundColor);

            Marshal.Copy(outputBytes, 0, sourcePointer, length);
            //if (backgroundColor != 4294309365)
            //{
            //    var replacement = System.Drawing.Color.FromArgb((int)backgroundColor);
            //    var toReplace = System.Drawing.Color.FromArgb(16185078);
            //    const int pixelSize = 4; // 32 bits per pixel

            //    for (int y = 0; y < bitmapData.Height; ++y)
            //    {
            //        byte* sourceRow = (byte*)bitmapData.Scan0 + (y * bitmapData.Stride);
            //        //byte* targetRow = (byte*)targetData.Scan0 + (y * targetData.Stride);

            //        for (int x = 0; x < bitmapData.Width; ++x)
            //        {
            //            byte b = sourceRow[x * pixelSize + 0];
            //            byte g = sourceRow[x * pixelSize + 1];
            //            byte r = sourceRow[x * pixelSize + 2];
            //            byte a = sourceRow[x * pixelSize + 3];

            //            if (toReplace.R == r && toReplace.G == g && toReplace.B == b)
            //            {
            //                r = replacement.R;
            //                g = replacement.G;
            //                b = replacement.B;
            //            }

            //            //targetRow[x * pixelSize + 0] = b;
            //            //targetRow[x * pixelSize + 1] = g;
            //            //targetRow[x * pixelSize + 2] = r;
            //            //targetRow[x * pixelSize + 3] = a;
            //        }
            //    }
            //}
            outputBitmap.UnlockBits(bitmapData);

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.ToString());
            //}

            return outputBitmap;
        }
        //private string _currentThemeId;

        //// cache icons for specific themes: <<ThemeId, IconForLightTheme>, IconForThemeId>
        //private readonly Dictionary<Tuple<string, BitmapImage>, BitmapImage> _cacheThemeIcons =
        //  new Dictionary<Tuple<string, BitmapImage>, BitmapImage>();

        //protected override BitmapImage GetIconCurrentTheme(BitmapImage iconLight)
        //{
        //    Debug.Assert(iconLight != null);
        //    return _currentThemeId.ToThemesEnum() == Themes.Light ? iconLight : GetCachedIcon(iconLight);
        //}

        //private BitmapImage GetCachedIcon(BitmapImage iconLight)
        //{
        //    BitmapImage cachedIcon;
        //    var key = Tuple.Create(_currentThemeId, iconLight);
        //    if (_cacheThemeIcons.TryGetValue(key, out cachedIcon))
        //    {
        //        return cachedIcon;
        //    }

        //    var backgroundColor = UIElement..FindResource<System.Drawing.Color>(VsColors.ToolWindowBackgroundKey);
        //    cachedIcon = CreateInvertedIcon(iconLight, backgroundColor);
        //    _cacheThemeIcons.Add(key, cachedIcon);
        //    return cachedIcon;
        //}
        //private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        //{
        //    // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

        //    using (MemoryStream outStream = new MemoryStream())
        //    {
        //        BitmapEncoder enc = new BmpBitmapEncoder();
        //        enc.Frames.Add(BitmapFrame.Create(bitmapImage));
        //        enc.Save(outStream);
        //        System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

        //        return new Bitmap(bitmap);
        //    }
        //}
        //[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        //public static extern bool DeleteObject(IntPtr hObject);

        //private BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        //{
        //    IntPtr hBitmap = bitmap.GetHbitmap();
        //    BitmapImage retval;

        //    try
        //    {
        //        retval = Imaging.CreateBitmapSourceFromHBitmap(
        //                     hBitmap,
        //                     IntPtr.Zero,
        //                     Int32Rect.Empty,
        //                     BitmapSizeOptions.FromEmptyOptions());
        //    }
        //    finally
        //    {
        //        DeleteObject(hBitmap);
        //    }

        //    return retval;
        //}
        //private BitmapImage CreateInvertedIcon(BitmapImage inputIcon, System.Drawing.Color backgroundColor)
        //{
        //    using (var bitmap = BitmapImage2Bitmap(inputIcon))
        //    {
        //        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        //        var bitmapData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
        //        var sourcePointer = bitmapData.Scan0;
        //        var length = Math.Abs(bitmapData.Stride) * bitmap.Height;
        //        var outputBytes = new byte[length];
        //        Marshal.Copy(sourcePointer, outputBytes, 0, length);
        //        _vsUIShell5.ThemeDIBits((UInt32)outputBytes.Length, outputBytes, (UInt32)bitmap.Width,
        //                                (UInt32)bitmap.Height, true, backgroundColor.ToUInt());
        //        Marshal.Copy(outputBytes, 0, sourcePointer, length);
        //        bitmap.UnlockBits(bitmapData);
        //        return Bitmap2BitmapImage(bitmap);
        //    }
        //}
    }
}