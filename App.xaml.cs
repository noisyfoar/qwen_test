using NPFGEO.Data;
using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.View;
using System.Collections.Generic;
using System.Windows;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var curves = new List<LISCurveItem>
            {
                new LISCurveItem(new Curve("GR", "gAPI", "Gamma Ray")),
                new LISCurveItem(new Curve("RHOB", "g/cc", "Bulk Density")),
                new LISCurveItem(new Curve("NPHI", "v/v", "Neutron Porosity")),
            };

            var parameterTables = new List<ParameterTable>
            {
                new ParameterTable("Основная таблица", new[]
                {
                    new ParameterItem("Step", "0.1"),
                    new ParameterItem("NullValue", "-999.25"),
                }),
            };

            var window = new ImportDialog(curves, parameterTables);
            MainWindow = window;
            window.Show();
        }
    }
}
