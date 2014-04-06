using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Leem.Testify
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class MyControl : UserControl
    {
        Poco.CodeModule src;
        public MyControl()
        {
            InitializeComponent();
            this.DataContext = src;
            LoadTestData();
  
        }
        

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "We are inside {0}.button1_Click()", this.ToString()),
                            "My Tool Window");
        }
        private void LoadTestData()
        {
            

            var module = new Poco.CodeModule { Summary = new Poco.Summary
                                                      { BranchCoverage=99.9M,
                                                        SequenceCoverage=90.0M,
                                                        NumBranchPoints=100, 
                                                        VisitedBranchPoints=99,
                                                        NumSequencePoints=88,
                                                        VisitedSequencePoints=77 },
                                                 Classes = new List<Poco.CodeClass>{new Poco.CodeClass
                                                        {Name="First Class"}, new Poco.CodeClass
                                                        {Name="Second Class"}}
            };
            CollectionViewSource test;
            //test.Source=    
            //SummaryView.ItemsSource = module;

        }

    }
}


