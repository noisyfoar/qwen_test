using Microsoft.Win32;
using NPFGEO.Data;
using NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        private string _curveFilter = string.Empty;
        private ParameterTable _selectedParameterTable;
        private ExportTemplate _selectedTemplate;
        private MnemonicsSet _currentMnemonicsSet;
        private IInterpolator _currentInterpolator;

        public ICollectionView AvailableCurvesView => _availableCurvesView;

        public ObservableCollection<LISCurveItem> SelectedCurves => _selectedCurves;

        public ObservableCollection<ParameterTable> ParameterTables => _parameterTables;
        public ObservableCollection<ExportTemplate> Templates { get; }
        public ObservableCollection<MnemonicsSet> MnemonicsSets { get; }
        public IList<IInterpolator> Interpolators { get; }

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
                ApplyTemplate();
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
        public ICommand UseFullRangeCommand { get; }

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
            Interpolators = new List<IInterpolator> { new NonInterpolator(), new LinearInterpolator(), new NextNeighborInterpolator() };

            RefreshTemplates();
            RefreshMnemonicsSets();

            SelectedTemplate = Templates.FirstOrDefault();
            CurrentMnemonicsSet = MnemonicsSets.FirstOrDefault(); 
            CurrentInterpolator = Interpolators.FirstOrDefault();

            UseFullRange();

            MoveSelectedRightCommand = new RelayCommand(MoveSelectedToRight, CanMoveSelectedToRight);
            MoveSelectedLeftCommand = new RelayCommand(MoveSelectedToLeft, CanMoveSelectedToLeft);
            MoveAllRightCommand = new RelayCommand(_ => MoveAllToRight(), _ => _allCurves.Any(curve => curve.IsEnabled));
            MoveAllLeftCommand = new RelayCommand(_ => MoveAllToLeft(), _ => _selectedCurves.Count > 0);

            SaveTemplateCommand = new RelayCommand(_ => SaveTemplate(), _ => CanSaveTemplate());
            SaveAsTemplateCommand = new RelayCommand(_ => SaveTemplateAs(), _ => CanSaveTemplateAs());

            UseFullRangeCommand = new RelayCommand(_ => UseFullRange(), _ => CanUseFullRange());
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
        }

        private void MoveAllToRight()
        {
            var items = _allCurves.Where(curve => curve.IsEnabled).ToList();
            foreach (var curve in items)
            {
                AddCurve(curve);
            }

            _availableCurvesView.Refresh();
        }

        private void MoveAllToLeft()
        {
            var items = _selectedCurves.ToList();
            foreach (var selected in items)
            {
                RemoveCurve(selected);
            }

            _availableCurvesView.Refresh();
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
        }

        private void RemoveCurve(LISCurveItem item)
        {
            if (item == null)
            {
                return;
            }

            item.IsEnabled = true;
            _selectedCurves.Remove(item);
        }

        private static List<LISCurveItem> GetSelectedItems(object parameter)
        {
            List<LISCurveItem> result = new List<LISCurveItem>();

            if (!(parameter is IEnumerable selectedItems))
            {
                return result;
            }

            foreach (object item in selectedItems)
            {
                LISCurveItem curve = item as LISCurveItem;
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
            if (template.IsEmpty())
            {
                return;
            }

            MoveAllToLeft();
            foreach (ExportTemplateItem item in template.Items ?? Enumerable.Empty<ExportTemplateItem>())
            {
                LISCurveItem source = _allCurves.FirstOrDefault(curve =>
                    string.Equals(curve.SourceName?.Trim(), item.SourceName?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (source == null)
                {
                    continue;
                }

                AddCurve(source);
            }

            _availableCurvesView.Refresh();
        }

        private ExportTemplate ToTemplate()
        {
            ExportTemplate template = new ExportTemplate();
            foreach (LISCurveItem item in _selectedCurves)
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
                Precision = item.Precision,
                ExportName = item.ExportName,
                Description = item.Description,
            };
        }

        private bool CanSaveTemplate()
        {
            return SelectedTemplate != null && !SelectedTemplate.IsEmpty();
        }

        private void SaveTemplate()
        {
            if (SelectedTemplate == null || SelectedTemplate.IsEmpty())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedTemplate.FileName))
            {
                SaveTemplateAs();
                return;
            }

            ExportTemplateReaderWriter writer = new ExportTemplateReaderWriter();
            ExportTemplate template = ToTemplate();
            template.FileName = SelectedTemplate.FileName;
            template.Name = SelectedTemplate.Name;
            writer.Write(template.FileName, template);

            int index = Templates.IndexOf(SelectedTemplate);
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

            SaveFileDialog saveFileDialog = new SaveFileDialog
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

            string fullName = saveFileDialog.FileName;
            ExportTemplate template = ToTemplate();
            template.Name = Path.GetFileNameWithoutExtension(fullName);
            template.FileName = fullName;

            ExportTemplateReaderWriter writer = new ExportTemplateReaderWriter();
            writer.Write(fullName, template);

            ExportTemplate replaced = Templates.FirstOrDefault(existing =>
                string.Equals(existing.FileName, fullName, StringComparison.OrdinalIgnoreCase));

            if (replaced != null)
            {
                int index = Templates.IndexOf(replaced);
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
            Templates.Add(ExportTemplate.GetEmpty());
            EnsureTemplatesDirectoryExists();

            ExportTemplateReaderWriter reader = new ExportTemplateReaderWriter();
            foreach (string file in Directory.GetFiles(TemplatesDirectoryPath, "*.xml", SearchOption.AllDirectories))
            {
                try
                {
                    ExportTemplate template = reader.Read(file);
                    Templates.Add(template);
                }
                catch
                {
                    // Skip invalid template files so one malformed XML does not break the dialog.
                }
            }
        }

        private void ApplyMnemonicsSet()
        {
            if (!CurrentMnemonicsSet.IsEmpty())
            {
                ApplyMnemonicsSet(_currentMnemonicsSet);
            }
        }
        private void ApplyMnemonicsSet(MnemonicsSet mnemonicsSet)
        {
            if (mnemonicsSet.IsEmpty())
            {
                return;
            }

            foreach (LISCurveItem CurveItem in AvailableCurvesView)
            {
                MnemonicsSetItem setItem = mnemonicsSet.Items?.FirstOrDefault
                    (
                        item => string.Equals
                        (
                            item.Source?.Trim(),
                            CurveItem.SourceName?.Trim(),
                            StringComparison.OrdinalIgnoreCase
                        )
                    );
                if (setItem != null)
                {
                    CurveItem.ExportName = setItem.Mnemonics ?? string.Empty;
                }
            }
        }
        private void RefreshMnemonicsSets()
        {
            MnemonicsSets.Clear();
            MnemonicsSets.Add(MnemonicsSet.GetEmpty());
            EnsureTemplatesDirectoryExists();
            MnemonicsSetReaderWriter reader = new MnemonicsSetReaderWriter();

            string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Genesis\Mnemonics sets";
            string[] files = Directory
                .GetFiles(dir, "*.*", SearchOption.AllDirectories)
                .Where(s => Path.GetExtension(s).TrimStart('.').ToLowerInvariant() == "xml")
                .ToArray();

            foreach (string file in files)
            {
                try
                {
                    MnemonicsSet set = reader.Read(file);
                    MnemonicsSets.Add(set);
                }
                catch
                {

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


        private double _start;
        private double _stop;

        public double Start
        {
            get => _start;
            set
            {
                if (_start != value)
                {
                    _start = value;
                    CallPropertyChanged(nameof(Start));
                }
            }
        }
        public double Stop
        {
            get => _stop;
            set
            {
                if (_stop != value)
                {
                    _stop = value;
                    CallPropertyChanged(nameof(Stop));
                }
            }
        }
        public void UpdateBorders(IEnumerable<Curve> curves)
        {
            double min, currentMin;
            double max , currentMax;
            min = double.MaxValue;
            max = double.MinValue;

            foreach (Curve curve in curves)
            {
                if (curve.DepthMatrix == null) continue;
                if (curve.DepthMatrix.Count == 0) continue;

                currentMin = curve.Roof;
                currentMax = curve.Foot;

                min = currentMin < min ? currentMin : min;
                max = currentMax > max ? currentMax : max;
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
        private bool CanUseFullRange()
        {
            return _allCurves.Select(item => item.Source).Any(curve => curve != null && curve.DepthMatrix != null && curve.DepthMatrix.Count > 0);
        }
        private void UseFullRange()
        {
            IEnumerable<Curve> sourceCurves = _allCurves.Select(item => item.Source);
            UpdateBorders(sourceCurves);
        }


        public IInterpolator CurrentInterpolator
        {
            get => _currentInterpolator;
            set
            {
                if (Equals(_currentInterpolator, value))
                {
                    return;
                }

                _currentInterpolator = value;
                CallPropertyChanged(nameof(CurrentInterpolator));
            }
        }
    }
}