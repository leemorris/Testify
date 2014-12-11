using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace Leem.Testify
{
    public partial class CodeMarkGlyph : UserControl
    {
        //private CodeMarkManager _codeMarkManager;
        private Poco.CoveredLinePoco _coveredLine;
        public CodeMarkGlyph()
        {
            // initialize all components
            InitializeComponent();
            this.MouseLeftButtonDown += new MouseButtonEventHandler(CodeMarkGlyph_MouseLeftButtonDown);
            this.MouseRightButtonDown += new MouseButtonEventHandler(CodeMarkGlyph_MouseRightButtonDown);
        }

        public CodeMarkGlyph(Poco.CoveredLinePoco line)
            : this()
        {
            _coveredLine = line;
            ellipse.Width = 6;
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
            var x = 1;
            var package = new TestifyPackage();
            package.TEST(sender, e);

            // call GoToBookmark function of the manager on left mouse button down event
            // _coverageManager.GotoBookmark(BookmarkNumber);


           // control.Visibility = Visibility;
            Popup codePopup = new Popup();
            TextBlock popupText = new TextBlock();
            popupText.Text = _coveredLine.UnitTests.First().TestMethodName;
            popupText.Background = Brushes.LightBlue;
            popupText.Foreground = Brushes.Blue;
            //codePopup.Child = popupText;
            codePopup.Child = new Button { Content = popupText };
            codePopup.MouseLeftButtonDown += new MouseButtonEventHandler(PopupClicked);
            codePopup.PlacementTarget = this;
            codePopup.HorizontalOffset = 10;
            //codePopup.VerticalOffset = 5;
            codePopup.IsOpen = true;
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