using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel
{

    public sealed class ImportDialogViewModel : ViewModelBase
    {

        #region private var
        private readonly ObservableCollection<LISCurveItem> _availableCurves;
        private readonly ObservableCollection<LISCurveItem> _selectedCurves;
        private readonly ObservableCollection<ParameterTable> _parameterTables;
        private readonly ICollectionView _availableCurvesView;

        private string _curveFilter = string.Empty;
        private ParameterTable _selectedParameterTable;
        #endregion

        #region public var
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

        public RelayCommand AddCurves { get; }
        public RelayCommand RemoveCurves { get; }
        public RelayCommand SelectAll { get; }
        public RelayCommand UnselectAll { get; }

        #endregion

        public ImportDialogViewModel( IEnumerable<LISCurveItem> curves, IEnumerable<ParameterTable> parameterTables)
        {
            _availableCurves = new ObservableCollection<LISCurveItem>(curves ?? Enumerable.Empty<LISCurveItem>());
            _selectedCurves = new ObservableCollection<LISCurveItem>();
            _parameterTables = new ObservableCollection<ParameterTable>(parameterTables ?? Enumerable.Empty<ParameterTable>());
            _selectedParameterTable = _parameterTables.FirstOrDefault();

            _availableCurvesView = CollectionViewSource.GetDefaultView(_availableCurves);
            _availableCurvesView.Filter = FilterCurve;

            AddCurves = new RelayCommand(MoveSelectedToRight, CanMoveSelectedToRight);
            RemoveCurves = new RelayCommand(MoveSelectedToLeft, CanMoveSelectedToLeft);
            SelectAll = new RelayCommand(_ => MoveAllToRight(), _ => _availableCurves.Count > 0);
            UnselectAll = new RelayCommand(_ => MoveAllToLeft(), _ => _selectedCurves.Count > 0);
        }
        public string SearchText
        {
            get => CurveFilter;
            set => CurveFilter = value;
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

        #region command
        private bool CanMoveSelectedToRight(object parameter)
        {
            return GetSelectedItems(parameter).Count > 0;
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
        }
        private bool CanMoveSelectedToLeft(object parameter)
        {
             return GetSelectedItems(parameter).Count > 0;
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
        }

        private void MoveAllToRight()
        {
            var items = _availableCurves.ToList();
            foreach (var curve in items)
            {
                _availableCurves.Remove(curve);
                _selectedCurves.Add(new LISCurveItem(curve.Source));
            }
        }

        private void MoveAllToLeft()
        {
            var items = _selectedCurves.ToList();
            foreach (var selected in items)
            {
                _selectedCurves.Remove(selected);
                _availableCurves.Add(selected);
            }
        }
        #endregion

        #region utils
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
        #endregion
    }
}