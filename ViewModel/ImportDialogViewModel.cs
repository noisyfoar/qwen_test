using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel
{

    public sealed class ImportDialogViewModel : ViewModelBase
    {
        private readonly ObservableCollection<LISCurveItem> _allCurves;
        private readonly ObservableCollection<LISCurveItem> _selectedCurves;
        private readonly ObservableCollection<ParameterTable> _parameterTables;
        private readonly ICollectionView _availableCurvesView;
        private static readonly string TemplatesDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Genesis",
            "Export templates");
        private static readonly string MnemonicsDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Genesis",
            "Mnemonics sets");
        private const string NoTemplateName = "Без шаблона";

        private string _curveFilter = string.Empty;
        private ParameterTable _selectedParameterTable;
        private ExportTemplate _selectedTemplate;
        private MnemonicsSet _currentMnemonicsSet;
        private bool _suppressTemplateApply;

        public ICollectionView AvailableCurvesView => _availableCurvesView;

        public ObservableCollection<LISCurveItem> SelectedCurves => _selectedCurves;

        public ObservableCollection<ParameterTable> ParameterTables => _parameterTables;
        public ObservableCollection<ExportTemplate> Templates { get; }
        public ObservableCollection<MnemonicsSet> MnemonicsSets { get; }

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
                CallPropertyChanged(nameof(SelectedTemplate));
                if (!_suppressTemplateApply)
                {
                    ApplyTemplate();
                }
                RaiseCommandStates();
            }
        }

        public MnemonicsSet CurrentMnemonicsSet
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
                ApplyMnemonicsSet();
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
            MnemonicsSets = new ObservableCollection<MnemonicsSet>();
            RefreshTemplates();
            RefreshMnemonicsSets();
            _suppressTemplateApply = true;
            SelectedTemplate = Templates.FirstOrDefault();
            _suppressTemplateApply = false;
            CurrentMnemonicsSet = MnemonicsSets.FirstOrDefault();

            MoveSelectedRightCommand = new RelayCommand(MoveSelectedToRight, CanMoveSelectedToRight);
            MoveSelectedLeftCommand = new RelayCommand(MoveSelectedToLeft, CanMoveSelectedToLeft);
            MoveAllRightCommand = new RelayCommand(_ => MoveAllToRight(), _ => _allCurves.Any(curve => curve.IsEnabled));
            MoveAllLeftCommand = new RelayCommand(_ => MoveAllToLeft(), _ => _selectedCurves.Count > 0);

            DoneCommand = new RelayCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty), _ => _selectedCurves.Count > 0);
            CancelCommand = new RelayCommand(_ => RequestCancel?.Invoke(this, EventArgs.Empty));

            SaveTemplateCommand = new RelayCommand(_ => SaveTemplate(), _ => CanSaveTemplate());
            SaveAsTemplateCommand = new RelayCommand(_ => SaveTemplateAs(), _ => CanSaveTemplateAs());
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
            if (_selectedTemplate != null && !IsNoTemplate(_selectedTemplate))
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
            };
        }

        private void ApplyMnemonicsSet()
        {
            if (_currentMnemonicsSet != null)
            {
                ApplyMnemonicsSet(_currentMnemonicsSet);
            }
        }

        private void ApplyMnemonicsSet(MnemonicsSet set)
        {
            if (set == null)
            {
                return;
            }

            foreach (var curve in _allCurves)
            {
                var setItem = set.Items?.FirstOrDefault(item =>
                    string.Equals(item.Source?.Trim(), curve.SourceName?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (setItem != null)
                {
                    curve.ExportName = setItem.Mnemonics ?? string.Empty;
                }
            }
        }

        private bool CanSaveTemplate()
        {
            return SelectedTemplate != null && !IsNoTemplate(SelectedTemplate);
        }

        private void SaveTemplate()
        {
            if (SelectedTemplate == null || IsNoTemplate(SelectedTemplate))
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
            // Re-read from disk to keep collection and selection in sync
            // with the exact serialized payload.
            template = writer.Read(template.FileName);

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
            Templates.Add(CreateNoTemplateOption());
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

        private static ExportTemplate CreateNoTemplateOption()
        {
            return new ExportTemplate
            {
                Name = NoTemplateName,
            };
        }

        private static bool IsNoTemplate(ExportTemplate template)
        {
            return template != null
                   && string.IsNullOrWhiteSpace(template.FileName)
                   && string.Equals(template.Name, NoTemplateName, StringComparison.Ordinal);
        }

        private void RefreshMnemonicsSets()
        {
            MnemonicsSets.Clear();
            EnsureMnemonicsDirectoryExists();

            var reader = new MnemonicsSetReaderWriter();
            foreach (var file in Directory.GetFiles(MnemonicsDirectoryPath, "*.xml", SearchOption.AllDirectories))
            {
                try
                {
                    var set = reader.Read(file);
                    MnemonicsSets.Add(set);
                }
                catch
                {
                    // Skip invalid mnemonics files so one malformed XML does not break the dialog.
                }
            }
        }

        private static void EnsureMnemonicsDirectoryExists()
        {
            if (!Directory.Exists(MnemonicsDirectoryPath))
            {
                Directory.CreateDirectory(MnemonicsDirectoryPath);
            }
        }


        private void RaiseCommandStates()
        {
            CommandManager.InvalidateRequerySuggested();
        }

    }
}