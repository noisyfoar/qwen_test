using NPFGEO.Data;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel
{
    public class TabFormatDialogLIS : ViewModelBase
    {
        public TabFormatDialogLIS(Curves selectedCurves)
        {
            SelectedCurves = selectedCurves ?? new Curves();
        }

        public Curves SelectedCurves { get; }

        public bool CanApply()
        {
            return SelectedCurves != null && SelectedCurves.Count > 0;
        }
    }
}
