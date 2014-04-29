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



    }
}