using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel;
using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        }
        private ImportDialogViewModel _viewModel { get { return DataContext as ImportDialogViewModel; } }

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