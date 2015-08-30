using System.Collections.ObjectModel;
using System.Linq;

namespace Leem.Testify.SummaryView.ViewModel
{
    public class CoverageViewModel :TreeViewItemViewModel
    {
        readonly ReadOnlyCollection<ModuleViewModel> _modules;

        public CoverageViewModel(Leem.Testify.Poco.CodeModule[] modules)
        {
            _modules = new ReadOnlyCollection<ModuleViewModel>(
                (from module in modules
                 select new ModuleViewModel(module))
                .ToList());
        }
        // The Name property is needed by the SummaryViewControl.XAML, do not remove.
        public string Name { get; set; }


        public int Level { get; set; }
        public ReadOnlyCollection<ModuleViewModel> Modules
        {
            get { return _modules; }
        }
    }
}
