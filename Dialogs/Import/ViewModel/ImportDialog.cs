namespace ShellExtension.Formats.LIS.Dialogs.Import.ViewModel
{
    public class ImportDialog : ViewModelBase
    {
        private ViewModelBase _currentStage;

        public ImportDialog()
        {
            CurvesStage = new SelectCurvesDialog();
            RenameStage = new SelectRenameDialog();

            CurrentStage = CurvesStage;

            NextCommand = new NonParamRelayCommand(Next, CanNext);
            PreviousCommand = new NonParamRelayCommand(Previous, CanPrevious);
        }

        public SelectCurvesDialog CurvesStage { get; }
        public SelectRenameDialog RenameStage { get; }

        public ViewModelBase CurrentStage
        {
            get => _currentStage;
            private set
            {
                _currentStage = value;
                CallPropertyChanged(nameof(CurrentStage));
                CallPropertyChanged(nameof(IsSelectCurves));
                CallPropertyChanged(nameof(IsSelectRename));
            }
        }

        public bool IsSelectCurves => CurrentStage is SelectCurvesDialog;
        public bool IsSelectRename => CurrentStage is SelectRenameDialog;

        public NonParamRelayCommand NextCommand { get; }
        public NonParamRelayCommand PreviousCommand { get; }

        public bool CanNext()
        {
            return IsSelectCurves && CurvesStage.CanApply();
        }

        public void Next()
        {
            if (IsSelectCurves)
            {
                CurrentStage = RenameStage;
            }
        }

        public bool CanPrevious()
        {
            return IsSelectRename;
        }

        public void Previous()
        {
            if (IsSelectRename)
            {
                CurrentStage = CurvesStage;
            }
        }

        public bool CanFinish()
        {
            return IsSelectRename && RenameStage.CanApply();
        }

        public void Finish()
        {
            // Заглушка: на этом шаге можно применить RenameStage к CurvesStage.Selected.
        }
    }
}
