using System.Collections.ObjectModel;
using System.Linq;

namespace Leem.Testify.SummaryView.ViewModel
{
    public class CoverageViewModel
    {
        readonly ReadOnlyCollection<ModuleViewModel> _modules;

        public CoverageViewModel(Leem.Testify.Poco.CodeModule[] modules)
        {
            _modules = new ReadOnlyCollection<ModuleViewModel>(
                (from module in modules
                 select new ModuleViewModel(module))
                .ToList());
        }
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }
        public int Level { get; set; }
        public ReadOnlyCollection<ModuleViewModel> Modules
        {
            get { return _modules; }
        }
    }
}
