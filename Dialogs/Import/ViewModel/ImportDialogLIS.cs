using NPFGEO.Data;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel
{
    public class ImportDialogLIS : ViewModelBase
    {
        private Curves _curves;
        private Curves _selectedCurves;

        private ViewModelBase _currentStage;
        public ViewModelBase CurrentStage
        {
            get => _currentStage;
            private set
            {
                _currentStage = value;
                CallPropertyChanged(nameof(CurrentStage));
                CallPropertyChanged(nameof(IsTabCurves));
                CallPropertyChanged(nameof(IsTabFormat));
            }
        }

        public ImportDialogLIS(Curves curves)
        {
            _curves = curves ?? new Curves();

            NextCommand = new NonParamRelayCommand(Next, CanNext);
            PreviousCommand = new NonParamRelayCommand(Previous, CanPrevious);

            CurrentStage = new TabCurvesDialogLIS(_curves);
        }

        public Curves AllCurves => _curves;

        // Результат импорта после Finish().
        public Curves SelectedCurves => _selectedCurves ?? new Curves();

        public NonParamRelayCommand NextCommand { get; }
        public NonParamRelayCommand PreviousCommand { get; }

        public bool IsTabCurves => CurrentStage is TabCurvesDialogLIS;
        public bool IsTabFormat => CurrentStage is TabFormatDialogLIS;

        public bool CanFinish()
        {
            return CurrentStage is TabFormatDialogLIS formatStage && formatStage.CanApply();
        }

        public void Finish()
        {
            if (CurrentStage is TabFormatDialogLIS formatStage)
            {
                _selectedCurves = formatStage.SelectedCurves;
                _curves = _selectedCurves;
            }
            else if (CurrentStage is TabCurvesDialogLIS curvesStage)
            {
                _selectedCurves = curvesStage.GetSelectedSourceCurves();
                _curves = _selectedCurves;
            }
            else
            {
                _selectedCurves = new Curves();
                _curves = _selectedCurves;
            }

            CallPropertyChanged(nameof(AllCurves));
            CallPropertyChanged(nameof(SelectedCurves));
        }

        public bool CanNext()
        {
            return CurrentStage is TabCurvesDialogLIS curvesStage && curvesStage.CanApply();
        }

        public void Next()
        {
            if (CurrentStage is TabCurvesDialogLIS curvesStage)
            {
                CurrentStage = new TabFormatDialogLIS(curvesStage.GetSelectedSourceCurves());
            }
        }

        public bool CanPrevious()
        {
            return CurrentStage is TabFormatDialogLIS;
        }

        public void Previous()
        {
            if (CurrentStage is TabFormatDialogLIS formatStage)
            {
                CurrentStage = new TabCurvesDialogLIS(AllCurves, formatStage.SelectedCurves);
            }
        }
    }
}
