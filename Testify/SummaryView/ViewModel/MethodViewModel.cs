using System;
using System.Text;
using System.Threading;

namespace Leem.Testify.SummaryView.ViewModel
{
    public class MethodViewModel : TreeViewItemViewModel
    {
        private readonly Poco.CodeMethod _method;
        private readonly ClassViewModel parent;
        private bool _displaySequenceCoverage;
        private SynchronizationContext _uiContext;


        public MethodViewModel(Poco.CodeMethod method, ClassViewModel parentClass,SynchronizationContext uiContext)
            : base(parentClass, false)
        {
            parent = parentClass;
            _method = method;
            Type = "Method";
            this.ShouldShowSummary = true;
            parent.Parent.CoverageChanged += CoverageChanged;
            _uiContext = uiContext;
        }


        protected virtual new void CoverageChanged(object sender, CoverageChangedEventArgs e)
        {
            _displaySequenceCoverage = e.DisplaySequenceCoverage;
            _uiContext.Send(x => base.OnPropertyChanged("Coverage"), null);
        }

        public string Name
        {
            get
            {
                var methodName = _method.Name.ToString();
                string name = methodName.Substring(methodName.LastIndexOf(".") + 1);
                name = name.Replace("ctor(", parent.Name + "(")
                           .Replace("cctor(", parent.Name + "(");

                int startOfParameters = name.IndexOf("(");
                var arguments = name.Substring(startOfParameters + 1, name.IndexOf(")") - name.IndexOf("(") - 1);
                if (arguments.Length > 30)
                {
                    var truncatedArguments = arguments.Substring(0, 27) + "...";
                    name = name.Replace(arguments, truncatedArguments);
                }

                return name;
            }
        }

        public string FullName
        {
            get
            {
                return _method.Name;
            }
        }

        public string FileName
        {
            get { return _method.FileName; }
        }

        public int NumSequencePoints
        {
            get { return _method.Summary.NumSequencePoints; }
        }

        public int NumBranchPoints
        {
            get { return _method.Summary.NumBranchPoints; }
        }

        public decimal SequenceCoverage
        {
            get { return _method.Summary.SequenceCoverage; }
        }

        public decimal Coverage
        {
            get
            {
                if (_displaySequenceCoverage)
                {
                    return _method.Summary.SequenceCoverage;
                }
                else
                {
                    return _method.Summary.BranchCoverage;
                }
            }
        }

        public int VisitedBranchPoints
        {
            get { return _method.Summary.VisitedBranchPoints; }
        }

        public int VisitedSequencePoints
        {
            get { return _method.Summary.VisitedSequencePoints; }
        }

        public decimal BranchCoverage
        {
            get { return _method.Summary.BranchCoverage; }
        }

        public int Line
        {
            get { return _method.Line; }
        }

        public int Column
        {
            get { return _method.Column; }
        }

        public int Level { get { return 3; } }
    }
}