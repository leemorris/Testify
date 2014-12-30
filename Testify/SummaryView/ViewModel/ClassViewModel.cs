

namespace Leem.Testify.SummaryView.ViewModel
{
    public class ClassViewModel : TreeViewItemViewModel
    {
        readonly Poco.CodeClass _class;
        private readonly ITestifyQueries _queries;

        public ClassViewModel(Poco.CodeClass codeClass, ModuleViewModel parentModule)
            : base(parentModule, true)
        {
            _class = codeClass;
            _queries = TestifyQueries.Instance;
        }

        public string Name
        {
            get { return _class.Name.Substring(_class.Name.LastIndexOf(".") + 1); }
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

        public string FileName
        {
            get { return _class.FileName; }
        }

        public int Line
        {
            get { return _class.Line; }
        }

        public int Column
        {
            get { return _class.Column; }
        }

        protected override void LoadChildren()
        {
            foreach (Poco.CodeMethod method in _queries.GetMethods(_class))
                base.Children.Add(new MethodViewModel(method, this));
        }

        public int Level { get { return 2; } }
       
    }
}