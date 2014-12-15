using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

namespace Leem.Testify.UnitTestAdornment
{
    public class UnitTestSelector : Canvas
    {
        private Geometry textGeometry;
        private Grid postGrid;
        private static Brush brush;
        private static Pen solidPen;
        private static Pen dashPen;
        private double vertPos;
        private ICollection<Poco.UnitTest> collection;
        private IAdornmentLayer _layer;
        private ITestifyQueries queries;

        public UnitTestSelector( double ypos, UnitTestAdornment coveredLineInfo, IAdornmentLayer layer)
        {
            _layer = layer;
            string HeavyCheckMark = ((char)(0x2714)).ToString();
            string HeavyMultiplicationSign = ((char)(0x2716)).ToString();

            var myResourceDictionary = new ResourceDictionary();
            myResourceDictionary.Source =
                new Uri("/Testify;component/UnitTestAdornment/ResourceDictionary.xaml",
                        UriKind.RelativeOrAbsolute);

            var backgroundBrush = (Brush)myResourceDictionary["BackgroundBrush"];
            var borderBrush = (Brush)myResourceDictionary["BorderBrush"];
            var textBrush = (Brush)myResourceDictionary["TextBrush"];
            if (brush == null)
            {
             
                brush = (Brush)myResourceDictionary["BackgroundBrush"];
                //brush.Freeze();
                Brush penBrush = (Brush)myResourceDictionary["BorderBrush"];
                //penBrush.Freeze(); Can't be frozen because it is a Dynamic Resource
                solidPen = new Pen(penBrush, 0.5);
                //solidPen.Freeze(); Can't be frozen because it is a Dynamic Resource
                dashPen = new Pen(penBrush, 0.5);
                //dashPen.DashStyle = DashStyles.Dash;
                //dashPen.Freeze(); Can't be frozen because it is a Dynamic Resource
            }



            TextBlock tb = new TextBlock();
            tb.Text = " " ;
            var marginWidth = 20;
            var Margin = new Thickness(marginWidth, 0, marginWidth, 0);

            this.postGrid = new Grid();
            //this.postGrid.ShowGridLines = true;

            this.postGrid.RowDefinitions.Add(new RowDefinition());
            this.postGrid.RowDefinitions.Add(new RowDefinition());
            ColumnDefinition cEdge = new ColumnDefinition();
            cEdge.Width = new GridLength(1, GridUnitType.Auto);
            ColumnDefinition cEdge2 = new ColumnDefinition();
            cEdge2.Width = new GridLength(19, GridUnitType.Star);
            this.postGrid.ColumnDefinitions.Add(cEdge);
            this.postGrid.ColumnDefinitions.Add(new ColumnDefinition());
            this.postGrid.ColumnDefinitions.Add(cEdge2);
            System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();

            rect.Fill = brush;
            rect.Stroke = (Brush)myResourceDictionary["BorderBrush"];
           
            Size inf = new Size(double.PositiveInfinity, double.PositiveInfinity);
            tb.Measure(inf);
            //this.postGrid.Width = 600;//tb.DesiredSize.Width + 2 * MarginWidth;

            Grid.SetColumn(rect, 0);
            Grid.SetRow(rect, 0);
            Grid.SetRowSpan(rect, 3);
            Grid.SetColumnSpan(rect, 3);
            this.postGrid.Children.Add(rect);
            double desiredSize = 0;

            var header = new Label { Foreground = textBrush, Background = backgroundBrush, BorderBrush = borderBrush };
       
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, 1);
            Grid.SetColumnSpan(header, 3);
            header.Content = string.Format("Unit tests covering Line # {0}", coveredLineInfo.CoveredLine.LineNumber);
            this.postGrid.Children.Add(header);

            for (var i = 0; i < coveredLineInfo.CoveredLine.UnitTests.Count; i++)
            {
                var test = coveredLineInfo.CoveredLine.UnitTests.ElementAt(i);
               this.postGrid.RowDefinitions.Add(new RowDefinition());
               Label icon = new Label { Background = backgroundBrush, BorderBrush = borderBrush, FocusVisualStyle = null };
               if (test.IsSuccessful)
               {
                   icon.Content = HeavyCheckMark;
                   icon.Foreground = new SolidColorBrush(Colors.Green);
               }
               else
               {
                   icon.Content = HeavyMultiplicationSign;
                   icon.Foreground = new SolidColorBrush(Colors.Red);
               }
               //icon.DataContext = test;
               //Binding iconBinding = new Binding("IsSuccessful");
               //icon.SetBinding(TextBox.TextProperty, iconBinding);
               Grid.SetRow(icon, i+1);
               Grid.SetColumn(icon, 0);
               this.postGrid.Children.Add(icon);

               TextBox testName = new TextBox { Foreground = textBrush, Background = backgroundBrush, BorderBrush = borderBrush, FocusVisualStyle= null };
               testName.DataContext = test;
               Binding testBinding = new Binding("TestMethodName");
               testName.Text = test.TestMethodName;
               testName.MouseDoubleClick += TestName_MouseDoubleClick;
               testName.SetBinding(TextBox.TextProperty, testBinding);
               Grid.SetRow(testName, i+1);
               Grid.SetColumn(testName, 1);
               this.postGrid.Children.Add(testName);
               testName.Measure(inf);
               testName.Margin = Margin;
              // testName.Width =( 2* testName.DesiredSize.Width) + 2 * MarginWidth; 
               if (testName.DesiredSize.Width > desiredSize)
               { 
                   desiredSize = testName.DesiredSize.Width;
               }
            }


            Canvas.SetLeft(this.postGrid,0);
            Canvas.SetTop(this.postGrid, ypos);

            this.Focus();
            this.postGrid.Background = backgroundBrush;
            this.postGrid.LostFocus += postGrid_LostFocus;
            this.postGrid.MouseLeftButtonDown += postGrid_MouseLeftButtonDown;
            this.Children.Add(this.postGrid);
        }

        void postGrid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _layer.RemoveAllAdornments();
        }

        void postGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            _layer.RemoveAllAdornments();

        }

        private void TestName_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            // Get an instance of the currently running Visual Studio IDE.
            //EnvDTE80.DTE2 dte2;
            //dte2 = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.
            //GetActiveObject("VisualStudio.DTE.12.0");
            var unitTestMethodName = ((TextBox)sender).Text;
            queries = TestifyQueries.Instance;
            string filePath = string.Empty;
            int line = 0;
            int column = 0;
         
            string name = string.Empty;
            EnvDTE.Window openDocumentWindow = null;
            string clickedMethodName = string.Empty;
            var result = queries.GetUnitTestByName(unitTestMethodName);
            EnvDTE80.DTE2 dte = TestifyPackage.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            //queries.GetProjectFilePathFromMethod(sender.
            //IList<CodeElement> classes;
            //IList<CodeElement> methods;
            //if (dte.ActiveDocument != null)
            //{

            //    CodeModelService.GetCodeBlocks(dte.ActiveDocument.ProjectItem.FileCodeModel, out classes, out methods);

            //    if (type == "Leem.Testify.MethodViewModel")
            //    {
            //        clickedMethodName = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FullName;

            //        var method = queries.GetMethod(clickedMethodName);
            //        filePath = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).FileName;
            //        line = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Line;
            //        column = ((Leem.Testify.MethodViewModel)(((System.Windows.Controls.HeaderedItemsControl)(e.Source)).Header)).Column;
            //    }
            //}


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
                        var selection = window.Document.DTE.ActiveDocument.Selection as EnvDTE.TextSelection;
                        selection.StartOfDocument();
                        selection.MoveToLineAndOffset(line, column, true);

                        selection.SelectLine();

                        continue;
                    }
                }
            }
        }

       



    }
}
      