using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModels;
using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using System.Collections.Generic;
using System.Windows;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.View
{

    public partial class ImportDialog : Window
    {
        private readonly ImportDialogViewModel _viewModel;

        public ImportDialog(IReadOnlyCollection<LISCurveItem> curves, IReadOnlyCollection<ParameterTable> parameterTables)
        {
            InitializeComponent();
            _viewModel = new ImportDialogViewModel(curves, parameterTables);
            DataContext = _viewModel;
            _viewModel.RequestClose += OnRequestClose;
            _viewModel.RequestCancel += OnRequestCancel;
        }

        public IReadOnlyList<LISCurveItem> SelectedCurvesResult => _viewModel.GetSelectedCurves();

        private void OnRequestClose(object sender, System.EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnRequestCancel(object sender, System.EventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}