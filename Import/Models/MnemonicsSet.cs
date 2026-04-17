using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Models
{
    public sealed class MnemonicsLibrary
    {
    }

    [DataContract(Namespace = "")]
    public sealed class MnemonicsSetItem
    {
        [DataMember]
        public string Source { get; set; }

        [DataMember]
        public string Mnemonics { get; set; }
    }

    [DataContract(Namespace = "")]
    public sealed class MnemonicsSet
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<MnemonicsSetItem> Items { get; set; }

        public string FileName { get; set; }

        private static readonly string _emptyName = "Без библиотеки";

        public MnemonicsSet()
        {
            Items = new List<MnemonicsSetItem>();
        }

        public static MnemonicsSet GetEmpty() => new MnemonicsSet
        {
            Name = _emptyName,
            Items = new List<MnemonicsSetItem>(),
            FileName = string.Empty,
        };
        public bool IsEmpty() =>
            this == null ||
            string.IsNullOrWhiteSpace(Name) ||
            Name == _emptyName ||
            Items?.Count == 0;
    }

    public sealed class MnemonicsSetReaderWriter
    {
        public MnemonicsSet Read(string fileName)
        {
            MnemonicsSet set = null;
            using (var fs = File.OpenRead(fileName))
            {
                DataContractSerializerSettings settings = new DataContractSerializerSettings();
                DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(MnemonicsSet), settings);

                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
                xmlReaderSettings.CheckCharacters = false;

                using (XmlReader xmlReader = XmlReader.Create(fs, xmlReaderSettings))
                {
                    set = (MnemonicsSet)dataContractSerializer.ReadObject(xmlReader);
                    set.FileName = fileName;
                    xmlReader.Close();
                }
            }

            return set;
        }
    }
}