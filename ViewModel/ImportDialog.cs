namespace NPFGEO.IO.LAS.Import.ViewModel
{
    public class ImportDialog : ViewModelBase
    {
        private ViewModelBase _currentStage;

        public ImportDialog()
        {
            EncodingStage = new SelectEncodingDialog();
            MetricStage = new SelectMetricDialog();
            CurvesStage = new SelectCurvesDialog();

            CurrentStage = EncodingStage;

            NextCommand = new NonParamRelayCommand(Next, CanNext);
            PreviousCommand = new NonParamRelayCommand(Previous, CanPrevious);
        }

        public SelectEncodingDialog EncodingStage { get; }
        public SelectMetricDialog MetricStage { get; }
        public SelectCurvesDialog CurvesStage { get; }

        public ViewModelBase CurrentStage
        {
            get => _currentStage;
            private set
            {
                _currentStage = value;
                CallPropertyChanged(nameof(CurrentStage));
                CallPropertyChanged(nameof(IsSelectEncoding));
                CallPropertyChanged(nameof(IsSelectMetric));
                CallPropertyChanged(nameof(IsSelectCurves));
            }
        }

        public bool IsSelectEncoding => CurrentStage is SelectEncodingDialog;
        public bool IsSelectMetric => CurrentStage is SelectMetricDialog;
        public bool IsSelectCurves => CurrentStage is SelectCurvesDialog;

        public NonParamRelayCommand NextCommand { get; }
        public NonParamRelayCommand PreviousCommand { get; }

        public bool CanNext()
        {
            return !IsSelectCurves;
        }

        public void Next()
        {
            if (IsSelectEncoding)
            {
                CurrentStage = MetricStage;
                return;
            }

            if (IsSelectMetric)
            {
                CurrentStage = CurvesStage;
            }
        }

        public bool CanPrevious()
        {
            return !IsSelectEncoding;
        }

        public void Previous()
        {
            if (IsSelectCurves)
            {
                CurrentStage = MetricStage;
                return;
            }

            if (IsSelectMetric)
            {
                CurrentStage = EncodingStage;
            }
        }

        public bool CanFinish()
        {
            return IsSelectCurves && CurvesStage.CanApply();
        }

        public void Finish()
        {
            // Заглушка: здесь будет итоговая сборка результата импорта.
        }
    }
}
