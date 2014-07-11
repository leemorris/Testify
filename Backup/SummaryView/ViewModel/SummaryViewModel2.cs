using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Leem.Testify.SummaryView.ViewModel;
using System.Collections.Generic;

namespace Leem.Testify
{

    /// <summary>
    /// The ViewModel for the LoadOnDemand demo.  This simply
    /// exposes a read-only collection of regions.
    /// </summary>
    public class SummaryViewModel2 //: TreeViewItemViewModel
    {
        private ITestifyQueries queries;
        public SummaryViewModel2()
        {
            var module = new Poco.CodeModule
            {
                Name = "default Summary--Yeah",
                CodeModuleId = 1,
                Summary = new Poco.Summary()
            };

            var moduleArray = new Poco.CodeModule[]{module};
            Items = new ObservableCollection<SummaryViewModel2>();
            Items.Add(new SummaryViewModel2(moduleArray));
        }
        //public SummaryViewModel2()
        //{
        //    queries = TestifyQueries.Instance;
        //    Poco.CodeModule[] modules = queries.GetSummaries(); //null;// = Database.GetRegions();
        //    SummaryViewModel viewModel = new SummaryViewModel(modules);
        //    Summary = new SummaryInfo { Name = "Look at me" };
        //    _items = new ObservableCollection<SummaryViewModel2>(
        //        (from module in modules
        //         select new SummaryViewModel2(module))
        //        .ToList());
        //}

        ObservableCollection<SummaryViewModel2> _items;

        public SummaryViewModel2(Poco.CodeModule[] modules)
        {
            var numSequencePoints = modules.Sum(x => x.Summary.NumSequencePoints);
            var numBranchPoints = modules.Sum(x => x.Summary.NumBranchPoints);
            var visitedSequencePoints  = modules.Sum(x => x.Summary.VisitedSequencePoints);
            var visitedBranchPoints = modules.Sum(x => x.Summary.VisitedBranchPoints);
            decimal branchCoverage = numBranchPoints / visitedBranchPoints;
            decimal sequenceCoverage = numSequencePoints / visitedSequencePoints;
            Summary = new SummaryInfo { Name="Look at me",
                                        NumBranchPoints=numBranchPoints,
                                        NumSequencePoints = numSequencePoints,
                                        VisitedBranchPoints = visitedBranchPoints,
                                        VisitedSequencePoints=visitedSequencePoints};
            if (visitedSequencePoints > 0 && visitedBranchPoints > 0)
            {
                Summary.BranchCoverage = branchCoverage;
                Summary.SequenceCoverage = sequenceCoverage;
            }
            _items = new ObservableCollection<SummaryViewModel2>(
                (from module in modules
                 select new SummaryViewModel2(module))
                .ToList());

        }

        public SummaryViewModel2(Poco.CodeModule module)
        {
           Summary = new SummaryInfo(module.Summary, module.Name);
           _items = new ObservableCollection<SummaryViewModel2>(
                (from clas in module.Classes
                 select new SummaryViewModel2(clas))
                .ToList());

        }

        public SummaryViewModel2(Poco.CodeClass clas)
        {
            
            var className =  clas.Name.Substring(clas.Name.LastIndexOf(".") + 1);
            Summary = new SummaryInfo(clas.Summary, className);
            _items = new ObservableCollection<SummaryViewModel2>(
                (from method in clas.Methods
                 select new SummaryViewModel2(method))
                .ToList());

        }
        public SummaryViewModel2(Poco.CodeMethod method)
        {
            var start = method.Name.IndexOf("::") + 2;
            var end = method.Name.IndexOf("(") - start;
            Summary = new SummaryInfo(method.Summary, method.Name.Substring(start, end));
        }
        public ObservableCollection<SummaryViewModel2> Items 
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

        private SummaryViewModel2 parent;

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