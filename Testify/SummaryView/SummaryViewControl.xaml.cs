using System.Windows.Controls;
using Leem.Testify;
using System.Windows;
using System;
using log4net;
using EnvDTE;
using EnvDTE80;

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
                Poco.CodeModule[] modules = queries.GetSummaries();
                SummaryViewModel viewModel = new SummaryViewModel(modules);
                CoverageViewModel coverageViewModel = new CoverageViewModel(modules);
               // var model = new SummaryViewModel[] { viewModel };
                base.DataContext = coverageViewModel;

                treeGrid.DataContext = coverageViewModel;

            }
            else
            {
                base.DataContext = new SummaryViewModel();
            }
           
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

            if (type == "Leem.Testify.ClassViewModel")
            {
                filePath = ((Leem.Testify.ClassViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FileName;
                line = ((Leem.Testify.ClassViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Line;
                column = ((Leem.Testify.ClassViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Column;
            }
            else if (type == "Leem.Testify.MethodViewModel")
            {
                filePath = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FileName;
                line = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Line;
                column = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Column;
            }

            DTE2 dte = TestifyPackage.GetGlobalService(typeof(DTE)) as DTE2;

            if (filePath != string.Empty && !dte.ItemOperations.IsFileOpen(filePath))
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
                    if (window.Document != null && window.Document.FullName == filePath)
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