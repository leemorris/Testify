﻿using System.Windows.Controls;
using Leem.Testify;


namespace Leem.Testify
{
    public partial class SummaryViewControl : UserControl
    {
        private ITestifyQueries queries;
        public SummaryViewControl()
        {
            queries = TestifyQueries.Instance;
           
            InitializeComponent();

            Poco.CodeModule[] modules = queries.GetModules(); //null;// = Database.GetRegions();
            SummaryViewModel viewModel = new SummaryViewModel(modules);
            base.DataContext = viewModel;
        }
    }
}