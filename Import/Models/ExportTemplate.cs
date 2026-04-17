using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models
{
    [DataContract(Namespace = "")]
    public sealed class ExportTemplateItem
    {
        [DataMember]
        public string SourceName { get; set; }
        [DataMember]
        public int    Precision { get; set; }

        [DataMember]
        public string ExportName { get; set; }

        [DataMember]
        public string Description { get; set; }
    }

    [DataContract(Namespace = "")]
    public sealed class ExportTemplate
    {
        [DataMember]
        public string Name { get; set; }

        public string FileName { get; set; }

        [DataMember]
        public ObservableCollection<ExportTemplateItem> Items { get; set; }

        private static readonly string _emptyName = "Без шаблона";
        public static ExportTemplate GetEmpty() => new ExportTemplate
        { 
            Name = _emptyName, 
            FileName = String.Empty,
            Items = new ObservableCollection<ExportTemplateItem>()
        };

        public bool IsEmpty() =>
            this == null ||
            string.IsNullOrWhiteSpace(Name) ||
            Name == _emptyName ||
            Items?.Count == 0;

        public ExportTemplate()
        {
            Items = new ObservableCollection<ExportTemplateItem>();
        }

        public ExportTemplate Clone() => new ExportTemplate
        {
            Name = this.Name,
            FileName = this.FileName,
            Items = new ObservableCollection<ExportTemplateItem>(this.Items ?? Enumerable.Empty<ExportTemplateItem>())
        };
    } 

    public sealed class ExportTemplateReaderWriter
    {
        public ExportTemplate Read(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Template file name is not specified.", nameof(fileName));
            }

            using (var fs = File.OpenRead(fileName))
            {
                var serializer = new DataContractSerializer(typeof(ExportTemplate), new DataContractSerializerSettings());
                var readerSettings = new XmlReaderSettings
                {
                    CheckCharacters = false,
                };

                using (var xmlReader = XmlReader.Create(fs, readerSettings))
                {
                    var template = (ExportTemplate)serializer.ReadObject(xmlReader);
                    template.FileName = fileName;
                    return template;
                }
            }
        }

        public void Write(string fileName, ExportTemplate template)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Template file name is not specified.", nameof(fileName));
            }

            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            using (var fs = File.Create(fileName))
            {
                var serializer = new DataContractSerializer(
                    typeof(ExportTemplate),
                    new DataContractSerializerSettings
                    {
                        PreserveObjectReferences = false,
                        MaxItemsInObjectGraph = int.MaxValue,
                    });

                var writerSettings = new XmlWriterSettings
                {
                    Indent = true,
                    CheckCharacters = false,
                    NewLineHandling = NewLineHandling.Entitize,
                    Encoding = Encoding.Unicode,
                };

                using (var xmlWriter = XmlWriter.Create(fs, writerSettings))
                {
                    serializer.WriteObject(xmlWriter, template);
                    xmlWriter.Flush();
                }
            }
        }
    }
}