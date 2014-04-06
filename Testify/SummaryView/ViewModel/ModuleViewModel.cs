using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Leem.Testify
{
    public class ModuleViewModel : TreeViewItemViewModel
    {
        readonly Poco.CodeModule _module;
        private ITestifyQueries queries;

        public ModuleViewModel(Poco.CodeModule module) 
            : base(null, true)
        {
            _module = module;
            queries = TestifyQueries.Instance;
        }

        public string ModuleName
        {
            get { return _module.Name; }
        }

        public int NumSequencePoints
        {
            get { return _module.Summary.NumSequencePoints; }
        }

        public int NumBranchPoints
        {
            get { return _module.Summary.NumBranchPoints; }
        }

        public decimal SequenceCoverage
        {
            get { return _module.Summary.SequenceCoverage; }
        }

        public int VisitedBranchPoints
        {
            get { return _module.Summary.VisitedBranchPoints; }
        }

        public int VisitedSequencePoints
        {
            get { return _module.Summary.VisitedSequencePoints; }
        }

        public decimal BranchCoverage
        {
            get { return _module.Summary.BranchCoverage; }
        }

        protected override void LoadChildren()
        {
            var codeClasses = queries.GetClasses(_module);
            foreach ( var codeClass in codeClasses )
                base.Children.Add(new ClassViewModel(codeClass, this));
        }
    }
}