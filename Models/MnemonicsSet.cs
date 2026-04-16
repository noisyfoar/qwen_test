using System;
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
        public List<MnemonicsSetItem> Items { get; set; } = new List<MnemonicsSetItem>();

        public string FileName { get; set; }
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

        public void Write(string fileName, MnemonicsSet set)
        {
            using (var fs = File.Create(fileName))
            {
                DataContractSerializerSettings settings = new DataContractSerializerSettings();
                settings.PreserveObjectReferences = false;
                settings.MaxItemsInObjectGraph = int.MaxValue;
                DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(MnemonicsSet), settings);

                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                xmlWriterSettings.Indent = true;
                xmlWriterSettings.CheckCharacters = false;
                xmlWriterSettings.NewLineHandling = NewLineHandling.Entitize;
                xmlWriterSettings.Encoding = Encoding.Unicode;

                XmlWriter xmlWriter = XmlWriter.Create(fs, xmlWriterSettings);
                dataContractSerializer.WriteObject(xmlWriter, set);
                xmlWriter.Flush();
            }
        }
    }
}
