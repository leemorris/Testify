using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Leem.Testify
{
    public partial class CodeMarkGlyph : UserControl
    {
        //private CodeMarkManager _codeMarkManager;
        private Poco.CoveredLinePoco _coveredLine;
        private IWpfTextView view;

        public CodeMarkGlyph()
        {
            // initialize all components
            InitializeComponent();
            this.MouseLeftButtonDown += new MouseButtonEventHandler(CodeMarkGlyph_MouseLeftButtonDown);
            this.MouseRightButtonDown += new MouseButtonEventHandler(CodeMarkGlyph_MouseRightButtonDown);
        }

        public CodeMarkGlyph(IWpfTextView view, Poco.CoveredLinePoco line, double yPosition)
            : this()
        {
            YPosition = yPosition;
            this.view = view;
            _coveredLine = line;
            ellipse.Width = ellipse.Height;
            if (!line.UnitTests.Any() && line.IsCode)
            {
                ellipse.Fill = new SolidColorBrush(Colors.Orange);
                ellipse.Stroke = new SolidColorBrush(Colors.Orange);
            }
            else if (line.UnitTests.Any(x => x.IsSuccessful.Equals(true)))
            {
                ellipse.Fill = new SolidColorBrush(Colors.Green);
                ellipse.Stroke = new SolidColorBrush(Colors.Green);
            }
            else
            {
                ellipse.Fill = new SolidColorBrush(Colors.Red);
                ellipse.Stroke = new SolidColorBrush(Colors.Red);
            }

            // }
            // position the text bloc
            //text.HorizontalAlignment = HorizontalAlignment.Center;
            //text.VerticalAlignment = VerticalAlignment.Center;
            //// change font settings
            //text.FontFamily = new FontFamily("Verdana");
            //text.FontSize = 12;
            //text.FontWeight = FontWeights.ExtraBold;
            //text.Foreground = new SolidColorBrush(Colors.White);
            //text.Width = 16;
            //text.Height = 16;
            //Canvas.SetLeft(text, 3.5);
            //Canvas.SetTop(text, 0.5);
            //Canvas.SetZIndex(text, 1);
            //LayoutRoot.Children.Add(text);
        }


        public double YPosition { get; set; }
        //public CodeMarkGlyph(Poco.CoveredLinePoco line, CodeMarkManager codeMarkManager)
        ////  : this(number)
        //{
        //    // assign the bookmark manager with which this glyph is associated
        //    _codeMarkManager = codeMarkManager;
        //    // subscribe mouse events
        //    this.MouseLeftButtonDown += new MouseButtonEventHandler(CodeMarkGlyph_MouseLeftButtonDown);
        //    this.MouseRightButtonDown += new MouseButtonEventHandler(CodeMarkGlyph_MouseRightButtonDown);
        //}

        private void CodeMarkGlyph_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var glyph = (CodeMarkGlyph)sender;
            var manager = Leem.Testify.UnitTestAdornment.UnitTestAdornmentManager.Create(view);

            //var provider  = Leem.Testify.UnitTestAdornment.UnitTestAdornmentProvider.Create(view);

            var snapshotSpanLine = view.TextSnapshot.GetLineFromLineNumber(glyph._coveredLine.LineNumber);
            var snapshotSpan =  new SnapshotSpan(snapshotSpanLine.Start,snapshotSpanLine.End);
            var unitTestAdornment = new UnitTestAdornment.UnitTestAdornment(snapshotSpan,glyph._coveredLine,glyph.YPosition);

            view.GetAdornmentLayer("PostAdornmentLayer").RemoveAllAdornments();            
            if (unitTestAdornment.CoveredLine.UnitTests.Any())
            {
                manager.DisplayUnitTestSelector(unitTestAdornment);
            }

           
            //provider.Add(snapshotSpan, glyph._coveredLine.UnitTests);
           // Leem.Testify.UnitTestAdornment.UnitTestAdornmentProvider.Add(SnapshotSpan snapshotSpan);

           // var x = 1;
           // var package = new TestifyPackage();
           // package.TEST(sender, e);

           // // call GoToBookmark function of the manager on left mouse button down event
           // // _coverageManager.GotoBookmark(BookmarkNumber);


           //// control.Visibility = Visibility;
           // Popup codePopup = new Popup();
           // TextBlock popupText = new TextBlock();
           // popupText.Text = _coveredLine.UnitTests.First().TestMethodName;
           // popupText.Background = Brushes.LightBlue;
           // popupText.Foreground = Brushes.Blue;
           // //codePopup.Child = popupText;
           // codePopup.Child = new Button { Content = popupText };
           // codePopup.MouseLeftButtonDown += new MouseButtonEventHandler(PopupClicked);
           // codePopup.PlacementTarget = this;
           // codePopup.HorizontalOffset = 10;
           // //codePopup.VerticalOffset = 5;
           // codePopup.IsOpen = true;
        }


        void CodeMarkGlyph_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var x = 1;
        //    // call RemoveBookmark function of the manager on right mouse button down event
        //    _coverageManager.RemoveBookmark(BookmarkNumber);
        }
        void PopupClicked(object sender, MouseButtonEventArgs e)
        {
            var x = 1;
            //    // call RemoveBookmark function of the manager on right mouse button down event
            //    _coverageManager.RemoveBookmark(BookmarkNumber);
        }
    }
}