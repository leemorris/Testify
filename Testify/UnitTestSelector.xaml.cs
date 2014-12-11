using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
       
        }

        void PopupClicked(object sender, EventArgs e)
        {
            var x = 1;
            //    // call RemoveBookmark function of the manager on right mouse button down event
            //    _coverageManager.RemoveBookmark(BookmarkNumber);
        }
    }
}
