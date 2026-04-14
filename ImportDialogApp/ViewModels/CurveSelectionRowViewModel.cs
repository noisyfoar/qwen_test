using ImportDialogApp.Models;

namespace ImportDialogApp.ViewModels;

public sealed class CurveSelectionRowViewModel : ViewModelBase
{
    private string _exportName;
    private string _description;
    private int _precision;

    public CurveSelectionRowViewModel(CurveItem source)
    {
        Source = source;
        _exportName = source.ExportName;
        _description = source.Description;
        _precision = source.Precision;
    }

    public CurveItem Source { get; }

    public string SourceName => Source.SourceName;
    public string Units => Source.Units;

    public string ExportName
    {
        get => _exportName;
        set
        {
            if (_exportName == value)
            {
                return;
            }

            _exportName = value;
            Source.ExportName = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description == value)
            {
                return;
            }

            _description = value;
            Source.Description = value;
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
            Source.Precision = value;
            OnPropertyChanged();
        }
    }

}
