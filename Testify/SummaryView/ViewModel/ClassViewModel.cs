

namespace Leem.Testify
{
    public class ClassViewModel : TreeViewItemViewModel
    {
        readonly Poco.CodeClass _class;
        private ITestifyQueries queries;

        public ClassViewModel(Poco.CodeClass codeClass, ModuleViewModel parentModule)
            : base(parentModule, true)
        {
            _class = codeClass;
            queries = TestifyQueries.Instance;
        }

        public string ClassName
        {
            get { return _class.Name.Substring(_class.Name.LastIndexOf(".") + 1); }
        }
        public string ModuleName
        {
            get { return _class.Name; }
        }

        public int NumSequencePoints
        {
            get { return _class.Summary.NumSequencePoints; }
        }

        public int NumBranchPoints
        {
            get { return _class.Summary.NumBranchPoints; }
        }

        public decimal SequenceCoverage
        {
            get { return _class.Summary.SequenceCoverage; }
        }

        public int VisitedBranchPoints
        {
            get { return _class.Summary.VisitedBranchPoints; }
        }

        public int VisitedSequencePoints
        {
            get { return _class.Summary.VisitedSequencePoints; }
        }

        public decimal BranchCoverage
        {
            get { return _class.Summary.BranchCoverage; }
        }
        protected override void LoadChildren()
        {
            foreach (Poco.CodeMethod method in queries.GetMethods(_class))
                base.Children.Add(new MethodViewModel(method, this));
        }
    }
}