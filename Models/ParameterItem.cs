using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel;

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
        public ParameterTable(string name, IEnumerable<ParameterItem> rows = null)
            : this(
                name,
                new[] { "Name", "Value" },
                (rows ?? Enumerable.Empty<ParameterItem>())
                    .Select(item => (IReadOnlyList<string>)new[]
                    {
                        item?.Name ?? string.Empty,
                        item?.Value ?? string.Empty,
                    }))
        {
        }

        public ParameterTable(string name, IEnumerable<string> columns, IEnumerable<IReadOnlyList<string>> rows = null)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Table" : name;
            Columns = new ReadOnlyCollection<string>(
                (columns ?? Enumerable.Empty<string>())
                    .Select((columnName, index) => string.IsNullOrWhiteSpace(columnName) ? $"Column{index + 1}" : columnName.Trim())
                    .DefaultIfEmpty("Value")
                    .Distinct(StringComparer.Ordinal)
                    .ToList());

            Rows = new ReadOnlyCollection<ParameterRow>(
                (rows ?? Enumerable.Empty<IReadOnlyList<string>>())
                    .Select(row => new ParameterRow(row))
                    .ToList());
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
