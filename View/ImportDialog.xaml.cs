using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel;
using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using System.Collections.Generic;
using System.Windows;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.View
{

    public partial class ImportDialog : Window
    {
        public ImportDialog(IReadOnlyCollection<LISCurveItem> curves, IReadOnlyCollection<ParameterTable> parameterTables)
        {
            InitializeComponent();
        }
        private ImportDialogViewModel _viewModel { get { return DataContext as ImportDialogViewModel; } }

    }
}