using System.Collections.ObjectModel;
using System.Linq;

namespace Leem.Testify.SummaryView.ViewModel
{
    public class CoverageViewModel : TreeViewItemViewModel
    {
        private readonly ObservableCollection<ModuleViewModel> _modules;

        public CoverageViewModel(Leem.Testify.Poco.CodeModule[] modules,TestifyContext context)
        {
            _modules = new ObservableCollection<ModuleViewModel>(
                (from module in modules
                 select new ModuleViewModel(module,context))
                .ToList());
        }

        // The Name property is needed by the SummaryViewControl.XAML, do not remove.
        public string Name { get; set; }

        public int Level { get; set; }

        public ObservableCollection<ModuleViewModel> Modules
        {
            get { return _modules; }
        }
    }
}