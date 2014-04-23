using System.Windows.Controls;
using Leem.Testify;
using System.Windows;

namespace Leem.Testify
{
    public partial class SummaryViewControl : UserControl
    {
        private ITestifyQueries queries;
        public TestifyCoverageWindow _parent;

        public SummaryViewControl(TestifyCoverageWindow parent)
        {
            InitializeComponent();
            _parent = parent;
            queries = TestifyQueries.Instance;



            if (TestifyQueries.SolutionName != null)
            {
                Poco.CodeModule[] modules = queries.GetSummaries();
                SummaryViewModel2 viewModel = new SummaryViewModel2(modules);
                base.DataContext = viewModel;
                treeGrid.DataContext = viewModel;

            }
            else
            {
                base.DataContext = new SummaryViewModel2();
            }
        }

        //public SummaryViewControl()
        //{
        //    queries = TestifyQueries.Instance;
           
        //    InitializeComponent();

        //    if (TestifyQueries.SolutionName != null)
        //    {
        //        Poco.CodeModule[] modules = queries.GetSummaries();
        //        SummaryViewModel2 viewModel = new SummaryViewModel2(modules);
        //        base.DataContext = viewModel;
        //        treeGrid.DataContext = viewModel;

        //    }
        //    else
        //    {
        //        base.DataContext = new SummaryViewModel2();
        //    }


        //}

    }
}