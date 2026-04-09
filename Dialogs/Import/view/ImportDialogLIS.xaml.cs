using System.Windows;
using System.Windows.Input;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.View
{
    /// <summary>
    /// Логика взаимодействия для ImportDialog.xaml
    /// </summary>
    public partial class ImportDialogLIS : Window
    {
        public ImportDialogLIS()
        {
            InitializeComponent();
        }

        public ViewModel.ImportDialogLIS ViewModel { get { return DataContext as ViewModel.ImportDialogLIS; } }

        private void Apply_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ViewModel != null && ViewModel.CanFinish();
        }

        private void Apply_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.Finish();
            this.DialogResult = true;
        }

        private void Cancel_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Cancel_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
