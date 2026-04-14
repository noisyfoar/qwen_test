using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImportDialogApp.Models;

public sealed class ParameterRow : INotifyPropertyChanged
{
    private string _name;
    private string _value;

    public ParameterRow(string name, string value)
    {
        _name = name;
        _value = value;
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
