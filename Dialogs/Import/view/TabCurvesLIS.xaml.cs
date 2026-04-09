using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ZedGraph;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.View
{
    public partial class TabCurvesLIS : UserControl
    {
        public static RoutedCommand Up = new RoutedCommand();
        public static RoutedCommand Down = new RoutedCommand();
        public static RoutedCommand AddCurves = new RoutedCommand();
        public static RoutedCommand RemoveCurves = new RoutedCommand();

        public TabCurvesLIS()
        {
            InitializeComponent();
        }

        ViewModel.TabCurvesDialogLIS ViewModel { get { return DataContext as ViewModel.TabCurvesDialogLIS; } }

        private void SelectAll_CanExecute(object sender, CanExecuteRoutedEventArgs e)
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
            ViewModel.Up(SelectedCurves.SelectedItem as NPFGEO.Data.Curve);
            SelectedCurves.ScrollIntoView(SelectedCurves.SelectedItem);
        }

        private void Down_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedCurves != null && SelectedCurves.SelectedItem != null;
        }

        private void Down_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.Down(SelectedCurves.SelectedItem as NPFGEO.Data.Curve);
            SelectedCurves.ScrollIntoView(SelectedCurves.SelectedItem);

        }

        private void AddCurves_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = LISCurvesList.SelectedItems != null && LISCurvesList.SelectedItems.Count > 0;
        }


        private void AddCurves_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.AddCurve(LISCurvesList.SelectedItems.Cast<NPFGEO.Data.Curve>());
        }

        private void RemoveCurve_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedCurves != null && SelectedCurves.SelectedItems.Count != 0;
        }


        private void RemoveCurve_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.RemoveCurve(SelectedCurves.SelectedItems.Cast<NPFGEO.Data.Curve>().ToArray());
        }

        private void AddToTargetButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).DataContext as NPFGEO.Data.Curve;
            ViewModel.AddCurve(new NPFGEO.Data.Curve[] { item });
        }
    }
}
