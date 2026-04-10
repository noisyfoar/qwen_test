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
using System.Windows.Shapes;

namespace ShellExtension.Formats.LIS.Dialogs.Import.View
{
    /// <summary>
    /// Логика взаимодействия для ImportDialog.xaml
    /// </summary>
    public partial class ImportDialog : Window
    {
        public ImportDialog()
        {
            InitializeComponent();
        }

        public ViewModel.ImportDialog ViewModel { get { return DataContext as ViewModel.ImportDialog; } }

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
