using EnvDTE;
using EnvDTE80;
using Leem.Testify.SummaryView.ViewModel;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Leem.Testify.SummaryView
{
    public partial class SummaryViewControl : UserControl
    {
        private ITestifyQueries _queries;
        private TestifyCoverageWindow _parent;
        //private readonly ILog Log = LogManager.GetLogger(typeof(SummaryViewControl));

        public SummaryViewControl(TestifyCoverageWindow parent)
        {
            InitializeComponent();
            _parent = parent;
            _queries = TestifyQueries.Instance;



            if (TestifyQueries.SolutionName != null)
            {
                //Todo make this async
                Task<CoverageViewModel> coverageViewModel = GetSummariesAsync();
                coverageViewModel.Wait();
                base.DataContext =  coverageViewModel.Result;

                treeGrid.DataContext = coverageViewModel.Result;

            }
            else
            {
                base.DataContext = new SummaryViewModel();
            }
           
        }

        private  async Task<CoverageViewModel> GetSummariesAsync()
        {
            Poco.CodeModule[] modules =  _queries.GetModules();
            var coverageViewModel = new CoverageViewModel(modules);
            return coverageViewModel;
        }

        void ItemDoubleClicked(object sender, RoutedEventArgs e)
        {
            _queries = TestifyQueries.Instance;
            string filePath = string.Empty;
            int line = 0;
            int column = 0;
            string type = ((HeaderedItemsControl)(e.Source)).Header.ToString();
            string name = string.Empty;
            EnvDTE.Window openDocumentWindow;
            var clickedMethodName = string.Empty;

            var dte = TestifyPackage.GetGlobalService(typeof(DTE)) as DTE2;

            //IList<CodeElement> classes;
            //IList<CodeElement> methods;
            if (dte.ActiveDocument != null)
            {

                //CodeModelService.GetCodeBlocks(dte.ActiveDocument.ProjectItem.FileCodeModel, out classes, out methods);

                if (type == "Leem.Testify.MethodViewModel")
                {
                    clickedMethodName =((MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FullName;

                   // var method = _queries.GetMethod(clickedMethodName);
                    filePath = ((MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FileName;
                    line = ((MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Line;
                    column = ((MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Column;
                }
            }


            if (!string.IsNullOrEmpty(filePath)  && filePath != string.Empty && !dte.ItemOperations.IsFileOpen(filePath))
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
                    if (window.Document != null && window.Document.FullName.Equals(filePath,StringComparison.OrdinalIgnoreCase))
                    {
                        openDocumentWindow = window;
                        openDocumentWindow.Activate();
                        var selection = window.Document.DTE.ActiveDocument.Selection as TextSelection;
                        selection.StartOfDocument();
                        selection.MoveToLineAndOffset(line, column,true);
                       
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