using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShellExtension.Formats.LIS.Dialogs.Import.View
{
    /// <summary>
    /// Логика взаимодействия для SelectCurves.xaml
    /// </summary>
    public partial class SelectCurves : UserControl
    {
        public static RoutedCommand Up = new RoutedCommand();
        public static RoutedCommand Down = new RoutedCommand();
        public static RoutedCommand AddCurves = new RoutedCommand();
        public static RoutedCommand RemoveCurves = new RoutedCommand();

        public SelectCurves()
        {
            InitializeComponent();
        }

        ViewModel.SelectCurvesDialog ViewModel { get { return DataContext as ViewModel.SelectCurvesDialog; } }

        private void SelecteAll_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ViewModel != null && ViewModel.CanSelectAll();
        }

        private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.SelectAll();
        }

        private void UnselectAll_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ViewModel != null && ViewModel.CanUnselectAll();
        }

        private void UnselectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.UnselectAll();
        }

        private void Up_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedCurves != null && SelectedCurves.SelectedItem != null;
        }

        private void Up_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.Up(SelectedCurves.SelectedItem as ViewModel.SelectCurvesDialog.Item);
            SelectedCurves.ScrollIntoView(SelectedCurves.SelectedItem);
        }

        private void Down_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedCurves != null && SelectedCurves.SelectedItem != null;
        }

        private void Down_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.Down(SelectedCurves.SelectedItem as ViewModel.SelectCurvesDialog.Item);
            SelectedCurves.ScrollIntoView(SelectedCurves.SelectedItem);

        }

        private void AddCurves_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CurvesList.SelectedItems != null && CurvesList.SelectedItems.Count > 0;
        }


        private void AddCurves_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.AddCurve(CurvesList.SelectedItems.Cast<ViewModel.SelectCurvesDialog.Item>().ToArray());
        }

        private void RemoveCurve_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedCurves != null && SelectedCurves.SelectedItems.Count != 0;
        }


        private void RemoveCurve_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.RemoveCurve(SelectedCurves.SelectedItems.Cast<ViewModel.SelectCurvesDialog.Item>().ToArray());

        }

        private void AddToTargetButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).DataContext as ViewModel.SelectCurvesDialog.Item;
            ViewModel.AddCurve(new ViewModel.SelectCurvesDialog.Item[] { item });
        }
    }
}
