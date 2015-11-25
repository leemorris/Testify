using System;
using System.Windows.Controls;

namespace Leem.Testify
{
    /// <summary>
    /// Interaction logic for UnitTestSelector.xaml
    /// </summary>
    public partial class UnitTestSelector : UserControl
    {
        public UnitTestSelector(UnitTestSelectorWindow parent)
        {
            InitializeComponent();
            parent.Content = "dfdsa";
        }

        void PopupClicked(object sender, EventArgs e)
        {
            var x = 1;
            //    // call RemoveBookmark function of the manager on right mouse button down event
            //    _coverageManager.RemoveBookmark(BookmarkNumber);
        }

        public void Connect(int connectionId, object target)
        {
            throw new NotImplementedException();
        }
    }
}
