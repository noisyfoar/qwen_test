using System.Collections.Generic;
using System.Linq;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models
{
    public sealed class ParameterTable
    {
        public ParameterTable(string name, IEnumerable<string> columns, IEnumerable<IReadOnlyList<string>> rows = null)
        {
            Name = name;

            Columns = columns.ToList();
            Rows = rows.Select(r=>r.ToList()).ToList();
        }

        public string Name { get; }

        public IReadOnlyList<string> Columns { get; }
        public IReadOnlyList<IReadOnlyList<string>> Rows { get; }

    }

}