using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using NPFGEO.Data;
using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Export;
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
        private const string NoMnemonicsSetName = "Без библиотеки";

        private string _curveFilter = string.Empty;
        private double? _start;
        private double? _stop;
        private ParameterTable _selectedParameterTable;
        private ExportTemplate _selectedTemplate;
        private MnemonicsSet _currentMnemonicsSet;
        private bool _suppressTemplateApply;
        private readonly NonInterpolator _interpolatorNone = new NonInterpolator();
        private readonly LinearInterpolator _interpolatorLinear = new LinearInterpolator();
        private readonly NextNeighborInterpolator _interpolatorNextNeighbor = new NextNeighborInterpolator();
        private IInterpolator _currentInterpolator;

        public ICollectionView AvailableCurvesView => _availableCurvesView;

        public ObservableCollection<LISCurveItem> SelectedCurves => _selectedCurves;

        public ObservableCollection<ParameterTable> ParameterTables => _parameterTables;
        public ObservableCollection<ExportTemplate> Templates { get; }
        public ObservableCollection<MnemonicsSet> MnemonicsSets { get; }

        public IReadOnlyList<IInterpolator> Interpolators { get; }

        public IInterpolator CurrentInterpolator
        {
            get => _currentInterpolator;
            set
            {
                if (ReferenceEquals(_currentInterpolator, value))
                {
                    return;
                }

                _currentInterpolator = value;
                CallPropertyChanged(nameof(CurrentInterpolator));
            }
        }

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

        public double? Start
        {
            get => _start;
            set
            {
                if (_start == value)
                {
                    return;
                }

                _start = value;
                CallPropertyChanged(nameof(Start));
                RaiseCommandStates();
            }
        }

        public double? Stop
        {
            get => _stop;
            set
            {
                if (_stop == value)
                {
                    return;
                }

                _stop = value;
                CallPropertyChanged(nameof(Stop));
                RaiseCommandStates();
            }
        }

        public ICommand MoveSelectedRightCommand { get; }
        public ICommand MoveSelectedLeftCommand { get; }
        public ICommand MoveAllRightCommand { get; }
        public ICommand MoveAllLeftCommand { get; }
        public ICommand UseFullRangeCommand { get; }
        public ICommand FixAllRangeCommand { get; }
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
            Interpolators = new[]
            {
                _interpolatorNone,
                _interpolatorLinear,
                _interpolatorNextNeighbor,
            };
            _currentInterpolator = _interpolatorNone;
            RefreshTemplates();
            RefreshMnemonicsSets();
            _suppressTemplateApply = true;
            SelectedTemplate = Templates.FirstOrDefault();
            _suppressTemplateApply = false;
            CurrentMnemonicsSet = MnemonicsSets.FirstOrDefault();
            UseFullRange();

            MoveSelectedRightCommand = new RelayCommand(MoveSelectedToRight, CanMoveSelectedToRight);
            MoveSelectedLeftCommand = new RelayCommand(MoveSelectedToLeft, CanMoveSelectedToLeft);
            MoveAllRightCommand = new RelayCommand(_ => MoveAllToRight(), _ => _allCurves.Any(curve => curve.IsEnabled));
            MoveAllLeftCommand = new RelayCommand(_ => MoveAllToLeft(), _ => _selectedCurves.Count > 0);
            UseFullRangeCommand = new RelayCommand(_ => UseFullRange(), _ => CanUseFullRange());
            FixAllRangeCommand = new RelayCommand(_ => FixAllRange(), _ => CanFixAllRange());

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

        private bool CanUseFullRange()
        {
            return GetRangeSourceCurves()
                .Select(item => item.Source)
                .Any(curve => curve != null && curve.DepthMatrix != null && curve.DepthMatrix.Count > 0);
        }

        private void UseFullRange()
        {
            var sourceCurves = GetRangeSourceCurves()
                .Select(item => item.Source);

            UpdateBorders(sourceCurves);
        }

        private void UpdateBorders(IEnumerable<Curve> curves)
        {
            var min = double.MaxValue;
            var max = double.MinValue;

            foreach (var curve in curves ?? Enumerable.Empty<Curve>())
            {
                if (curve == null || curve.DepthMatrix == null || curve.DepthMatrix.Count == 0)
                {
                    continue;
                }

                var roof = curve.Roof;
                var foot = curve.Foot;

                if (roof < min)
                {
                    min = roof;
                }

                if (foot > max)
                {
                    max = foot;
                }
            }

            if (min == double.MaxValue && max == double.MinValue)
            {
                Start = 0.0;
                Stop = 1000.0;
            }
            else
            {
                Start = Math.Floor(min * 10.0) / 10.0;
                Stop = Math.Floor(max * 10.0) / 10.0;
            }
        }

        private bool CanFixAllRange()
        {
            return Start != null && Stop != null;
        }

        private void FixAllRange()
        {
            if (Start == null || Stop == null)
            {
                return;
            }

            var from = Math.Floor(Start.Value * 10.0) / 10.0;
            var to = Math.Floor(Stop.Value * 10.0) / 10.0;

            if (from <= to)
            {
                Start = from;
                Stop = to;
                return;
            }

            // Keep range valid for users who entered reversed values.
            Start = to;
            Stop = from;
        }

        private IEnumerable<LISCurveItem> GetRangeSourceCurves()
        {
            if (_selectedCurves.Count > 0)
            {
                return _selectedCurves;
            }

            return _allCurves;
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
            if (_currentMnemonicsSet != null && !IsNoMnemonicsSet(_currentMnemonicsSet))
            {
                ApplyMnemonicsSet(_currentMnemonicsSet);
            }
        }

        private void ApplyMnemonicsSet(MnemonicsSet set)
        {
            if (set == null || IsNoMnemonicsSet(set))
            {
                return;
            }

            var map = (set.Items ?? Enumerable.Empty<MnemonicsSetItem>())
                .Where(item => !string.IsNullOrWhiteSpace(item.Source))
                .GroupBy(item => item.Source.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var curve in _allCurves)
            {
                if (string.IsNullOrWhiteSpace(curve.SourceName))
                {
                    continue;
                }

                if (map.TryGetValue(curve.SourceName.Trim(), out var setItem))
                {
                    curve.ExportName = setItem?.Mnemonics ?? string.Empty;
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
            MnemonicsSets.Add(CreateNoMnemonicsSetOption());
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

        private static MnemonicsSet CreateNoMnemonicsSetOption()
        {
            return new MnemonicsSet
            {
                Name = NoMnemonicsSetName,
            };
        }

        private static bool IsNoMnemonicsSet(MnemonicsSet set)
        {
            return set != null
                   && string.IsNullOrWhiteSpace(set.FileName)
                   && string.Equals(set.Name, NoMnemonicsSetName, StringComparison.Ordinal);
        }


        private void RaiseCommandStates()
        {
            CommandManager.InvalidateRequerySuggested();
        }

    }
}