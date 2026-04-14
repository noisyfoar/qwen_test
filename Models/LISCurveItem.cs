using NPFGEO.Data;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models
{

    public sealed class LISCurveItem : ViewModelBase
    {

        public Curve Source { get; }

        private string _oldName;

        public string Name
        {
            get { return _oldName; }
        }
        public string NewName
        {
            set
            {
                Source.Caption = value;
                CallPropertyChanged(nameof(Name));
            }
            get { return Source.Caption; }
        }

        public string Description
        {
            set 
            { 
                Source.Description = value;
                CallPropertyChanged(nameof(Description));
            }
            get { return Source.Description; }
        }
        public string Units
        {
            set 
            { 
                Source.Units = value;
                CallPropertyChanged(nameof(Units));
            }
            get { return Source.Units; }
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
            Source = source;
            _oldName = source.Caption;
        }

    }
}