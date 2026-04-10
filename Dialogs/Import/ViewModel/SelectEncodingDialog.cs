using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace NPFGEO.IO.LAS.Import.ViewModel
{
    public class SelectEncodingDialog : ViewModelBase
    {
        private readonly string _rawSampleText;
        private EncodingItem _currentEncoder;
        private string _textExample;

        public SelectEncodingDialog(string rawSampleText = "")
        {
            _rawSampleText = rawSampleText ?? string.Empty;

            Encoders = new ObservableCollection<EncodingItem>
            {
                new EncodingItem("Win-1251", Encoding.GetEncoding(1251)),
                new EncodingItem("Cyr-866", Encoding.GetEncoding(866)),
                new EncodingItem("KOI8-R", Encoding.GetEncoding("koi8-r")),
                new EncodingItem("UTF-8", Encoding.UTF8)
            };

            SetWin1251Command = new NonParamRelayCommand(SetWin1251, () => true);
            SetCyr866Command = new NonParamRelayCommand(SetCyr866, () => true);
            SetKOI8RCommand = new NonParamRelayCommand(SetKOI8R, () => true);
            SetUTF8Command = new NonParamRelayCommand(SetUTF8, () => true);

            CurrentEncoder = Encoders[0];
        }

        public ObservableCollection<EncodingItem> Encoders { get; }

        public EncodingItem CurrentEncoder
        {
            get => _currentEncoder;
            set
            {
                _currentEncoder = value;
                CallPropertyChanged(nameof(CurrentEncoder));
                UpdateTextPreview();
            }
        }

        public string TextExample
        {
            get => _textExample;
            private set
            {
                _textExample = value;
                CallPropertyChanged(nameof(TextExample));
            }
        }

        public ICommand SetWin1251Command { get; }
        public ICommand SetCyr866Command { get; }
        public ICommand SetKOI8RCommand { get; }
        public ICommand SetUTF8Command { get; }

        public bool CanApply()
        {
            return CurrentEncoder != null;
        }

        private void SetWin1251()
        {
            CurrentEncoder = Encoders[0];
        }

        private void SetCyr866()
        {
            CurrentEncoder = Encoders[1];
        }

        private void SetKOI8R()
        {
            CurrentEncoder = Encoders[2];
        }

        private void SetUTF8()
        {
            CurrentEncoder = Encoders[3];
        }

        private void UpdateTextPreview()
        {
            if (CurrentEncoder == null)
            {
                TextExample = string.Empty;
                return;
            }

            TextExample = _rawSampleText;
        }

        public class EncodingItem
        {
            public EncodingItem(string displayName, Encoding encoding)
            {
                DisplayName = displayName;
                Encoding = encoding;
            }

            public string DisplayName { get; }
            public string Name => Encoding?.WebName ?? string.Empty;
            public Encoding Encoding { get; }
        }
    }
}
