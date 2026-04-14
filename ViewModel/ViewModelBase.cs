using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void CallPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
