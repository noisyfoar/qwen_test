using System.Collections.Generic;
using System.Windows;
using ImportDialogApp.Models;
using ShellExtension.Formats.LIS.Dialogs;

namespace ImportDialogApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenImportDialog_Click(object sender, RoutedEventArgs e)
    {
        IReadOnlyCollection<CurveItem> curves = new[]
        {
            new CurveItem("GR", "gAPI"),
            new CurveItem("RHOB", "g/cm3"),
            new CurveItem("NPHI", "v/v"),
            new CurveItem("DT", "us/ft")
        };

        IReadOnlyCollection<ParameterTable> tables = new[]
        {
            new ParameterTable(
                "Стандартная",
                new[]
                {
                    new ParameterRow("Параметр", "Глубина"),
                    new ParameterRow("Ед. изм.", "м")
                }),
            new ParameterTable(
                "Расширенная",
                new[]
                {
                    new ParameterRow("Параметр", "Глубина"),
                    new ParameterRow("Ед. изм.", "м"),
                    new ParameterRow("Нуль", "-999.25"),
                    new ParameterRow("Порог", "0.01")
                }),
            new ParameterTable(
                "Пользовательская",
                new[]
                {
                    new ParameterRow("Ключ", "Значение")
                })
        };

        var dialog = new ImportDialog(curves, tables) { Owner = this };
        var result = dialog.ShowDialog();

        if (result != true)
        {
            return;
        }

        MessageBox.Show(
            this,
            $"Выбрано кривых: {dialog.SelectedCurvesResult.Count}",
            "Результат",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
