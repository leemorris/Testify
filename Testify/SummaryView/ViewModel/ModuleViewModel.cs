using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Leem.Testify
{
    public class ModuleViewModel : TreeViewItemViewModel
    {
        readonly Poco.CodeModule _module;
        private ITestifyQueries queries;
        public ModuleViewModel()
        {
            _module = new Poco.CodeModule();
            _module.Summary = new Poco.Summary();
        }
        public ModuleViewModel(Poco.CodeModule module)
            : base(null, true)
        {
            _module = module;
            queries = TestifyQueries.Instance;
        }

        public string Name
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

        public int Level { get { return 3; } }
        
    }
}