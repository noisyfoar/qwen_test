namespace NPFGEO.IO.LAS.Import.ViewModel
{
    public class SelectRenameDialog : ViewModelBase
    {
        private RenameMode _mode;
        private string _prefix = string.Empty;
        private string _postfix = string.Empty;

        public SelectRenameDialog()
        {
            Mode = RenameMode.None;
        }

        public RenameMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                CallPropertyChanged(nameof(Mode));
                CallPropertyChanged(nameof(IsNoRename));
                CallPropertyChanged(nameof(IsPrefix));
                CallPropertyChanged(nameof(IsPostfix));
                CallPropertyChanged(nameof(NeedAffixValue));
                CallPropertyChanged(nameof(PreviewText));
            }
        }

        public string Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value ?? string.Empty;
                CallPropertyChanged(nameof(Prefix));
                CallPropertyChanged(nameof(PreviewText));
            }
        }

        public string Postfix
        {
            get => _postfix;
            set
            {
                _postfix = value ?? string.Empty;
                CallPropertyChanged(nameof(Postfix));
                CallPropertyChanged(nameof(PreviewText));
            }
        }

        public string PreviewText
        {
            get
            {
                const string sampleCurveName = "GR";

                switch (Mode)
                {
                    case RenameMode.Prefix:
                        return $"{Prefix}{sampleCurveName}";
                    case RenameMode.Postfix:
                        return $"{sampleCurveName}{Postfix}";
                    default:
                        return sampleCurveName;
                }
            }
        }

        public bool IsNoRename
        {
            get => Mode == RenameMode.None;
            set
            {
                if (value)
                    Mode = RenameMode.None;
            }
        }

        public bool IsPrefix
        {
            get => Mode == RenameMode.Prefix;
            set
            {
                if (value)
                    Mode = RenameMode.Prefix;
            }
        }

        public bool IsPostfix
        {
            get => Mode == RenameMode.Postfix;
            set
            {
                if (value)
                    Mode = RenameMode.Postfix;
            }
        }

        public bool NeedAffixValue => Mode == RenameMode.Prefix || Mode == RenameMode.Postfix;

        public bool CanApply()
        {
            if (!NeedAffixValue)
                return true;

            return Mode == RenameMode.Prefix
                ? !string.IsNullOrWhiteSpace(Prefix)
                : !string.IsNullOrWhiteSpace(Postfix);
        }
    }

    public enum RenameMode
    {
        None = 0,
        Prefix = 1,
        Postfix = 2
    }
}
