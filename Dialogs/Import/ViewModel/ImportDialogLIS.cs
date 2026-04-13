using System.Collections.Generic;
using System.Linq;

namespace ShellExtension.Formats.LIS.Dialogs.Import.ViewModel
{
    public class ImportDialogLIS : ViewModelBase
    {
        private ViewModelBase _currentStage;
        private List<TabCurvesDialogLIS.Item> _selectedCurvesSnapshot = new List<TabCurvesDialogLIS.Item>();

        public ImportDialogLIS()
        {
            CurvesStage = new TabCurvesDialogLIS();
            RenameStage = new TabRenameDialogLIS();

            CurrentStage = CurvesStage;

            NextCommand = new NonParamRelayCommand(Next, CanNext);
            PreviousCommand = new NonParamRelayCommand(Previous, CanPrevious);
        }

        public TabCurvesDialogLIS CurvesStage { get; }
        public TabRenameDialogLIS RenameStage { get; }

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

        public bool IsSelectCurves => CurrentStage is TabCurvesDialogLIS;
        public bool IsSelectRename => CurrentStage is TabRenameDialogLIS;

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
                _selectedCurvesSnapshot = CurvesStage.GetSelectedItems().ToList();
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
                CurvesStage.RestoreSelectedItems(_selectedCurvesSnapshot);
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
