using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;


namespace Leem.Testify
{
    public partial class CodeMarkGlyph : UserControl
    {
		// bookmark number with which this glyph is associated
        public int BookmarkNumber { get; set; }

		// bookmark manager associated with current margin and this glyph
        private CodeMarkManager _codeMarkManager;

        public CodeMarkGlyph()
        {
			// initialize all components
            InitializeComponent();
        }

        public CodeMarkGlyph(IList<UnitTest> unitTests)
            : this()
        {
            var number = 1;
			// assign the bookmark number
            BookmarkNumber = number;

			// create a text block to hold the text of glyph (0, 1... ?)
            TextBlock text = new TextBlock();
            if (number != BookmarkManager.HelpBookmarkNumber)
            {
				// this is not a help bookmark so write the number
                text.Text = number.ToString();
            }
            else
            {
				// yes this is a help bookmark
                text.Text = "M"; // change the text to '?'
				// change the background fill to a new radient brush of green color
				// so that we can identify help bookmark from other bookmarks
                RadialGradientBrush brush = new RadialGradientBrush();
                brush.GradientOrigin = new Point(0.25, 0.15);
                GradientStopCollection stops = new GradientStopCollection();
                stops.Add(new GradientStop(Colors.LimeGreen, 0.2));
                stops.Add(new GradientStop(Colors.Green, 0.9));
                brush.GradientStops = stops;
                ellipse.Fill = brush;
                ellipse.Stroke = new SolidColorBrush(Colors.Green);
            }
			// position the text bloc
            text.HorizontalAlignment = HorizontalAlignment.Center;
            text.VerticalAlignment = VerticalAlignment.Center;
			// change font settings
            text.FontFamily = new FontFamily("Verdana");
            text.FontSize = 12;
            text.FontWeight = FontWeights.ExtraBold;
            text.Foreground = new SolidColorBrush(Colors.White);
            text.Width = 16;
            text.Height = 16;
            Canvas.SetLeft(text, 3.5);
            Canvas.SetTop(text, 0.5);
            Canvas.SetZIndex(text, 1);
            LayoutRoot.Children.Add(text);
        }

        public CodeMarkGlyph(Poco.CoveredLine line, CodeMarkManager codeMarkManager)
          //  : this(number)
        {
			// assign the bookmark manager with which this glyph is associated
            _codeMarkManager = codeMarkManager;
			// subscribe mouse events
            this.MouseLeftButtonDown += new MouseButtonEventHandler(CodeMarkGlyph_MouseLeftButtonDown);
            //this.MouseRightButtonDown += new MouseButtonEventHandler(CodeMarkGlyph_MouseRightButtonDown);
        }

        void CodeMarkGlyph_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
			// call GoToBookmark function of the manager on left mouse button down event
           // _coverageManager.GotoBookmark(BookmarkNumber);
        }

        //void CodeMarkGlyph_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    // call RemoveBookmark function of the manager on right mouse button down event
        //    _coverageManager.RemoveBookmark(BookmarkNumber);
        //}
    }
}
