using NPFGEO.Data;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel
{
    public class ImportDialogLIS : ViewModelBase
    {
        private Curves _curves;
        private Curves _selectedCurves;
        bool _rename = false;
        bool _preOrPost = false;
        string _add = "";
        
        public ImportDialogLIS(Curves curves)
        {
            _curves = curves;


            NextCommand = new NonParamRelayCommand(Next, CanNext);
            PreviousCommand = new NonParamRelayCommand(Previous, CanPrevious);
        }
        public ViewModelBase CurrentStage { set; get; }


        public Curves AllCurves
        {
            get { return _curves; }
        }

        public NonParamRelayCommand NextCommand { private set; get; }
        public NonParamRelayCommand PreviousCommand { private set; get; }

        public bool IsTabCurves { get { return CurrentStage is TabCurvesDialogLIS; } }
        public bool IsTabFormat { get { return CurrentStage is TabFormatDialogLIS; } }

        void UpdateStageFlags()
        {
            CallPropertyChanged("IsTabCurves");
            CallPropertyChanged("IsTabFormat");
        }
        public bool CanFinish()
        {
            return CurrentStage is TabFormatDialogLIS && (CurrentStage as TabFormatDialogLIS).CanApply();
        }

        public void Finish()
        {
            _curves = null;
        }

        public bool CanNext()
        {
            return !(CurrentStage is TabFormatDialogLIS);
        }
        public void Next()
        {
            if(CurrentStage is TabCurvesDialogLIS)
            {
                NextToTabFormat(CurrentStage as TabCurvesDialogLIS);
            }
            UpdateStageFlags();
        }
        private void NextToTabFormat(TabCurvesDialogLIS prev)
        {

            
            CallPropertyChanged("CurrentStage");
        }

        public bool CanPrevious()
        {
            return !(CurrentStage is TabCurvesDialogLIS);
        }

        public void Previous()
        {
            if(CurrentStage is TabFormatDialogLIS)
            {
                PreviousToTabCurves();
            }
            UpdateStageFlags();
        }
        private void PreviousToTabCurves()
        {
            // clear the data

            CurrentStage = new TabCurvesDialogLIS(AllCurves);
            CallPropertyChanged("CurrentStage");
        }
        

    }
}
