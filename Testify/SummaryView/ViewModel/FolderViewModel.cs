using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

namespace Leem.Testify.SummaryView.ViewModel
{
    public class FolderViewModel : TreeViewItemViewModel
    {
        internal Poco.Folder _folder;
        private readonly ITestifyQueries _queries;
        private TestifyContext _context;
        private SynchronizationContext _uiContext;
        private ModuleViewModel _parent;
        private FolderViewModel _folderParent;
        private Dictionary<string, Bitmap> _iconCache;

        // constructor when created by a Module
        public FolderViewModel(Poco.Folder folder, ModuleViewModel parentModule, TestifyContext context, SynchronizationContext uiContext, Dictionary<string, Bitmap> iconCache)
            : base(parentModule, (true))
        {
            _folder = folder;
            _queries = TestifyQueries.Instance;
            _context = context;
            _uiContext = uiContext;
            _parent = parentModule;
            _iconCache = iconCache;
            Bitmap tempIcon;
            _iconCache.TryGetValue("Folder", out tempIcon);
            Icon = ConvertBitmapToBitmapImage.Convert(tempIcon);
        }

        // constructor when created by a Folder
        public FolderViewModel(Poco.Folder folder, FolderViewModel parentFolder, TestifyContext context, SynchronizationContext uiContext, Dictionary<string, Bitmap> iconCache)
            : base(parentFolder, (folder.Classes.Count > 0)  || folder.Descendants.Count > 0)
        {
            _folder = folder;
            _queries = TestifyQueries.Instance;
            _context = context;
            _uiContext = uiContext;
            _folderParent = parentFolder;
            _iconCache = iconCache;
            Bitmap tempIcon;
            _iconCache.TryGetValue("Folder", out tempIcon);
            Icon = ConvertBitmapToBitmapImage.Convert(tempIcon);
        }

        public string Name
        {
            get { return _folder.FolderName; }
        }

        //public int NumSequencePoints
        //{
        //    get { return _folder.Summary.NumSequencePoints; }
        //}

        //public int NumBranchPoints
        //{
        //    get { return _folder.Summary.NumBranchPoints; }
        //}

        //public decimal SequenceCoverage
        //{
        //    get { return _folder.Summary.SequenceCoverage; }
        //}

        //public int VisitedBranchPoints
        //{
        //    get { return _folder.Summary.VisitedBranchPoints; }
        //}

        //public int VisitedSequencePoints
        //{
        //    get { return _folder.Summary.VisitedSequencePoints; }
        //}

        //public decimal BranchCoverage
        //{
        //    get { return _folder.Summary.BranchCoverage; }
        //}




        protected override void LoadChildren()
        {
            foreach (Poco.CodeClass codeClass in _queries.GetClasses(_folder, _context))
            {
                base.Children.Add(new ClassViewModel(codeClass, this, _context, _uiContext, _iconCache));
            }
            foreach (Poco.Folder folder in _queries.GetFolders(_folder, _context))
            {
                base.Children.Add(new FolderViewModel(folder, this, _context, _uiContext, _iconCache));
            }

        }

        public int Level { get { return 1; } }

        //protected virtual void ClassChanged(object sender, ClassChangedEventArgs e)
        //{
        //    foreach (var entity in _context.ChangeTracker.Entries())
        //    {
        //        entity.Reload();
        //    }
        //    foreach (var clas in e.ChangedClasses)
        //    {
        //        if (clas.EndsWith(this.Name))
        //        {
        //            if(base.Children.Count>0)
        //            {
        //                _uiContext.Send(x => base.Children.Clear(), null); 
        //            }
        //            _uiContext.Send(x => LoadChildren(), null);
        //            _class = _context.CodeClass.FirstOrDefault(x => x.Name.EndsWith(this.Name));
        //            _uiContext.Send(x => base.OnPropertyChanged("SequenceCoverage"), null);
        //            _uiContext.Send(x => base.OnPropertyChanged("BranchCoverage"), null);
        //            _parent.UpdateCoverage();
                   
                 
        //        }
        //    }
            


        //}
    }
}