using EnvDTE;
using EnvDTE80;
using Leem.Testify.SummaryView.ViewModel;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Leem.Testify.SummaryView
{
    public partial class SummaryViewControl : UserControl
    {
        private ITestifyQueries _queries;
        private TestifyCoverageWindow _parent;
        private bool wasCalledForMethod;
        //private readonly ILog Log = LogManager.GetLogger(typeof(SummaryViewControl));
        private TestifyContext _context;

        public SummaryViewControl(TestifyCoverageWindow parent)
        {
            InitializeComponent();
            _parent = parent;
            _queries = TestifyQueries.Instance;


            if (TestifyQueries.SolutionName != null)
            {
                _context = new TestifyContext(TestifyQueries.SolutionName);
                _queries.ClassChanged += ClassChanged;
                //using (var context = new TestifyContext(TestifyQueries.SolutionName))
                //{
                      //Todo make this async
                BuildCoverageViewModel();
                //}
              
            }
            else
            {
                base.DataContext = new SummaryViewModel(_context);
            }
        }

        private void BuildCoverageViewModel()
        {
            var coverageViewModel = GetSummaries(_context);
  
            
            if (coverageViewModel.Modules.Count > 0)
            {
                //treeGrid.DataContext = null;
                this.Dispatcher.Invoke((Action)(() =>
                {
                    base.DataContext = coverageViewModel;
                    treeGrid.DataContext = coverageViewModel;
                }));
            }
            else
            {
                base.Content = "Waiting for Solution to be Built";
            }
        }

        private CoverageViewModel GetSummaries(TestifyContext context)
        {
            Poco.CodeModule[] modules = _queries.GetModules(context);
            var coverageViewModel = new CoverageViewModel(modules,context);

            return coverageViewModel;
        }

        protected virtual void ClassChanged(object sender, ClassChangedEventArgs e)
        {
            _context = new TestifyContext(TestifyQueries.SolutionName);
            if(e.ChangedClasses.Count>0)
            {
                 BuildCoverageViewModel();

            }
           
        }

        private void ItemDoubleClicked(object sender, RoutedEventArgs e)
        {
            // This event is raised multiple times, if the user double -clicks on a Method, this is fired for Method, Class and Module
            // if the user double-clicks on a Class, this is fired for the Class and Module
            _queries = TestifyQueries.Instance;
            string filePath = string.Empty;
            int line = 0;
            int column = 0;
            string type = ((HeaderedItemsControl)(e.Source)).Header.ToString();
            string name = string.Empty;
            EnvDTE.Window openDocumentWindow;
            var clickedMethodName = string.Empty;

            var dte = TestifyPackage.GetGlobalService(typeof(DTE)) as DTE2;

            if (dte.ActiveDocument != null)
            {
                if (type == "Leem.Testify.SummaryView.ViewModel.MethodViewModel")
                {
                    wasCalledForMethod = true; // set flag so we know this event fired for a Method
                    clickedMethodName = ((MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FullName;

                    filePath = ((MethodViewModel)(((HeaderedItemsControl)(e.Source)).Header)).FileName;
                    if (filePath == null)
                    {
                        filePath = (((ClassViewModel)(((Leem.Testify.SummaryView.ViewModel.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)((e.Source))).Header)).Parent))._class).FileName;
                    }
                    line = ((MethodViewModel)(((HeaderedItemsControl)(e.Source)).Header)).Line;
                    line = line > 1 ? line-- : 1;
                    column = ((MethodViewModel)(((HeaderedItemsControl)(e.Source)).Header)).Column;
                    column = column > 1 ? column-- : 1;
                }
                if (type == "Leem.Testify.SummaryView.ViewModel.ClassViewModel" && wasCalledForMethod == false)
                {
                    // If event wasn't fired for a Method, then we can navigate to the class
                    clickedMethodName = ((ClassViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Name;

                    filePath = ((ClassViewModel)(((HeaderedItemsControl)(e.Source)).Header)).FileName;
                    line = ((ClassViewModel)(((HeaderedItemsControl)(e.Source)).Header)).Line - 1;
                    column = 1;
                }
                if (type == "Leem.Testify.SummaryView.ViewModel.ModuleViewModel")
                {
                    // This event is fired for the Module as the last step. Re-set the Method flag and do nothing else.
                    wasCalledForMethod = false;
                }
            }

            if (!string.IsNullOrEmpty(filePath) && filePath != string.Empty && !dte.ItemOperations.IsFileOpen(filePath))
            {
                openDocumentWindow = dte.ItemOperations.OpenFile(filePath);
                if (openDocumentWindow != null)
                {
                    openDocumentWindow.Activate();
                }
                else
                {
                    for (var i = 1; i == dte.Documents.Count; i++)
                    {
                        if (dte.Documents.Item(i).Name == name)
                        {
                            openDocumentWindow = dte.Documents.Item(i).ProjectItem.Document.ActiveWindow;
                        }
                    }
                }
            }
            else
            {
                for (var i = 1; i <= dte.Windows.Count; i++)
                {
                    var window = dte.Windows.Item(i);
                    if (window.Document != null && window.Document.FullName.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        openDocumentWindow = window;
                        openDocumentWindow.Activate();
                        var selection = window.Document.DTE.ActiveDocument.Selection as TextSelection;
                        selection.StartOfDocument();
                        selection.MoveToLineAndOffset(line, column, true);

                        selection.SelectLine();

                        continue;
                    }
                }
            }
        }

        public void Connect(int connectionId, object target)
        {
            throw new NotImplementedException();
        }
    }
}