
        #region Мнемоники
        MnemonicsSet _currentMnemonicsSet;
        public MnemonicsSet CurrentMnemonicsSet
        {
            set
            {
                _currentMnemonicsSet = value;
                CallPropertyChanged("CurrentMnemonicsSet");
                ApplyMnemonicsSet();
            }
            get { return _currentMnemonicsSet; }
        }

        public ObservableCollection<MnemonicsSet> MnemonicsSets { private set; get; } = new ObservableCollection<MnemonicsSet>();
        void ApplyMnemonicsSet()
        {
            if (_currentMnemonicsSet != null)
            {
                ApplyMnemonicsSet(_currentMnemonicsSet);
            }
        }
        void ApplyMnemonicsSet(MnemonicsSet set)
        {
            foreach (var item in AvailableCurves)
            {
                var setItem = set.Items.FirstOrDefault(a => a.Source == item.SourceName);
                if (setItem != null)
                {
                    item.ExportName = setItem.Mnemonics;
                }
            }
        }

        #endregion
        #region Шаблоны кривых
        private ExportTemplate _selectedTemplate;
        public ExportTemplate SelectedTemplate
        {
            set
            {
                _selectedTemplate = value;
                CallPropertyChanged("SelectedTemplate");
                ApplyTemplate();
            }
            get { return _selectedTemplate; }
        }
        public ObservableCollection<ExportTemplate> Templates { get; }

        void ApplyTemplate()
        {
            if (_selectedTemplate != null)
            {
                ApplyTemplate(_selectedTemplate);
            }
        }

        void ApplyTemplate(ExportTemplate template)
        {
            UnselectAll();
            foreach (var item in template.Items)
            {
                var source = AvailableCurves.FirstOrDefault(a => a.SourceName == item.SourceName);
                if (source != null)
                {
                    AddCurve(source);
                    source.ExportName = item.ExportName;
                    source.Precision = item.Precision;
                    source.Description = item.Description;
                }
            }
        }

        ExportTemplate ToTemplate()
        {
            var template = new ExportTemplate();

            foreach (var item in SelectedCurves.OfType<Item>())
            {
                var outItem = ToTemplateItem(item);
                template.Items.Add(outItem);
            }

            return template;
        }

        ExportTemplateItem ToTemplateItem(Item item)
        {
            var result = new ExportTemplateItem();
            result.SourceName = item.SourceName;
            result.ExportName = item.ExportName;
            result.Description = item.Description;
            result.Precision = item.Precision;
            return result;
        }
        public NonParamRelayCommand SaveTemplateCommand { private set; get; }
        bool CanSaveTemplate()
        {
            return SelectedTemplate != null;
        }

        void SaveTemplate()
        {
            var writer = new ExportTemplateReaderWriter();

            var template = ToTemplate();
            template.FileName = SelectedTemplate.FileName;
            template.Name = SelectedTemplate.Name;
            writer.Write(SelectedTemplate.FileName, template);

            int index = Templates.IndexOf(SelectedTemplate);
            Templates[index] = template;
            SelectedTemplate = template;
        }
        public NonParamRelayCommand SaveAsTemplateCommand { private set; get; }
        bool CanSaveTemplateAs()
        {
            return SelectedCurves.Count > 0;
        }

        public void SaveTemplateAs()
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Genesis\Export templates";
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = dir;
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.Filter = "XML File(*.xml)|*.xml";
            var result = saveFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                var fullName = saveFileDialog.FileName;

                var template = ToTemplate();
                template.Name = System.IO.Path.GetFileNameWithoutExtension(fullName);
                template.FileName = fullName;

                var writer = new ExportTemplateReaderWriter();
                writer.Write(fullName, template);

                var replaced = Templates.FirstOrDefault(a => a.FileName == fullName);
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
        }

        void RefreshMnemonicsSets()
        {
            MnemonicsSets.Clear();

            var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Genesis\Mnemonics sets";

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var files = Directory
                .GetFiles(dir, "*.*", SearchOption.AllDirectories)
                .Where(s => Path.GetExtension(s).TrimStart('.').ToLowerInvariant() == "xml")
                .ToArray();

            var reader = new MnemonicsSetReaderWriter();
            foreach (var file in files)
            {
                try
                {
                    var mnemSet = reader.Read(file);
                    MnemonicsSets.Add(mnemSet);
                }
                catch (Exception ex) { }
            }
        }

        void RefreshTemplates()
        {
            Templates.Clear();

            var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Genesis\Export templates";

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var files = Directory
                .GetFiles(dir, "*.*", SearchOption.AllDirectories)
                .Where(s => Path.GetExtension(s).TrimStart('.').ToLowerInvariant() == "xml")
                .ToArray();

            var reader = new ExportTemplateReaderWriter();
            foreach (var file in files)
            {
                try
                {
                    var template = reader.Read(file);
                    Templates.Add(template);
                }
                catch (Exception ex) { }
            }
        }
        #endregion

        #region Шаблоны настроек экспорта

        private ExportSettingsTemplate _selectedSettingsTemplate;
        public ExportSettingsTemplate SelectedSettingsTemplate
        {
            set
            {
                _selectedSettingsTemplate = value;
                CallPropertyChanged(nameof(SelectedSettingsTemplate));
                ApplySettingsTemplate();
            }
            get { return _selectedSettingsTemplate; }
        }
        public ObservableCollection<ExportSettingsTemplate> SettingsTemplates { get; }

        void ApplySettingsTemplate()
        {
            if (_selectedSettingsTemplate != null)
            {
                ApplySettingsTemplate(_selectedSettingsTemplate);
            }
        }
        void ApplySettingsTemplate(ExportSettingsTemplate template)
        {
            ExportSettingsTemplateConverter templateConverter = new ExportSettingsTemplateConverter();
            templateConverter.ApplyExportSettingsTemplate(this, template);
        }

        public NonParamRelayCommand SaveSettingsTemplateCommand { private set; get; }
        bool CanSaveSettingsTemplate()
        {
            return SelectedSettingsTemplate != null;
        }

        void SaveSettingsTemplate()
        {
            var writer = new ExportSettingsTemplateReaderWriter();
            ExportSettingsTemplateConverter templateConverter = new ExportSettingsTemplateConverter();

            var template = templateConverter.GetExportSettingsTemplate(this);
            template.FileName = SelectedSettingsTemplate.FileName;
            template.Name = SelectedSettingsTemplate.Name;
            writer.Write(SelectedSettingsTemplate.FileName, template);

            int index = SettingsTemplates.IndexOf(SelectedSettingsTemplate);
            SettingsTemplates[index] = template;
            SelectedSettingsTemplate = template;
        }
        public NonParamRelayCommand SaveAsSettingsTemplateCommand { private set; get; }
        bool CanSaveSettingsTemplateAs()
        {
            return true;
        }

        public void SaveSettingsTemplateAs()
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Genesis\Export settings templates";
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = dir;
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.Filter = "XML File(*.xml)|*.xml";
            var result = saveFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                var fullName = saveFileDialog.FileName;

                ExportSettingsTemplateConverter templateConverter = new ExportSettingsTemplateConverter();
                var template = templateConverter.GetExportSettingsTemplate(this);
                template.Name = System.IO.Path.GetFileNameWithoutExtension(fullName);
                template.FileName = fullName;

                var writer = new ExportSettingsTemplateReaderWriter();
                writer.Write(fullName, template);

                var replaced = SettingsTemplates.FirstOrDefault(a => a.FileName == fullName);
                if (replaced != null)
                {
                    int index = SettingsTemplates.IndexOf(replaced);
                    SettingsTemplates[index] = template;
                }
                else
                {
                    SettingsTemplates.Add(template);
                }

                SelectedSettingsTemplate = template;
            }
        }
        void RefreshSettingsTemplates()
        {
            SettingsTemplates.Clear();

            var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Genesis\Export settings templates";

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var files = Directory
                .GetFiles(dir, "*.*", SearchOption.AllDirectories)
                .Where(s => Path.GetExtension(s).TrimStart('.').ToLowerInvariant() == "xml")
                .ToArray();

            var reader = new ExportSettingsTemplateReaderWriter();
            foreach (var file in files)
            {
                try
                {
                    var template = reader.Read(file);
                    SettingsTemplates.Add(template);
                }
                catch (Exception ex) { }
            }
        }

        #endregion