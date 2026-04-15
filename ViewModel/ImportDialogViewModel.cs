using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel
{

    public sealed class ImportDialogViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<LISCurveItem> _allCurves;
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

        public event PropertyChangedEventHandler PropertyChanged;

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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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

        public ICommand MoveSelectedRightCommand { get; }
        public ICommand MoveSelectedLeftCommand { get; }
        public ICommand MoveAllRightCommand { get; }
        public ICommand MoveAllLeftCommand { get; }
        public ICommand DoneCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SaveTemplateCommand { get; }
        public ICommand SaveAsTemplateCommand { get; }

        public event EventHandler RequestClose;
        public event EventHandler RequestCancel;

        public ImportDialogViewModel(IEnumerable<LISCurveItem> curves, IEnumerable<ParameterTable> parameterTables)
        {
            _allCurves = new ObservableCollection<LISCurveItem>(curves ?? Enumerable.Empty<LISCurveItem>());
            _selectedCurves = new ObservableCollection<LISCurveItem>();
            _parameterTables = new ObservableCollection<ParameterTable>(parameterTables ?? Enumerable.Empty<ParameterTable>());
            _selectedParameterTable = _parameterTables.FirstOrDefault();

            _availableCurvesView = CollectionViewSource.GetDefaultView(_allCurves);
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

            MoveSelectedRightCommand = new DelegateCommand(MoveSelectedToRight, CanMoveSelectedToRight);
            MoveSelectedLeftCommand = new DelegateCommand(MoveSelectedToLeft, CanMoveSelectedToLeft);
            MoveAllRightCommand = new DelegateCommand(_ => MoveAllToRight(), _ => _allCurves.Any(curve => curve.IsEnabled));
            MoveAllLeftCommand = new DelegateCommand(_ => MoveAllToLeft(), _ => _selectedCurves.Count > 0);

            DoneCommand = new DelegateCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty), _ => _selectedCurves.Count > 0);
            CancelCommand = new DelegateCommand(_ => RequestCancel?.Invoke(this, EventArgs.Empty));

            SaveTemplateCommand = new DelegateCommand(_ => { });
            SaveAsTemplateCommand = new DelegateCommand(_ => { });
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

            if (!curve.IsEnabled)
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
                AddCurve(curve);
            }

            _availableCurvesView.Refresh();
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
                RemoveCurve(selected);
            }

            _availableCurvesView.Refresh();
            RaiseCommandStates();
        }

        private void MoveAllToRight()
        {
            var items = _allCurves.Where(curve => curve.IsEnabled).ToList();
            foreach (var curve in items)
            {
                AddCurve(curve);
            }

            _availableCurvesView.Refresh();
            RaiseCommandStates();
        }

        private void MoveAllToLeft()
        {
            var items = _selectedCurves.ToList();
            foreach (var selected in items)
            {
                RemoveCurve(selected);
            }

            _availableCurvesView.Refresh();
            RaiseCommandStates();
        }

        private void AddCurve(LISCurveItem item)
        {
            if (item == null || !item.IsEnabled)
            {
                return;
            }

            item.IsEnabled = false;
            if (!_selectedCurves.Contains(item))
            {
                _selectedCurves.Add(item);
            }

            RaiseCommandStates();
        }

        private void RemoveCurve(LISCurveItem item)
        {
            if (item == null)
            {
                return;
            }

            item.IsEnabled = true;
            _selectedCurves.Remove(item);
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


        public sealed class NamedItem
        {
            public NamedItem(string name)
            {
                Name = name ?? string.Empty;
            }

            public string Name { get; }
        }

        private void RaiseCommandStates()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private sealed class DelegateCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Func<object, bool> _canExecute;

            public DelegateCommand(Action<object> execute, Func<object, bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute == null || _canExecute(parameter);
            }

            public void Execute(object parameter)
            {
                _execute(parameter);
            }

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}