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
        private readonly ObservableCollection<LISCurveItem> _availableCurves;
        private readonly ObservableCollection<LISCurveItem> _selectedCurves;
        private readonly ObservableCollection<ParameterTable> _parameterTables;
        private readonly ICollectionView _availableCurvesView;

        private string _curveFilter = string.Empty;
        private ParameterTable _selectedParameterTable;
        private NamedItem _selectedTemplate;
        private NamedItem _currentMnemonicsSet;

        public ICollectionView AvailableCurvesView => _availableCurvesView;

        public ObservableCollection<LISCurveItem> SelectedCurves => _selectedCurves;

        public ObservableCollection<ParameterTable> ParameterTables => _parameterTables;
        public ObservableCollection<NamedItem> Templates { get; }
        public ObservableCollection<NamedItem> MnemonicsSets { get; }

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

        public NamedItem SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (Equals(_selectedTemplate, value))
                {
                    return;
                }

                _selectedTemplate = value;
                CallPropertyChanged(nameof(SelectedTemplate));
            }
        }

        public NamedItem CurrentMnemonicsSet
        {
            get => _currentMnemonicsSet;
            set
            {
                if (Equals(_currentMnemonicsSet, value))
                {
                    return;
                }

                _currentMnemonicsSet = value;
                CallPropertyChanged(nameof(CurrentMnemonicsSet));
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
        public RelayCommand CancelCommand { get; }
        public RelayCommand SaveTemplateCommand { get; }
        public RelayCommand SaveAsTemplateCommand { get; }

        // Backward compatible aliases.
        public RelayCommand AddCurves => MoveSelectedRightCommand;
        public RelayCommand RemoveCurves => MoveSelectedLeftCommand;
        public RelayCommand SelectAll => MoveAllRightCommand;
        public RelayCommand UnselectAll => MoveAllLeftCommand;
        public RelayCommand ConfirmCommand => DoneCommand;

        public event EventHandler RequestClose;
        public event EventHandler RequestCancel;

        public ImportDialogViewModel(IEnumerable<LISCurveItem> curves, IEnumerable<ParameterTable> parameterTables)
        {
            _availableCurves = new ObservableCollection<LISCurveItem>(curves ?? Enumerable.Empty<LISCurveItem>());
            _selectedCurves = new ObservableCollection<LISCurveItem>();
            _parameterTables = new ObservableCollection<ParameterTable>(parameterTables ?? Enumerable.Empty<ParameterTable>());

            if (_parameterTables.Count == 0)
            {
                _parameterTables.Add(new ParameterTable(
                    "Таблица 1",
                    new[] { "Параметр", "Значение" },
                    new List<ParameterRow>()));
            }

            _selectedParameterTable = _parameterTables.FirstOrDefault();

            _availableCurvesView = CollectionViewSource.GetDefaultView(_availableCurves);
            _availableCurvesView.Filter = FilterCurve;

            Templates = new ObservableCollection<NamedItem>
            {
                new NamedItem("Шаблон 1"),
                new NamedItem("Шаблон 2"),
                new NamedItem("Шаблон 3"),
            };
            MnemonicsSets = new ObservableCollection<NamedItem>
            {
                new NamedItem("Набор 1"),
                new NamedItem("Набор 2"),
            };
            SelectedTemplate = Templates.FirstOrDefault();
            CurrentMnemonicsSet = MnemonicsSets.FirstOrDefault();

            MoveSelectedRightCommand = new RelayCommand(MoveSelectedToRight, CanMoveSelectedToRight);
            MoveSelectedLeftCommand = new RelayCommand(MoveSelectedToLeft, CanMoveSelectedToLeft);
            MoveAllRightCommand = new RelayCommand(_ => MoveAllToRight(), _ => _availableCurves.Count > 0);
            MoveAllLeftCommand = new RelayCommand(_ => MoveAllToLeft(), _ => _selectedCurves.Count > 0);
            DoneCommand = new RelayCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty), _ => _selectedCurves.Count > 0);
            CancelCommand = new RelayCommand(_ => RequestCancel?.Invoke(this, EventArgs.Empty));
            SaveTemplateCommand = new RelayCommand(_ => { });
            SaveAsTemplateCommand = new RelayCommand(_ => { });
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

            return curve.SourceName.IndexOf(_curveFilter, StringComparison.OrdinalIgnoreCase) >= 0
                   || curve.Units.IndexOf(_curveFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

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

            RaiseCommandStates();
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

        private void RaiseCommandStates()
        {
            MoveSelectedRightCommand.RaiseCanExecuteChanged();
            MoveSelectedLeftCommand.RaiseCanExecuteChanged();
            MoveAllRightCommand.RaiseCanExecuteChanged();
            MoveAllLeftCommand.RaiseCanExecuteChanged();
            DoneCommand.RaiseCanExecuteChanged();
        }

        public sealed class NamedItem
        {
            public NamedItem(string name)
            {
                Name = name ?? string.Empty;
            }

            public string Name { get; }
        }
    }
}