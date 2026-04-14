using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
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

        public ImportDialogViewModel( IEnumerable<LISCurveItem> curves, IEnumerable<ParameterTable> parameterTables)
        {
            _availableCurves = new ObservableCollection<LISCurveItem>(curves ?? Enumerable.Empty<LISCurveItem>());
            _selectedCurves = new ObservableCollection<LISCurveItem>();
            _parameterTables = new ObservableCollection<ParameterTable>(parameterTables ?? Enumerable.Empty<ParameterTable>());
            _selectedParameterTable = _parameterTables.FirstOrDefault();

            _availableCurvesView = CollectionViewSource.GetDefaultView(_availableCurves);
            _availableCurvesView.Filter = FilterCurve;

            MoveSelectedRightCommand = new RelayCommand(MoveSelectedToRight, CanMoveSelectedToRight);
            MoveSelectedLeftCommand = new RelayCommand(MoveSelectedToLeft, CanMoveSelectedToLeft);
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


        private bool CanMoveSelectedToRight(object parameter)
        {
            return GetSelectedItems(parameter).Count > 0;
        }

        private bool CanMoveSelectedToLeft(object parameter)
        {
            return GetSelectedItems(parameter).Count > 0;
        }

        private static List<LISCurveItem> GetSelectedItems(object parameter)
        {
            var result = new List<LISCurveItem>();
            var selectedItems = parameter as IEnumerable;
            if (selectedItems == null)
            {
                return result;
            }

            foreach (var item in selectedItems)
            {
                var curve = item as LISCurveItem;
                if (curve != null)
                {
                    result.Add(curve);
                }
            }

            return result;
        }

        private void MoveSelectedToRight(object parameter)
        {
            var selectedItems = GetSelectedItems(parameter);
            if (selectedItems.Count == 0)
            {
                return;
            }

            foreach (var curve in selectedItems.ToList())
            {
                if (!_availableCurves.Remove(curve))
                {
                    continue;
                }

                _selectedCurves.Add(new LISCurveItem(curve.Source));
            }
            RaiseCommandStates();
        }

        private void MoveSelectedToLeft(object parameter)
        {
            var selectedItems = GetSelectedItems(parameter);
            if (selectedItems.Count == 0)
            {
                return;
            }

            foreach (var selected in selectedItems.ToList())
            {
                if (!_selectedCurves.Remove(selected))
                {
                    continue;
                }

                _availableCurves.Add(selected);
            }
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