using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ImportDialogApp.Models;

public sealed class ParameterTable
{
    public ParameterTable(string name, IEnumerable<ParameterRow>? rows = null)
    {
        Name = name;
        Rows = new ObservableCollection<ParameterRow>(rows ?? []);
    }

    public string Name { get; }

    public ObservableCollection<ParameterRow> Rows { get; }
}
