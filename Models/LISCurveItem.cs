using NPFGEO.Data;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models
{

    public sealed class LISCurveItem : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public Curve Source { get; }

        private readonly string _sourceName;
        private bool _isEnabled = true;
        private int _precision = 2;

        public string SourceName
        {
            get { return _sourceName; }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                {
                    return;
                }

                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        public int Precision
        {
            get => _precision;
            set
            {
                if (_precision == value)
                {
                    return;
                }

                _precision = value;
                OnPropertyChanged();
            }
        }

        public string ExportName
        {
            get { return Source.Caption; }
            set
            {
                Source.Caption = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NewName));
                OnPropertyChanged(nameof(Name));
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
                OnPropertyChanged();
            }
            get { return Source.Description ?? string.Empty; }
        }

        public string Units
        {
            set 
            { 
                Source.Units = value ?? string.Empty;
                OnPropertyChanged();
            }
            get { return Source.Units ?? string.Empty; }
        }

        public bool HasBeginDelta => Is2D();
        public bool Is1D => !Is2D();

        public double? Begin
        {
            set 
            {
                if(value != null && Is2D())
                {
                    Source.SetBegin((double)value);
                    OnPropertyChanged();
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
                if (value != null && Is2D())
                {
                    Source.SetDelta((double)value);
                    OnPropertyChanged();
                }
            }
            get
            {
                if (Is2D())
                    return (double)Source.GetDelta();
                return null;
            }
        }

        private bool Is2D() => Source.DataMatrix.Columns != 1;

        public LISCurveItem(Curve source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _sourceName = source.Caption ?? string.Empty;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}