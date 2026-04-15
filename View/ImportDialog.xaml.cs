using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel;
using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.View
{

    public partial class ImportDialog : Window
    {
        public ImportDialog(IReadOnlyCollection<LISCurveItem> curves, IReadOnlyCollection<ParameterTable> parameterTables)
        {
            InitializeComponent();
            DataContext = new ImportDialogViewModel(
                curves ?? new List<LISCurveItem>(),
                parameterTables ?? Enumerable.Empty<ParameterTable>());
            _viewModel.RequestClose += (_, __) => CloseWindowWithDialogResult(true);
            _viewModel.RequestCancel += (_, __) => CloseWindowWithDialogResult(false);
            BuildParameterColumns(_viewModel.SelectedParameterTable);
        }
        private ImportDialogViewModel _viewModel { get { return DataContext as ImportDialogViewModel; } }

        private void ParameterTableSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BuildParameterColumns(_viewModel.SelectedParameterTable);
        }

        private void BuildParameterColumns(ParameterTable table)
        {
            ParametersGrid.Columns.Clear();
            if (table == null || table.Columns == null)
            {
                return;
            }

            for (var index = 0; index < table.Columns.Count; index++)
            {
                ParametersGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = table.Columns[index],
                    Binding = new Binding($"Values[{index}]"),
                    IsReadOnly = true,
                });
            }
        }

        private void CloseWindowWithDialogResult(bool result)
        {
            try
            {
                DialogResult = result;
            }
            catch (InvalidOperationException)
            {
                Close();
            }
        }
    }
}