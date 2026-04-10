using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace NPFGEO.IO.LAS.Import.ViewModel
{
    public class SelectMetricDialog : ViewModelBase
    {
        private object _model;
        private MetricBase _currentMetric;

        public SelectMetricDialog()
        {
            Metrics = new ObservableCollection<MetricBase>
            {
                new Time(),
                new Depth()
            };

            CurrentMetric = Metrics.FirstOrDefault();
        }

        public object Model
        {
            get => _model;
            set
            {
                _model = value;
                CallPropertyChanged(nameof(Model));
            }
        }

        public ObservableCollection<MetricBase> Metrics { get; }

        public MetricBase CurrentMetric
        {
            get => _currentMetric;
            set
            {
                _currentMetric = value;
                CallPropertyChanged(nameof(CurrentMetric));
            }
        }

        public bool CanApply()
        {
            return CurrentMetric?.CanApply() == true;
        }
    }

    public abstract class MetricBase : ViewModelBase
    {
        public virtual bool CanApply() => true;
    }

    public sealed class Depth : MetricBase
    {
        private MetricCurve _curve;
        private bool _isReversed;

        public ObservableCollection<MetricCurve> Curves { get; } = new ObservableCollection<MetricCurve>();
        public ObservableCollection<DepthPreviewItem> Preview { get; } = new ObservableCollection<DepthPreviewItem>();

        public MetricCurve Curve
        {
            get => _curve;
            set
            {
                _curve = value;
                CallPropertyChanged(nameof(Curve));
            }
        }

        public bool IsReversed
        {
            get => _isReversed;
            set
            {
                _isReversed = value;
                CallPropertyChanged(nameof(IsReversed));
            }
        }

        public override bool CanApply()
        {
            return Curve != null;
        }
    }

    public sealed class Time : MetricBase
    {
        private TimeFormatBase _currentFormat;

        public Time()
        {
            Formats = new ObservableCollection<TimeFormatBase>
            {
                new FromPlusTimeCurveFormat(),
                new DateDotTimeFormat(),
                new ReverseDateDotTimeFormat(),
                new ReverseDateTimeDotMSFormat(),
                new OleTimeFormat(),
                new UtcTimeFormat(),
                new DecTimeFormat()
            };

            CurrentFormat = Formats.FirstOrDefault();
        }

        public ObservableCollection<TimeFormatBase> Formats { get; }
        public ObservableCollection<TimePreviewItem> Preview { get; } = new ObservableCollection<TimePreviewItem>();

        public TimeFormatBase CurrentFormat
        {
            get => _currentFormat;
            set
            {
                _currentFormat = value;
                CallPropertyChanged(nameof(CurrentFormat));
            }
        }

        public override bool CanApply()
        {
            return CurrentFormat?.CanApply() == true;
        }
    }

    public abstract class TimeFormatBase : ViewModelBase
    {
        private MetricCurve _curve;
        private bool _isReversed;

        public ObservableCollection<MetricCurve> Curves { get; } = new ObservableCollection<MetricCurve>();

        public MetricCurve Curve
        {
            get => _curve;
            set
            {
                _curve = value;
                CallPropertyChanged(nameof(Curve));
            }
        }

        public bool IsReversed
        {
            get => _isReversed;
            set
            {
                _isReversed = value;
                CallPropertyChanged(nameof(IsReversed));
            }
        }

        public virtual bool CanApply() => Curve != null;
    }

    public sealed class FromPlusTimeCurveFormat : TimeFormatBase
    {
        private DateTime? _from = DateTime.Now.Date;
        private MetricParameter _fromParameter;

        public ObservableCollection<MetricParameter> Parameters { get; } = new ObservableCollection<MetricParameter>();

        public DateTime? From
        {
            get => _from;
            set
            {
                _from = value;
                CallPropertyChanged(nameof(From));
            }
        }

        public MetricParameter FromParameter
        {
            get => _fromParameter;
            set
            {
                _fromParameter = value;
                CallPropertyChanged(nameof(FromParameter));
            }
        }

        public override bool CanApply()
        {
            return base.CanApply() && From.HasValue && FromParameter != null;
        }
    }

    public sealed class DateDotTimeFormat : TimeFormatBase { }
    public sealed class ReverseDateDotTimeFormat : TimeFormatBase { }
    public sealed class ReverseDateTimeDotMSFormat : TimeFormatBase { }
    public sealed class OleTimeFormat : TimeFormatBase { }
    public sealed class UtcTimeFormat : TimeFormatBase { }
    public sealed class DecTimeFormat : TimeFormatBase { }

    public sealed class MetricCurve : ViewModelBase
    {
        private string _mnemonics;

        public string Mnemonics
        {
            get => _mnemonics;
            set
            {
                _mnemonics = value;
                CallPropertyChanged(nameof(Mnemonics));
            }
        }

        public object Value { get; set; }
    }

    public sealed class MetricParameter : ViewModelBase
    {
        private string _mnemonics;

        public string Mnemonics
        {
            get => _mnemonics;
            set
            {
                _mnemonics = value;
                CallPropertyChanged(nameof(Mnemonics));
            }
        }

        public object Value { get; set; }
    }

    public sealed class DepthPreviewItem : ViewModelBase
    {
        public int Index { get; set; }
        public double Depth { get; set; }
    }

    public sealed class TimePreviewItem : ViewModelBase
    {
        public int Index { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
