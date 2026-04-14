using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImportDialogApp.Models;

public sealed class CurveItem : INotifyPropertyChanged
{
    private string exportName;
    private string description;
    private int precision;

    public CurveItem(string sourceName, string units)
    {
        SourceName = sourceName;
        Units = units;
        exportName = sourceName;
        description = string.Empty;
        precision = 2;
    }

    public string SourceName { get; }

    public string Units { get; }

    public string ExportName
    {
        get => exportName;
        set => SetField(ref exportName, value);
    }

    public string Description
    {
        get => description;
        set => SetField(ref description, value);
    }

    public int Precision
    {
        get => precision;
        set => SetField(ref precision, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
