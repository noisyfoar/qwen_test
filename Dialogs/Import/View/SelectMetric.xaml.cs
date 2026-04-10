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

namespace NPFGEO.IO.LAS.Import.View
{
    /// <summary>
    /// Логика взаимодействия для SelectMetric.xaml
    /// </summary>
    public partial class SelectMetric : UserControl
    {
        public SelectMetric()
        {
            InitializeComponent();
        }

        private void sourceDataGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            sourceDataGrid.Columns.Clear();

            if (e.NewValue != null)
            {
                var source = e.NewValue as NPFGEO.IO.LasResult;

                int index = 0;
                foreach (var curve in source.CurveInformation)
                {
                    var column = new DataGridTextColumn();
                    column.Width = new DataGridLength(100, DataGridLengthUnitType.Auto);
                    column.Header = curve.Mnemonics;
                    column.Binding = new Binding("Values[" + index + "]");

                    sourceDataGrid.Columns.Add(column);
                    index++;
                }
            }
        }

        ViewModel.SelectMetricDialog ViewModel { get { return DataContext as ViewModel.SelectMetricDialog; } }
    }
}
