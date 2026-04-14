using System.Collections.Generic;
using System.Windows;
using ImportDialogApp.Models;
using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel;

namespace ShellExtension.Formats.LIS.Dialogs;

public partial class ImportDialog : Window
{
    private readonly ImportDialogViewModel _viewModel;

    public ImportDialog(IReadOnlyCollection<CurveItem> curves, IReadOnlyCollection<ParameterTable> parameterTables)
    {
        InitializeComponent();
        _viewModel = new ImportDialogViewModel(curves, parameterTables);
        DataContext = _viewModel;
        _viewModel.RequestClose += OnRequestClose;
        _viewModel.RequestCancel += OnRequestCancel;
    }

    public IReadOnlyList<CurveItem> SelectedCurvesResult => _viewModel.GetSelectedCurves();

    private void OnRequestClose(object? sender, System.EventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnRequestCancel(object? sender, System.EventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
