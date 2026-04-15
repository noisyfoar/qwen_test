using System;
using System.Collections.Generic;
namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models
{
    public sealed class ParameterTable
    {
        public ParameterTable(string name, IEnumerable<string> columns, IEnumerable<IReadOnlyList<string>> rows = null)
        {
            Name = name ?? string.Empty;
            Columns = (columns ?? Enumerable.Empty<string>()).ToList();
            Rows = (rows ?? Enumerable.Empty<IReadOnlyList<string>>()).Select(r => r.ToList()).ToList();
        }

        public string Name { get; }

        public IReadOnlyList<string> Columns { get; }
        public IReadOnlyList<IReadOnlyList<string>> Rows { get; }
    }

}
