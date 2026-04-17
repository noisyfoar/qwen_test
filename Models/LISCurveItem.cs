using NPFGEO.Data;
using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel;
using System;
using System.Reflection;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models
{

    public sealed class LISCurveItem : ViewModelBase
    {
        public Curve Source { get; }
        private bool _isEnabled = true;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;
                CallPropertyChanged(nameof(IsEnabled));
            }
        }

        private readonly string _sourceName;

        public string SourceName
        {
            get { return _sourceName; }
        }

        public string ExportName
        {
            get { return Source.Caption; }
            set
            {
                Source.Caption = value ?? string.Empty;
                CallPropertyChanged(nameof(ExportName));
                CallPropertyChanged(nameof(NewName));
                CallPropertyChanged(nameof(Name));
            }
        }

        public string Name => SourceName;

        public string NewName
        {
            get => ExportName;
            set => ExportName = value;
        }

        public string Description
        {
            set 
            { 
                Source.Description = value ?? string.Empty;
                CallPropertyChanged(nameof(Description));
            }
            get { return Source.Description ?? string.Empty; }
        }

        public string Units
        {
            set 
            { 
                Source.Units = value ?? string.Empty;
                CallPropertyChanged(nameof(Units));
            }
            get { return Source.Units ?? string.Empty; }
        }

        public double? Begin
        {
            set 
            {
                if(value != null)
                {
                    Source.SetBegin((double)value);
                    CallPropertyChanged(nameof(Begin));
                    UpdateEndFromBeginDelta();
                }
            }
            get 
            {
                if (Is2D())
                    return (double)Source.GetBegin();
                return null;
            }
        }
        public double? Delta
        {
            set
            {
                if (value != null)
                {
                    Source.SetDelta((double)value);
                    CallPropertyChanged(nameof(Delta));
                    UpdateEndFromBeginDelta();
                }
            }
            get
            {
                if (Is2D())
                    return (double)Source.GetDelta();
                return null;
            }
        }

        public double? End
        {
            get
            {
                if (!Is2D())
                {
                    return null;
                }

                var getEndMethod = Source.GetType().GetMethod("GetEnd", BindingFlags.Public | BindingFlags.Instance);
                if (getEndMethod != null)
                {
                    try
                    {
                        var value = getEndMethod.Invoke(Source, null);
                        return value == null ? (double?)null : Convert.ToDouble(value);
                    }
                    catch
                    {
                        // Fall back to calculated value.
                    }
                }

                return CalculateEndValue();
            }
        }

        private bool Is2D() => Source.DataMatrix.Columns != 1;

        private void UpdateEndFromBeginDelta()
        {
            if (!Is2D())
            {
                return;
            }

            var end = CalculateEndValue();
            if (end == null)
            {
                return;
            }

            var setEndMethod = Source.GetType().GetMethod("SetEnd", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(double) }, null);
            if (setEndMethod != null)
            {
                try
                {
                    setEndMethod.Invoke(Source, new object[] { end.Value });
                }
                catch
                {
                    // Keep data editable even if end setter is unavailable for this curve type.
                }
            }

            CallPropertyChanged(nameof(End));
        }

        private double? CalculateEndValue()
        {
            if (!Is2D())
            {
                return null;
            }

            var begin = Begin;
            var delta = Delta;
            var pointsCount = GetPointsCount();

            if (begin == null || delta == null || pointsCount == null || pointsCount <= 0)
            {
                return null;
            }

            return begin.Value + delta.Value * (pointsCount.Value - 1);
        }

        private int? GetPointsCount()
        {
            var matrix = Source.DataMatrix;
            if (matrix == null)
            {
                return null;
            }

            var matrixType = matrix.GetType();
            var rowsProperty = matrixType.GetProperty("Rows", BindingFlags.Public | BindingFlags.Instance);
            if (rowsProperty != null)
            {
                var rowsValue = rowsProperty.GetValue(matrix, null);
                if (rowsValue != null)
                {
                    try
                    {
                        return Convert.ToInt32(rowsValue);
                    }
                    catch
                    {
                    }
                }
            }

            var countProperty = matrixType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            if (countProperty != null)
            {
                var countValue = countProperty.GetValue(matrix, null);
                if (countValue != null)
                {
                    try
                    {
                        return Convert.ToInt32(countValue);
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }

        public LISCurveItem(Curve source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _sourceName = source.Caption ?? string.Empty;
        }

    }
}