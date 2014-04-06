using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Leem.Testify
{
    /// <summary>
    /// The ViewModel for the LoadOnDemand demo.  This simply
    /// exposes a read-only collection of regions.
    /// </summary>
    public class SummaryViewModel : TreeViewItemViewModel
    {

        readonly ReadOnlyCollection<ModuleViewModel> _modules;

        public SummaryViewModel(Poco.CodeModule[] modules)
        {
            _modules = new ReadOnlyCollection<ModuleViewModel>(
                (from module in modules
                 select new ModuleViewModel(module))
                .ToList());
        }
        public ReadOnlyCollection<ModuleViewModel> Modules
        {
            get { return _modules; }
        }
    }
}