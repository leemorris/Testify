using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace Leem.Testify.SummaryView.ViewModel
{
    public class CoverageViewModel : TreeViewItemViewModel
    {
        private readonly ObservableCollection<ModuleViewModel> _modules;

        public CoverageViewModel(Leem.Testify.Poco.CodeModule[] modules, TestifyContext context, SynchronizationContext uiContext, Dictionary<string, Bitmap> iconCache)
        {
            _modules = new ObservableCollection<ModuleViewModel>(
                (from module in modules
                 select new ModuleViewModel(module, context, uiContext, iconCache))
                .ToList());
        }

        // The Name property is needed by the SummaryViewControl.XAML, do not remove.
        public string Name { get; set; }

        public int Level { get; set; }

        public ObservableCollection<ModuleViewModel> Modules
        {
            get { return _modules; }
        }




        public System.Windows.Media.SolidColorBrush BackgroundBrush { get; set; }
    }
}