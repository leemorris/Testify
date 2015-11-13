using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using EnvDTE;
using EnvDTE80;
using Leem.Testify.Poco;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Window = EnvDTE.Window;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Leem.Testify.UnitTestAdornment
{
    public class UnitTestSelector : Canvas
    {
        private static Brush brush;
        private static Pen solidPen;
        private static Pen dashPen;
        private readonly IAdornmentLayer _layer;
        private ITestifyQueries _queries;


        public UnitTestSelector(double ypos, UnitTestAdornment coveredLineInfo, IAdornmentLayer layer)
        {
            _layer = layer;
            string HeavyCheckMark = ((char) (0x2714)).ToString();
            string HeavyMultiplicationSign = ((char) (0x2716)).ToString();

            var myResourceDictionary = new ResourceDictionary();
            myResourceDictionary.Source =
                new Uri("/Testify;component/UnitTestAdornment/ResourceDictionary.xaml",
                    UriKind.RelativeOrAbsolute);

            var backgroundBrush = (Brush) myResourceDictionary["BackgroundBrush"];
            var borderBrush = (Brush) myResourceDictionary["BorderBrush"];
            var textBrush = (Brush) myResourceDictionary["TextBrush"];
            if (brush == null)
            {
                brush = (Brush) myResourceDictionary["BackgroundBrush"];
                //brush.Freeze();
                var penBrush = (Brush) myResourceDictionary["BorderBrush"];
                //penBrush.Freeze(); Can't be frozen because it is a Dynamic Resource
                solidPen = new Pen(penBrush, 0.5);
                //solidPen.Freeze(); Can't be frozen because it is a Dynamic Resource
                dashPen = new Pen(penBrush, 0.5);
                //dashPen.DashStyle = DashStyles.Dash;
                //dashPen.Freeze(); Can't be frozen because it is a Dynamic Resource
            }


            var tb = new TextBlock();
            tb.Text = " ";
            const int marginWidth = 20;
            var Margin = new Thickness(marginWidth, 0, marginWidth, 0);

            Grid postGrid = new Grid();

            postGrid.RowDefinitions.Add(new RowDefinition());
            postGrid.RowDefinitions.Add(new RowDefinition());
            var cEdge = new ColumnDefinition();
            cEdge.Width = new GridLength(1, GridUnitType.Auto);
            var cEdge2 = new ColumnDefinition {Width = new GridLength(19, GridUnitType.Star)};
            postGrid.ColumnDefinitions.Add(cEdge);
            postGrid.ColumnDefinitions.Add(new ColumnDefinition());
            postGrid.ColumnDefinitions.Add(cEdge2);
            var rect = new Rectangle();

            rect.Fill = brush;
            rect.Stroke = (Brush) myResourceDictionary["BorderBrush"];

            var inf = new Size(double.PositiveInfinity, double.PositiveInfinity);
            tb.Measure(inf);

            Grid.SetColumn(rect, 0);
            Grid.SetRow(rect, 0);
            Grid.SetRowSpan(rect, 3);
            Grid.SetColumnSpan(rect, 3);
            postGrid.Children.Add(rect);
            double desiredSize = 0;

            var header = new Label {Foreground = textBrush, Background = backgroundBrush, BorderBrush = borderBrush};

            Grid.SetRow(header, 0);
            Grid.SetColumn(header, 1);
            Grid.SetColumnSpan(header, 3);
            header.Content = string.Format("Unit tests covering Line # {0}, double-click to navigate", coveredLineInfo.CoveredLine.LineNumber);
            postGrid.Children.Add(header);
            var testMethods = coveredLineInfo.CoveredLine.TestMethods;
            var sortedTestMethods = testMethods.OrderByDescending(x => x.IsSuccessful ? 0 : 1).ToList();
            for (int i = 0; i < sortedTestMethods.Count; i++)
            {
                TestMethod test = sortedTestMethods.ElementAt(i);
                postGrid.RowDefinitions.Add(new RowDefinition());
                var icon = new Label {Background = backgroundBrush, BorderBrush = borderBrush, FocusVisualStyle = null};
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
                Grid.SetRow(icon, i + 1);
                Grid.SetColumn(icon, 0);
                postGrid.Children.Add(icon);

                var testName = new TextBox
                {
                    Foreground = textBrush,
                    Background = backgroundBrush,
                    BorderBrush = borderBrush,
                    FocusVisualStyle = null,
                    DataContext = test,
                    Text = test.TestMethodName,
                    Margin = Margin
                };

                var testBinding = new Binding("TestMethodName");

                testName.MouseDoubleClick += TestName_MouseDoubleClick;
                testName.SetBinding(TextBox.TextProperty, testBinding);
                Grid.SetRow(testName, i + 1);
                Grid.SetColumn(testName, 1);
                postGrid.Children.Add(testName);
                testName.Measure(inf);

                if (testName.DesiredSize.Width > desiredSize)
                {
                    desiredSize = testName.DesiredSize.Width;
                }
            }

            SetLeft(postGrid, 0);
            SetTop(postGrid, ypos);

            Focus();
            postGrid.Background = backgroundBrush;
            postGrid.LostFocus += postGrid_LostFocus;
            postGrid.MouseLeftButtonDown += postGrid_MouseLeftButtonDown;
            Children.Add(postGrid);
        }

        private void postGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _layer.RemoveAllAdornments();
        }

        private void postGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            _layer.RemoveAllAdornments();
        }


        private void TestName_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string unitTestMethodName = ((TextBox) sender).Text;
            _queries = TestifyQueries.Instance;
            int line = 0;
            const int column = 1;

            string name = string.Empty;
            Window openDocumentWindow;

            var unitTest = _queries.GetUnitTestByName(unitTestMethodName).FirstOrDefault();
            string filePath = unitTest.FilePath;
            line = unitTest.LineNumber;
            var dte = Package.GetGlobalService(typeof (DTE)) as DTE2;

            if (!string.IsNullOrEmpty(filePath) && filePath != string.Empty && !dte.ItemOperations.IsFileOpen(filePath))
            {
                openDocumentWindow = dte.ItemOperations.OpenFile(filePath);
                if (openDocumentWindow != null)
                {
                    ActivateWindowAtUnitTest(line, column, openDocumentWindow);
                }
                else
                {
                    for (int i = 1; i == dte.Documents.Count; i++)
                    {
                        if (dte.Documents.Item(i).Name == name)
                        {
                            openDocumentWindow = dte.Documents.Item(i).ProjectItem.Document.ActiveWindow;
                            ActivateWindowAtUnitTest(line, column, openDocumentWindow);
                        }
                    }
                }
            }

            else
            {
                for (int i = 1; i <= dte.Windows.Count; i++)
                {
                    Window window = dte.Windows.Item(i);
                    if (window.Document != null && window
                        .Document.FullName.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        //openDocumentWindow = window;
                        ActivateWindowAtUnitTest(line, column, window);
                    }
                }
            }
        }

        private static void ActivateWindowAtUnitTest(int line, int column, Window window)
        {
            window.Activate();
            var selection = window.Document.DTE.ActiveDocument.Selection as TextSelection;
            var adjustedLineNumber = line - 1;
            adjustedLineNumber = adjustedLineNumber < 1 ? 1: adjustedLineNumber;
            selection.MoveToLineAndOffset(adjustedLineNumber, column, true);

            selection.SelectLine();
        }
    }
}