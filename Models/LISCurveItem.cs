using NPFGEO.Data;
using System;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models
{

    public sealed class LISCurveItem : ViewModelBase
    {

        public Curve Source { get; }

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

        public double Begin
        {
            set 
            { 
                Source.SetBegin(value);
                CallPropertyChanged(nameof(Begin));
            }
            get 
            { return (double)Source.GetBegin(); }
        }
        public double Delta
        {
            set
            {
                Source.SetDelta(value);
                CallPropertyChanged(nameof(Delta));
            }
            get { return (double)Source.GetDelta(); }
        }

        public LISCurveItem(Curve source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _sourceName = source.Caption ?? string.Empty;
        }

    }
}