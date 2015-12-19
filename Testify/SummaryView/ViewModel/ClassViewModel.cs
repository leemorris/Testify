using System;
using System.Threading;
using System.Linq;

namespace Leem.Testify.SummaryView.ViewModel
{
    public class ClassViewModel : TreeViewItemViewModel
    {
        internal Poco.CodeClass _class;
        private readonly ITestifyQueries _queries;
        private TestifyContext _context;
        private SynchronizationContext _uiContext;
        private ModuleViewModel _parent;
        public ClassViewModel(Poco.CodeClass codeClass, ModuleViewModel parentModule, TestifyContext context, SynchronizationContext uiContext)
            : base(parentModule, (codeClass.Methods.Count > 0))
        {
            _class = codeClass;
            _queries = TestifyQueries.Instance;
            _context = context;
            _queries.ClassChanged += ClassChanged;
            _uiContext = uiContext;
            _parent = parentModule;
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
            foreach (Poco.CodeMethod method in _queries.GetMethods(_class, _context))
                base.Children.Add(new MethodViewModel(method, this));
        }

        public int Level { get { return 2; } }

        protected virtual void ClassChanged(object sender, ClassChangedEventArgs e)
        {
            foreach (var entity in _context.ChangeTracker.Entries())
            {
                entity.Reload();
            }
            foreach (var clas in e.ChangedClasses)
            {
                if (clas.EndsWith(this.Name))
                {
                    if(base.Children.Count>0)
                    {
                        _uiContext.Send(x => base.Children.Clear(), null); 
                    }
                    _uiContext.Send(x => LoadChildren(), null);
                    _class = _context.CodeClass.FirstOrDefault(x => x.Name.EndsWith(this.Name));
                    _uiContext.Send(x => base.OnPropertyChanged("SequenceCoverage"), null);
                    _uiContext.Send(x => base.OnPropertyChanged("BranchCoverage"), null);
                    _parent.UpdateCoverage();
                   
                 
                }
            }
            


        }
    }
}