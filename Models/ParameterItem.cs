using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models
{
    public sealed class ParameterItem : ViewModelBase
    {
        private string _name;
        private string _value;

        public ParameterItem(string name, string value)
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
                CallPropertyChanged(nameof(Name));
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
                CallPropertyChanged(nameof(Value));
            }
        }
    }

    public sealed class ParameterTable
    {
        public ParameterTable(string name, IEnumerable<ParameterItem> rows = null)
        {
            Name = name;
            Rows = new ObservableCollection<ParameterItem>(rows ?? new List<ParameterItem>());
        }

        public string Name { get; }

        public ObservableCollection<ParameterItem> Rows { get; }
    }

}
