using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using ImportDialogApp.Infrastructure;
using ImportDialogApp.Models;

namespace ImportDialogApp.ViewModels;

public sealed class ImportDialogViewModel : INotifyPropertyChanged
{
    private readonly ObservableCollection<CurveItem> _availableCurves;
    private readonly ObservableCollection<CurveSelectionRowViewModel> _selectedCurves;
    private readonly ObservableCollection<ParameterTable> _parameterTables;
    private readonly ICollectionView _availableCurvesView;

    private string _curveFilter = string.Empty;
    private ParameterTable? _selectedParameterTable;
    private CurveItem? _selectedAvailableCurve;
    private CurveSelectionRowViewModel? _selectedSelectedCurve;

    public ImportDialogViewModel(
        IEnumerable<CurveItem> curves,
        IEnumerable<ParameterTable> parameterTables)
    {
        _availableCurves = new ObservableCollection<CurveItem>(curves ?? Enumerable.Empty<CurveItem>());
        _selectedCurves = new ObservableCollection<CurveSelectionRowViewModel>();
        _parameterTables = new ObservableCollection<ParameterTable>(
            parameterTables?.Any() == true
                ? parameterTables
                : BuildDefaultParameterTables());
        _selectedParameterTable = _parameterTables.FirstOrDefault();

        _availableCurvesView = CollectionViewSource.GetDefaultView(_availableCurves);
        _availableCurvesView.Filter = FilterCurve;

        MoveSelectedRightCommand = new RelayCommand(_ => MoveSelectedToRight(), _ => SelectedAvailableCurve is not null);
        MoveSelectedLeftCommand = new RelayCommand(_ => MoveSelectedToLeft(), _ => SelectedSelectedCurve is not null);
        MoveAllRightCommand = new RelayCommand(_ => MoveAllToRight(), _ => _availableCurves.Count > 0);
        MoveAllLeftCommand = new RelayCommand(_ => MoveAllToLeft(), _ => _selectedCurves.Count > 0);
        DoneCommand = new RelayCommand(_ => Done(), _ => _selectedCurves.Count > 0);
        CancelCommand = new RelayCommand(_ => RequestCancel?.Invoke(this, EventArgs.Empty));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? RequestClose;
    public event EventHandler? RequestCancel;

    public ICollectionView AvailableCurvesView => _availableCurvesView;

    public ObservableCollection<CurveSelectionRowViewModel> SelectedCurves => _selectedCurves;

    public ObservableCollection<ParameterTable> ParameterTables => _parameterTables;

    public ParameterTable? SelectedParameterTable
    {
        get => _selectedParameterTable;
        set
        {
            if (Equals(_selectedParameterTable, value))
            {
                return;
            }

            _selectedParameterTable = value;
            OnPropertyChanged();
        }
    }

    public CurveItem? SelectedAvailableCurve
    {
        get => _selectedAvailableCurve;
        set
        {
            if (Equals(_selectedAvailableCurve, value))
            {
                return;
            }

            _selectedAvailableCurve = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    public CurveSelectionRowViewModel? SelectedSelectedCurve
    {
        get => _selectedSelectedCurve;
        set
        {
            if (Equals(_selectedSelectedCurve, value))
            {
                return;
            }

            _selectedSelectedCurve = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    public string CurveFilter
    {
        get => _curveFilter;
        set
        {
            if (_curveFilter == value)
            {
                return;
            }

            _curveFilter = value;
            OnPropertyChanged();
            _availableCurvesView.Refresh();
        }
    }

    public RelayCommand MoveSelectedRightCommand { get; }
    public RelayCommand MoveSelectedLeftCommand { get; }
    public RelayCommand MoveAllRightCommand { get; }
    public RelayCommand MoveAllLeftCommand { get; }
    public RelayCommand DoneCommand { get; }
    public RelayCommand ConfirmCommand => DoneCommand;
    public RelayCommand CancelCommand { get; }

    public string SearchText
    {
        get => CurveFilter;
        set => CurveFilter = value;
    }

    public IReadOnlyList<CurveItem> GetSelectedCurves()
    {
        return _selectedCurves.Select(
            row => new CurveItem(row.SourceName, row.Units)
            {
                ExportName = row.ExportName,
                Description = row.Description,
                Precision = row.Precision
            }).ToList();
    }

    private bool FilterCurve(object obj)
    {
        if (obj is not CurveItem curve)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_curveFilter))
        {
            return true;
        }

        return curve.SourceName.Contains(_curveFilter, StringComparison.OrdinalIgnoreCase)
               || curve.Units.Contains(_curveFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<ParameterTable> BuildDefaultParameterTables()
    {
        return new[]
        {
            new ParameterTable(
                "Стандартная",
                new[]
                {
                    new ParameterRow("Скважина", "Well-01"),
                    new ParameterRow("Район", "North")
                }),
            new ParameterTable(
                "Расширенная",
                new[]
                {
                    new ParameterRow("Скважина", "Well-01"),
                    new ParameterRow("Район", "North"),
                    new ParameterRow("Поле", "Field A")
                }),
            new ParameterTable(
                "Пользовательская",
                new[]
                {
                    new ParameterRow("Параметр", "Значение")
                })
        };
    }

    private void Done() => RequestClose?.Invoke(this, EventArgs.Empty);

    private void MoveSelectedToRight()
    {
        if (SelectedAvailableCurve is null)
        {
            return;
        }

        var curve = SelectedAvailableCurve;
        _availableCurves.Remove(curve);
        _selectedCurves.Add(new CurveSelectionRowViewModel(curve));
        SelectedAvailableCurve = null;
        RaiseCommandStates();
    }

    private void MoveSelectedToLeft()
    {
        if (SelectedSelectedCurve is null)
        {
            return;
        }

        var selected = SelectedSelectedCurve;
        _selectedCurves.Remove(selected);
        _availableCurves.Add(selected.Source);
        SelectedSelectedCurve = null;
        RaiseCommandStates();
    }

    private void MoveAllToRight()
    {
        var items = _availableCurves.ToList();
        foreach (var curve in items)
        {
            _availableCurves.Remove(curve);
            _selectedCurves.Add(new CurveSelectionRowViewModel(curve));
        }

        RaiseCommandStates();
    }

    private void MoveAllToLeft()
    {
        var items = _selectedCurves.ToList();
        foreach (var selected in items)
        {
            _selectedCurves.Remove(selected);
            _availableCurves.Add(selected.Source);
        }

        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        MoveSelectedRightCommand.RaiseCanExecuteChanged();
        MoveSelectedLeftCommand.RaiseCanExecuteChanged();
        MoveAllRightCommand.RaiseCanExecuteChanged();
        MoveAllLeftCommand.RaiseCanExecuteChanged();
        DoneCommand.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
