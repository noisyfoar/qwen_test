using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
        private static readonly string TemplatesDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Genesis",
            "Export templates");

        private string _curveFilter = string.Empty;
        private ParameterTable _selectedParameterTable;
        private ExportTemplate _selectedTemplate;
        private NamedItem _currentMnemonicsSet;

        public ICollectionView AvailableCurvesView => _availableCurvesView;

        public ObservableCollection<LISCurveItem> SelectedCurves => _selectedCurves;

        public ObservableCollection<ParameterTable> ParameterTables => _parameterTables;
        public ObservableCollection<ExportTemplate> Templates { get; }
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

        public ExportTemplate SelectedTemplate
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
                ApplyTemplate();
                RaiseCommandStates();
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

            Templates = new ObservableCollection<ExportTemplate>();
            MnemonicsSets = new ObservableCollection<NamedItem>
            {
                new NamedItem("Набор 1"),
                new NamedItem("Набор 2"),
            };
            RefreshTemplates();
            SelectedTemplate = Templates.FirstOrDefault();
            CurrentMnemonicsSet = MnemonicsSets.FirstOrDefault();

            MoveSelectedRightCommand = new DelegateCommand(MoveSelectedToRight, CanMoveSelectedToRight);
            MoveSelectedLeftCommand = new DelegateCommand(MoveSelectedToLeft, CanMoveSelectedToLeft);
            MoveAllRightCommand = new DelegateCommand(_ => MoveAllToRight(), _ => _allCurves.Any(curve => curve.IsEnabled));
            MoveAllLeftCommand = new DelegateCommand(_ => MoveAllToLeft(), _ => _selectedCurves.Count > 0);

            DoneCommand = new DelegateCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty), _ => _selectedCurves.Count > 0);
            CancelCommand = new DelegateCommand(_ => RequestCancel?.Invoke(this, EventArgs.Empty));

            SaveTemplateCommand = new DelegateCommand(_ => SaveTemplate(), _ => CanSaveTemplate());
            SaveAsTemplateCommand = new DelegateCommand(_ => SaveTemplateAs(), _ => CanSaveTemplateAs());
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

        private void ApplyTemplate()
        {
            if (_selectedTemplate != null)
            {
                ApplyTemplate(_selectedTemplate);
            }
        }

        private void ApplyTemplate(ExportTemplate template)
        {
            if (template == null)
            {
                return;
            }

            MoveAllToLeft();
            foreach (var item in template.Items ?? Enumerable.Empty<ExportTemplateItem>())
            {
                var source = _allCurves.FirstOrDefault(curve =>
                    string.Equals(curve.SourceName?.Trim(), item.SourceName?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (source == null)
                {
                    continue;
                }

                AddCurve(source);
                source.ExportName = item.ExportName ?? string.Empty;
                source.Precision = item.Precision;
                source.Description = item.Description ?? string.Empty;
            }

            _availableCurvesView.Refresh();
            RaiseCommandStates();
        }

        private ExportTemplate ToTemplate()
        {
            var template = new ExportTemplate();
            foreach (var item in _selectedCurves)
            {
                template.Items.Add(ToTemplateItem(item));
            }

            return template;
        }

        private static ExportTemplateItem ToTemplateItem(LISCurveItem item)
        {
            return new ExportTemplateItem
            {
                SourceName = item.SourceName,
                ExportName = item.ExportName,
                Description = item.Description,
                Precision = item.Precision,
            };
        }

        private bool CanSaveTemplate()
        {
            return SelectedTemplate != null;
        }

        private void SaveTemplate()
        {
            if (SelectedTemplate == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedTemplate.FileName))
            {
                SaveTemplateAs();
                return;
            }

            var writer = new ExportTemplateReaderWriter();
            var template = ToTemplate();
            template.FileName = SelectedTemplate.FileName;
            template.Name = SelectedTemplate.Name;
            writer.Write(template.FileName, template);

            var index = Templates.IndexOf(SelectedTemplate);
            if (index >= 0)
            {
                Templates[index] = template;
            }
            else
            {
                Templates.Add(template);
            }

            SelectedTemplate = template;
        }

        private bool CanSaveTemplateAs()
        {
            return _selectedCurves.Count > 0;
        }

        private void SaveTemplateAs()
        {
            EnsureTemplatesDirectoryExists();

            var saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = TemplatesDirectoryPath,
                OverwritePrompt = true,
                Filter = "XML File (*.xml)|*.xml",
                AddExtension = true,
                DefaultExt = ".xml",
            };

            var result = saveFileDialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            var fullName = saveFileDialog.FileName;
            var template = ToTemplate();
            template.Name = Path.GetFileNameWithoutExtension(fullName);
            template.FileName = fullName;

            var writer = new ExportTemplateReaderWriter();
            writer.Write(fullName, template);

            var replaced = Templates.FirstOrDefault(existing =>
                string.Equals(existing.FileName, fullName, StringComparison.OrdinalIgnoreCase));

            if (replaced != null)
            {
                var index = Templates.IndexOf(replaced);
                Templates[index] = template;
            }
            else
            {
                Templates.Add(template);
            }

            SelectedTemplate = template;
        }

        private void RefreshTemplates()
        {
            Templates.Clear();
            EnsureTemplatesDirectoryExists();

            var reader = new ExportTemplateReaderWriter();
            foreach (var file in Directory.GetFiles(TemplatesDirectoryPath, "*.xml", SearchOption.AllDirectories))
            {
                try
                {
                    var template = reader.Read(file);
                    Templates.Add(template);
                }
                catch
                {
                    // Skip invalid template files so one malformed XML does not break the dialog.
                }
            }
        }

        private static void EnsureTemplatesDirectoryExists()
        {
            if (!Directory.Exists(TemplatesDirectoryPath))
            {
                Directory.CreateDirectory(TemplatesDirectoryPath);
            }
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