using System.Collections.ObjectModel;
using System.Linq;

namespace Leem.Testify.SummaryView.ViewModel
{

    /// <summary>
    /// The ViewModel for the LoadOnDemand demo.  This simply
    /// exposes a read-only collection of regions.
    /// </summary>
    public class SummaryViewModel : TreeViewItemViewModel
    {
        private ITestifyQueries queries;
        public SummaryViewModel()
        {
            var module = new Poco.CodeModule
            {
                Name = "default Summary--Yeah",
                CodeModuleId = 1,
                Summary = new Poco.Summary()
            };

            var moduleArray = new Poco.CodeModule[]{module};
            Items = new ObservableCollection<SummaryViewModel>();
            Items.Add(new SummaryViewModel(moduleArray));
        }
        readonly ReadOnlyCollection<ModuleViewModel> _modules;  
        public ReadOnlyCollection<ModuleViewModel> Modules
        {
            get { return _modules; }
        }

        ObservableCollection<SummaryViewModel> _items;

        public SummaryViewModel(Poco.CodeModule[] modules)
        {
            var numSequencePoints = modules.Sum(x => x.Summary.NumSequencePoints);
            var numBranchPoints = modules.Sum(x => x.Summary.NumBranchPoints);
            var visitedSequencePoints  = modules.Sum(x => x.Summary.VisitedSequencePoints);
            var visitedBranchPoints = modules.Sum(x => x.Summary.VisitedBranchPoints);

            Summary = new SummaryInfo { Name="Look at me",
                                        NumBranchPoints=numBranchPoints,
                                        NumSequencePoints = numSequencePoints,
                                        VisitedBranchPoints = visitedBranchPoints,
                                        VisitedSequencePoints=visitedSequencePoints};
            if (visitedSequencePoints > 0 && visitedBranchPoints > 0)
            {
                decimal branchCoverage = numBranchPoints / visitedBranchPoints;
                decimal sequenceCoverage = numSequencePoints / visitedSequencePoints;
                Summary.BranchCoverage = branchCoverage;
                Summary.SequenceCoverage = sequenceCoverage;
            }
            _items = new ObservableCollection<SummaryViewModel>(
                (from module in modules
                 select new SummaryViewModel(module))
                .ToList());
            _modules = new ReadOnlyCollection<ModuleViewModel>(
                (from module in modules
                 select new ModuleViewModel(module))
                .ToList());

        }

        public SummaryViewModel(Poco.CodeModule module)
        {
           Summary = new SummaryInfo(module.Summary, module.Name);
           _items = new ObservableCollection<SummaryViewModel>(
                (from clas in module.Classes
                 select new SummaryViewModel(clas))
                .ToList());

        }

        public SummaryViewModel(Poco.CodeClass clas)
        {
            
            var className =  clas.Name.Substring(clas.Name.LastIndexOf(".") + 1);
            Summary = new SummaryInfo(clas.Summary, className);
            _items = new ObservableCollection<SummaryViewModel>(
                (from method in clas.Methods
                 select new SummaryViewModel(method))
                .ToList());

        }
        public SummaryViewModel(Poco.CodeMethod method)
        {
            var methodName = method.Name.ToString();
            var start = methodName.IndexOf("::") + 2;
            var end = methodName.IndexOf("(") - start;
            Summary = new SummaryInfo(method.Summary, methodName.Substring(start, end));
        }
        public ObservableCollection<SummaryViewModel> Items 
        {
            get { return _items; }
            set { _items = value; }
        }

        public SummaryInfo Summary { get; set; }
        #region Level

        // Returns the number of nodes in the longest path to a leaf

        public int Depth
        {
            get
            {
                int max;
                if (Items == null || Items.Count == 0)
                    max = 0;
                else
                    max = (int)Items.Max(r => r.Depth);
                return max + 1;
            }
        }

        private SummaryViewModel parent;

        // Returns the maximum depth of all siblings

        public int Level
        {
            get
            {
                if (parent == null)
                    return Depth;
                return parent.Level - 1;
            }
        }

        #endregion
    }
}