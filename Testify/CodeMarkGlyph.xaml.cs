using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Leem.Testify.Poco;
using Leem.Testify.UnitTestAdornment;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Leem.Testify
{
    public partial class CodeMarkGlyph : UserControl
    {

        private readonly CoveredLine _coveredLine;
        private readonly IWpfTextView _view;
        private TestifyContext _context;
        public CodeMarkGlyph(TestifyContext context)
        {
            _context = context;
            // initialize all components
            InitializeComponent();
            MouseLeftButtonDown += CodeMarkGlyphMouseLeftButtonDown;
            MouseRightButtonDown += CodeMarkGlyph_MouseRightButtonDown;
        }

        public CodeMarkGlyph(IWpfTextView view, CoveredLine line, double yPosition, TestifyContext context)
            : this(context)
        {
            YPosition = yPosition;
            this._view = view;
            _coveredLine = line;
            Ellipse.Height = (view.LineHeight * view.ZoomLevel/100) * .8;
            Ellipse.Width = Ellipse.Height;
            if ((!line.IsCovered && line.IsCode) || (line.IsBranch && line.IsSuccessful && line.BranchCoverage < 100))
            {
                Ellipse.Fill = new SolidColorBrush(Colors.Orange);
                Ellipse.Stroke = new SolidColorBrush(Colors.Orange);
            }
            else if (line.IsSuccessful.Equals(true))
            {
                Ellipse.Fill = new SolidColorBrush(Colors.Green);
                Ellipse.Stroke = new SolidColorBrush(Colors.Green);
            }
            else
            {
                Ellipse.Fill = new SolidColorBrush(Colors.Red);
                Ellipse.Stroke = new SolidColorBrush(Colors.Red);
            }
            if (line.IsBranch)
            {
                GlyphCharacter.Text = ((char)'\u2144').ToString();
                GlyphCharacter.Height = 1.25 * Ellipse.Height;
                GlyphCharacter.FontSize = Math.Round(Ellipse.Height) + 1;


                GlyphCharacter.Margin = new System.Windows.Thickness(GlyphCharacter.Height / 10, -GlyphCharacter.Height / 4, 0, 0);


            }

        }


        private double YPosition { get; set; }

        public void Connect(int connectionId, object target)
        {
            throw new NotImplementedException();
        }

        private void CodeMarkGlyphMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var glyph = (CodeMarkGlyph) sender;
            UnitTestAdornmentManager manager = UnitTestAdornmentManager.Create(_view);

            //var provider  = Leem.Testify.UnitTestAdornment.UnitTestAdornmentProvider.Create(view);

            ITextSnapshotLine snapshotSpanLine = _view.TextSnapshot.GetLineFromLineNumber(glyph._coveredLine.LineNumber);
            var snapshotSpan = new SnapshotSpan(snapshotSpanLine.Start, snapshotSpanLine.End);
            var unitTestAdornment = new UnitTestAdornment.UnitTestAdornment(snapshotSpan, glyph._coveredLine,
                glyph.YPosition);

            _view.GetAdornmentLayer("PostAdornmentLayer").RemoveAllAdornments();
            if (unitTestAdornment.CoveredLine.IsCovered && unitTestAdornment.CoveredLine.TestMethods.Any())
            {
                manager.DisplayUnitTestSelector(unitTestAdornment, _context);
            }


           
        }


        private void CodeMarkGlyph_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            int x = 1;
            //    // call RemoveBookmark function of the manager on right mouse button down event
            //    _coverageManager.RemoveBookmark(BookmarkNumber);
        }


    }
}