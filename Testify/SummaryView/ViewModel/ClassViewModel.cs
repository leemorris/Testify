using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

namespace Leem.Testify.SummaryView.ViewModel
{
    public class ClassViewModel : TreeViewItemViewModel
    {
        internal Poco.CodeClass _class;
        private readonly ITestifyQueries _queries;
        private TestifyContext _context;
        private SynchronizationContext _uiContext;
        private ModuleViewModel _moduleParent;
        private FolderViewModel _folderParent;
        private Dictionary<string, Bitmap> _iconCache;
        private bool _displaySequenceCoverage;

        // constructor when created by a Module
        public ClassViewModel(Poco.CodeClass codeClass, ModuleViewModel parentModule, TestifyContext context, SynchronizationContext uiContext, Dictionary<string, Bitmap> iconCache)
            : base(parentModule, (codeClass.Methods.Count > 0))
        {
            _class = codeClass;
            _queries = TestifyQueries.Instance;
            _context = context;
            _queries.ClassChanged += ClassChanged;
            _uiContext = uiContext;
            _iconCache = iconCache;
            Bitmap tempIcon;
            _iconCache.TryGetValue("C#File", out tempIcon);
            Icon = ConvertBitmapToBitmapImage.Convert(tempIcon);
            _moduleParent = parentModule;
            this.ShouldShowSummary = true;
            parentModule.CoverageChanged += CoverageChanged;
        }
        // constructor when created by a Folder
        public ClassViewModel(Poco.CodeClass codeClass, FolderViewModel parentFolder, TestifyContext context, SynchronizationContext uiContext, Dictionary<string, Bitmap> iconCache)
            : base(parentFolder, (codeClass.Methods.Count > 0))
        {
            _class = codeClass;
            _queries = TestifyQueries.Instance;
            _context = context;
            _queries.ClassChanged += ClassChanged;
            _uiContext = uiContext;
            _iconCache = iconCache;
            Bitmap tempIcon;
            _iconCache.TryGetValue("C#File", out tempIcon);
            Icon = ConvertBitmapToBitmapImage.Convert(tempIcon);
            _folderParent = parentFolder;
            this.ShouldShowSummary = true;
            parentFolder.CoverageChanged += CoverageChanged;
        }
        protected virtual new void CoverageChanged(object sender, CoverageChangedEventArgs e)
        {
            _displaySequenceCoverage = e.DisplaySequenceCoverage;
            _uiContext.Send(x => base.OnPropertyChanged("Coverage"), null);
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

        public decimal Coverage
        {
            get
            {
                if (_displaySequenceCoverage)
                {

                    return _class.Summary.SequenceCoverage;
                }
                else
                {

                    return _class.Summary.BranchCoverage;
                }
            }
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
            base.Children.Clear();
            foreach (Poco.CodeMethod method in _queries.GetMethods(_class, _context))
                base.Children.Add(new MethodViewModel(method, this,_uiContext));
        }

        public int Level { get { return 1; } }

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
                    //_uiContext.Send(x => base.OnPropertyChanged("SequenceCoverage"), null);
                    //_uiContext.Send(x => base.OnPropertyChanged("BranchCoverage"), null);
                    _uiContext.Send(x => base.OnPropertyChanged("Coverage"), null);
                    _moduleParent.UpdateCoverage();
                   
                 
                }
            }
            


        }
    }
}