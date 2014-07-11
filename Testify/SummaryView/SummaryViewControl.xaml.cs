using System.Windows.Controls;
using Leem.Testify;
using System.Windows;
using System;
using log4net;
using EnvDTE;
using EnvDTE80;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Clide;
using Clide.Solution;

namespace Leem.Testify
{
    public partial class SummaryViewControl : UserControl
    {
        private ITestifyQueries queries;
        public TestifyCoverageWindow _parent;
        private ILog Log = LogManager.GetLogger(typeof(SummaryViewControl));

        public SummaryViewControl(TestifyCoverageWindow parent)
        {
            InitializeComponent();
            _parent = parent;
            queries = TestifyQueries.Instance;



            if (TestifyQueries.SolutionName != null)
            {
                ///Todo make this async
                Task<CoverageViewModel> coverageViewModel = GetSummariesAsync();
                //CoverageViewModel coverageViewModel = GetSummariesAsync();
                //coverageViewModel.Start();
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

            Poco.CodeModule[] modules =  queries.GetModules();
            //SummaryViewModel viewModel = new SummaryViewModel(modules);
            CoverageViewModel coverageViewModel = new CoverageViewModel(modules);
            // var model = new SummaryViewModel[] { viewModel };
            return coverageViewModel;
        }
        void itemDoubleClicked(object sender, RoutedEventArgs e)
        {
            queries = TestifyQueries.Instance;
            string filePath = string.Empty;
            int line = 0;
            int column = 0;
            string type = ((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header.ToString();
            string name = string.Empty;
            EnvDTE.Window openDocumentWindow = null;
            string clickedMethodName = string.Empty;

            DTE2 dte = TestifyPackage.GetGlobalService(typeof(DTE)) as DTE2;
            //var x = dte.ActiveDocument.ProjectItem.FileCodeModel;
            IList<CodeElement> classes;
            IList<CodeElement> methods;
            if (dte.ActiveDocument != null)
            {

                CodeModelService.GetCodeBlocks(dte.ActiveDocument.ProjectItem.FileCodeModel, out classes, out methods);
                //var d = dte.Solution.Projects.Find(((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Name).

                if (type == "Leem.Testify.ClassViewModel")
                {
                    filePath = ((Leem.Testify.ClassViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FileName;
                    line = ((Leem.Testify.ClassViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Line;
                    column = ((Leem.Testify.ClassViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Column;
                    //filePath = dte.ActiveDocument.FullName;
                    //line = classes[0].StartPoint.Line;
                    //column = classes[0].StartPoint.LineCharOffset;
                }
                else if (type == "Leem.Testify.MethodViewModel")
                {
                    clickedMethodName =((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FullName;

                    var method = queries.GetMethod(clickedMethodName);
                    filePath = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FileName;
                    line = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Line;
                    column = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Column;
                }
            }


           // var xx = ((Clide.Solution.ItemNode)((new System.Collections.Generic.Mscorlib_CollectionDebugView<Clide.ITreeNode>(projects2)).Items[0])).Item;
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
            {// do something }
                for (var i = 1; i <= dte.Windows.Count; i++)
                {
                    var window = dte.Windows.Item(i);
                    if (window.Document != null && window.Document.FullName.Equals(filePath,StringComparison.OrdinalIgnoreCase))
                    {
                        openDocumentWindow = window;
                        openDocumentWindow.Activate();
                        var selection = window.Document.DTE.ActiveDocument.Selection as TextSelection;
                        selection.MoveToLineAndOffset(line, column);
      
  
                        continue;
                    }
                }
            }

        }

    }
}