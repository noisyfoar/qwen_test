using NPFGEO;
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

namespace ShellExtension.Formats.LIS.Dialogs 
{ 

    /// <summary>
    /// Логика взаимодействия для LisExportSettingsWindow.xaml
    /// </summary>
    public partial class LisExportSettingsWindow : Window
    {
        public LisExportSettingsWindow()
        {
            InitializeComponent();
        }

        public LisExportSettingsWindowVM vm { get { return DataContext as LisExportSettingsWindowVM; } }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NumberSaveButton.Focus();
            this.DialogResult = true;
            this.Close();
        }

     
    }

    public class LisExportSettingsWindowVM : ViewModelBase
    {
        public LisExportSettingsWindowVM()
        {
            Step = 0.1;
        }

        private double _Step;
        public double Step
        {
            get { return _Step; }
            set
            {
                _Step = value;
                CallPropertyChanged("Step");
                CalculateLISStepUnit();
            }
        }

        private int _LISStep;
        public int LISStep
        {
            get { return _LISStep; }
            set
            {
                _LISStep = value;
                CallPropertyChanged(nameof(LISStep));
            }
        }

        private string _LISstepUnit;
        public string LISstepUnit
        {
            get { return _LISstepUnit; }
            set
            {
                _LISstepUnit = value;
                CallPropertyChanged(nameof(LISstepUnit));
            }
        }

        public int LISstepKoef;

        private void CalculateLISStepUnit()
        {
            var zerosCount = 2;
            var currLISStep = Step * Math.Pow(10, zerosCount);
            while (currLISStep % 1 != 0)
            {
                //throw new Exception();
                zerosCount++;
                currLISStep = Step * Math.Pow(10, zerosCount);
            }

            LISStep = (int)currLISStep;
            LISstepKoef = (int)Math.Pow(10, zerosCount);
            LISstepUnit = (LISstepKoef >= 1000) ? "MM  " : "CM  ";
        }


    }
}
