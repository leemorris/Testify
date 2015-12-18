namespace Leem.Testify.SummaryView.ViewModel
{
    public class ModuleViewModel : TreeViewItemViewModel
    {
        private readonly Poco.CodeModule _module;
        private readonly ITestifyQueries _queries;
        private TestifyContext _context;

        public ModuleViewModel()
        {
            _module = new Poco.CodeModule { Summary = new Poco.Summary() };
        }

        public ModuleViewModel(Poco.CodeModule module, TestifyContext context)
            : base(null, true)
        {
            _module = module;
            _queries = TestifyQueries.Instance;
            _context = context;
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

        public string FileName
        {
            get { return _module.FileName; }
        }

        protected override void LoadChildren()
        {
            var codeClasses = _queries.GetClasses(_module, _context);
            foreach (var codeClass in codeClasses)
                base.Children.Add(new ClassViewModel(codeClass, this, _context));
        }

        public int Level { get { return 3; } }
    }
}