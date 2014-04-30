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
            string fileName= string.Empty;
            string type = ((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header.ToString();
            string name;

            if (type == "Leem.Testify.ClassViewModel")
            {
                name = ((Leem.Testify.ClassViewModel)(((System.Windows.Controls.HeaderedItemsControl)(sender)).Header)).Name;
                fileName = queries.GetProjectFilePathFromClass(name);
            }
            else if (type == "Leem.Testify.MethodViewModel")
            {
                name = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(sender)).Header)).Name;
                fileName = queries.GetProjectFilePathFromMethod(name);
            }
           // Log.DebugFormat("Type: {0}, Value: {1}", ((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header, ((Leem.Testify.ModuleViewModel)(((System.Windows.Controls.HeaderedItemsControl)(sender)).Header)).Name);

            
            
            //DTE2 dte = TestifyPackage.GetGlobalService(typeof(DTE)) as DTE2;
            //var projectPath = System.IO.Path.GetDirectoryName(dte.Solution.FullName) + "\\" + fileName;
            //EnvDTE.Window openDocumentWindow = dte.ItemOperations.OpenFile(projectPath);
            //if (openDocumentWindow != null)
            //{
            //    openDocumentWindow.Activate();
            //}
        }


    }
}