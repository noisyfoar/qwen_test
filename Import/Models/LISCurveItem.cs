using NPFGEO.Data;
using System;

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
                }
            }
            get
            {
                if (Is2D())
                    return (double)Source.GetDelta();
                return null;
            }
        }

        public int Precision
        {
            get
            {
                if(Source.GetDataPrecision() != null)
                {
                    return (int)Source.GetDataPrecision();
                }
                return 0;
            }
        }
        private bool Is2D() => Source.DataMatrix.Columns != 1;

        public LISCurveItem(Curve source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _sourceName = source.Caption ?? string.Empty;
        }

    }
}