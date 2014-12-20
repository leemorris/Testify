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
            CoverageViewModel coverageViewModel = new CoverageViewModel(modules);
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

            //IList<CodeElement> classes;
            //IList<CodeElement> methods;
            if (dte.ActiveDocument != null)
            {

                //CodeModelService.GetCodeBlocks(dte.ActiveDocument.ProjectItem.FileCodeModel, out classes, out methods);

                if (type == "Leem.Testify.MethodViewModel")
                {
                    clickedMethodName =((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FullName;

                    var method = queries.GetMethod(clickedMethodName);
                    filePath = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FileName;
                    line = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Line;
                    column = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Column;
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

    }
}