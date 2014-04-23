using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Leem.Testify
{
    public class ModuleViewModel //: TreeViewItemViewModel
    {
        readonly Poco.CodeModule _module;
        private ITestifyQueries queries;
        public ModuleViewModel()
        {
            _module = new Poco.CodeModule();
            _module.Summary = new Poco.Summary();
        }
        //public ModuleViewModel(Poco.CodeModule module) 
        //    : base(null, true)
        //{
        //    _module = module;
        //    queries = TestifyQueries.Instance;
        //}

        public string ModuleName
        {
            get { return _module.Name; }
           set { _module.Name = value; }
        }

        public int NumSequencePoints
        {
            get { return _module.Summary.NumSequencePoints; }
            set { _module.Summary.NumSequencePoints = value; }
        }

        public int NumBranchPoints
        {
            get { return _module.Summary.NumBranchPoints; }
            set { _module.Summary.NumBranchPoints = value; }
        }

        public decimal SequenceCoverage
        {
            get { return _module.Summary.SequenceCoverage; }
        }

        public int VisitedBranchPoints
        {
            get { return _module.Summary.VisitedBranchPoints; }
            set { _module.Summary.VisitedBranchPoints = value; }
        }

        public int VisitedSequencePoints
        {
            get { return _module.Summary.VisitedSequencePoints; }
            set { _module.Summary.VisitedBranchPoints = value; }
        }

        public decimal BranchCoverage
        {
            get { return _module.Summary.BranchCoverage; }
            set { _module.Summary.BranchCoverage = value; }
        }

        protected override void LoadChildren()
        {
            var codeClasses = queries.GetClasses(_module);
            foreach ( var codeClass in codeClasses )
                base.Children.Add(new ClassViewModel(codeClass, this));
        }
        //#region Level

        //// Returns the number of nodes in the longest path to a leaf

        //public int Depth
        //{
        //    get
        //    {
        //        int max;
        //        if (Items.Count == 0)
        //            max = 0;
        //        else
        //            max = (int)Items.Max(r => r.Depth);
        //        return max + 1;
        //    }
        //}

        //private DirectoryRecord parent;

        //// Returns the maximum depth of all siblings

        //public int Level
        //{
        //    get
        //    {
        //        if (parent == null)
        //            return Depth;
        //        return parent.Level - 1;
        //    }
        //}

        //#endregion
    }
}