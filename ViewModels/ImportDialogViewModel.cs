using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using System.Linq;
using System;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModels
{

    public sealed class ImportDialogViewModel : ViewModelBase
    {
        private readonly ObservableCollection<LISCurveItem> _availableCurves;
        private readonly ObservableCollection<LISCurveItem> _selectedCurves;
        private readonly ObservableCollection<ParameterTable> _parameterTables;
        private readonly ICollectionView _availableCurvesView;

        private string _curveFilter = string.Empty;
        private ParameterTable _selectedParameterTable;
        private LISCurveItem _selectedAvailableCurve;
        private LISCurveItem _selectedSelectedCurve;

        public ImportDialogViewModel( IEnumerable<LISCurveItem> curves, IEnumerable<ParameterTable> parameterTables)
        {
            _availableCurves = new ObservableCollection<LISCurveItem>(curves ?? Enumerable.Empty<LISCurveItem>());
            _selectedCurves = new ObservableCollection<LISCurveItem>();
            _parameterTables = new ObservableCollection<ParameterTable>(parameterTables ?? Enumerable.Empty<ParameterTable>());
            _selectedParameterTable = _parameterTables.FirstOrDefault();

            _availableCurvesView = CollectionViewSource.GetDefaultView(_availableCurves);
            _availableCurvesView.Filter = FilterCurve;

            MoveSelectedRightCommand = new RelayCommand(_ => MoveSelectedToRight(), _ => SelectedAvailableCurve != null);
            MoveSelectedLeftCommand = new RelayCommand(_ => MoveSelectedToLeft(), _ => SelectedSelectedCurve != null);
            MoveAllRightCommand = new RelayCommand(_ => MoveAllToRight(), _ => _availableCurves.Count > 0);
            MoveAllLeftCommand = new RelayCommand(_ => MoveAllToLeft(), _ => _selectedCurves.Count > 0);
            DoneCommand = new RelayCommand(_ => Done(), _ => _selectedCurves.Count > 0);
            CancelCommand = new RelayCommand(_ => RequestCancel?.Invoke(this, EventArgs.Empty));
        }

        public event EventHandler RequestClose;
        public event EventHandler RequestCancel;

        public ICollectionView AvailableCurvesView => _availableCurvesView;

        public ObservableCollection<LISCurveItem> SelectedCurves => _selectedCurves;

        public ObservableCollection<ParameterTable> ParameterTables => _parameterTables;

        public ParameterTable SelectedParameterTable
        {
            get => _selectedParameterTable;
            set
            {
                if (Equals(_selectedParameterTable, value))
                {
                    return;
                }

                _selectedParameterTable = value;
                CallPropertyChanged(nameof(SelectedParameterTable));
            }
        }

        public LISCurveItem SelectedAvailableCurve
        {
            get => _selectedAvailableCurve;
            set
            {
                if (Equals(_selectedAvailableCurve, value))
                {
                    return;
                }

                _selectedAvailableCurve = value;
                CallPropertyChanged(nameof(SelectedAvailableCurve));
                RaiseCommandStates();
            }
        }

        public LISCurveItem SelectedSelectedCurve
        {
            get => _selectedSelectedCurve;
            set
            {
                if (Equals(_selectedSelectedCurve, value))
                {
                    return;
                }

                _selectedSelectedCurve = value;
                CallPropertyChanged(nameof(SelectedSelectedCurve));
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
                CallPropertyChanged(nameof(CurveFilter));
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

        public IReadOnlyList<LISCurveItem> GetSelectedCurves()
        {
            return _selectedCurves.ToList();
        }

        private bool FilterCurve(object obj)
        {
            if (!(obj is LISCurveItem curve))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_curveFilter))
            {
                return true;
            }

            return curve.Name.IndexOf(_curveFilter, StringComparison.OrdinalIgnoreCase) >= 0
                   || curve.Units.IndexOf(_curveFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IEnumerable<ParameterTable> BuildDefaultParameterTables()
        {
            return null;
        }


        private void MoveSelectedToRight()
        {
            if (SelectedAvailableCurve is null)
            {
                return;
            }

            var curve = SelectedAvailableCurve;
            _availableCurves.Remove(curve);
            _selectedCurves.Add(new LISCurveItem(curve.Source));
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
            _availableCurves.Add(selected);
            SelectedSelectedCurve = null;
            RaiseCommandStates();
        }

        private void MoveAllToRight()
        {
            var items = _availableCurves.ToList();
            foreach (var curve in items)
            {
                _availableCurves.Remove(curve);
                _selectedCurves.Add(new LISCurveItem(curve.Source));
            }

            RaiseCommandStates();
        }

        private void MoveAllToLeft()
        {
            var items = _selectedCurves.ToList();
            foreach (var selected in items)
            {
                _selectedCurves.Remove(selected);
                _availableCurves.Add(selected);
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

    }
}