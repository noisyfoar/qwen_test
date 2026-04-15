using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models
{
    public sealed class ParameterRow
    {
        public ParameterRow(IReadOnlyList<string> values)
        {
            Values = new ReadOnlyCollection<string>((values ?? Array.Empty<string>()).ToList());
        }

        public IReadOnlyList<string> Values { get; }
    }

    public sealed class ParameterTable
    {
        public ParameterTable(string name, IReadOnlyList<string> columns, IReadOnlyList<ParameterRow> rows)
        {
            Name = name ?? string.Empty;
            Columns = columns ?? Array.Empty<string>();
            Rows = rows ?? Array.Empty<ParameterRow>();
        }

        public string Name { get; }

        public IReadOnlyList<string> Columns { get; }
        public IReadOnlyList<ParameterRow> Rows { get; }

        // Read-only accessor similar to legacy ListView/SubItems logic.
        // rowName: value in first column, paramIndex: target column index.
        public string GetValue(string rowName, int paramIndex)
        {
            if (paramIndex < 0)
            {
                return null;
            }

            var rowNameTrimmed = (rowName ?? string.Empty).Trim();
            foreach (var row in Rows)
            {
                if (row.Values.Count == 0)
                {
                    continue;
                }

                if ((row.Values[0] ?? string.Empty).Trim() != rowNameTrimmed)
                {
                    continue;
                }

                if (paramIndex > row.Values.Count - 1)
                {
                    return null;
                }

                return row.Values[paramIndex];
            }

            return null;
        }
    }

}
