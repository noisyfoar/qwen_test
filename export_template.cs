using NPFGEO.IO.LAS.MnemonicsLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NPFGEO.IO.LAS.Export
{
    [DataContract(Namespace = "")]
    public class ExportTemplateItem
    {
        [DataMember]
        public string SourceName { set; get; }
        [DataMember]
        public string ExportName { set; get; }
        [DataMember]
        public string Description { set; get; }
        [DataMember]
        public int Precision { set; get; }
    }
    [DataContract(Namespace = "")]
    public class ExportTemplate
    {
        public ExportTemplate()
        {
        }
        public ExportTemplate(List<CurvesSource> sources)
        {
            Sources = new ObservableCollection<CurvesSource>(sources);
        }

        [DataMember]
        public string Name { set; get; }
        public string FileName { set; get; }
        [DataMember]
        public ObservableCollection<ExportTemplateItem> Items { set; get; } = new ObservableCollection<ExportTemplateItem>();
        public ObservableCollection<CurvesSource> Sources { set; get; }
    }
    public class CurvesSource
    {
        public string Name { set; get; }
        public List<ExportTemplateItem> Curves { set; get; } = new List<ExportTemplateItem>();
    }
    public class ExportTemplateReaderWriter
    {
        public ExportTemplate Read(string fileName)
        {
            ExportTemplate temp = null;
            using (var fs = File.OpenRead(fileName))
            {
                DataContractSerializerSettings settings = new DataContractSerializerSettings();
                DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(ExportTemplate), settings);

                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
                xmlReaderSettings.CheckCharacters = false;

                using (XmlReader xmlReader = XmlReader.Create(fs, xmlReaderSettings))
                {
                    temp = (ExportTemplate)dataContractSerializer.ReadObject(xmlReader);
                    xmlReader.Close();
                }

                temp.FileName = fileName;
            }
            return temp;
        }

        public void Write(string fileName, ExportTemplate set)
        {
            using (var fs = File.Create(fileName))
            {
                DataContractSerializerSettings settings = new DataContractSerializerSettings();
                settings.PreserveObjectReferences = false;
                settings.MaxItemsInObjectGraph = int.MaxValue;
                DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(ExportTemplate), settings);

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

    [DataContract(Namespace = "")]
    public class ExportSettingsTemplateParams
    {
        [DataMember]
        public string MetricName { set; get; }
        [DataMember]
        public double DepthStep { set; get; } = double.NaN;
        [DataMember]
        public TimeSpan TimeStep { set; get; } = TimeSpan.Zero;
        [DataMember]
        public string CurrentFormatName { set; get; } = string.Empty;
        [DataMember]
        public bool FixStep { set; get; }
        [DataMember]
        public string Interpolator { set; get; }
        [DataMember]
        public bool RemoveEmptyLines { set; get; }
        [DataMember]
        public string SelectedEncoder { set; get; }
    }

    [DataContract(Namespace = "")]
    public class ExportSettingsTemplate
    {
        [DataMember]
        public string Name { set; get; }
        public string FileName { set; get; }

        [DataMember]
        public ExportSettingsTemplateParams Template { set; get; }
    }

    public class ExportSettingsTemplateReaderWriter
    {
        public ExportSettingsTemplate Read(string fileName)
        {
            ExportSettingsTemplate temp = null;
            using (var fs = File.OpenRead(fileName))
            {
                DataContractSerializerSettings settings = new DataContractSerializerSettings();
                DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(ExportSettingsTemplate), settings);

                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
                xmlReaderSettings.CheckCharacters = false;

                using (XmlReader xmlReader = XmlReader.Create(fs, xmlReaderSettings))
                {
                    temp = (ExportSettingsTemplate)dataContractSerializer.ReadObject(xmlReader);
                    xmlReader.Close();
                }

                temp.FileName = fileName;
            }
            return temp;
        }

        public void Write(string fileName, ExportSettingsTemplate set)
        {
            using (var fs = File.Create(fileName))
            {
                DataContractSerializerSettings settings = new DataContractSerializerSettings();
                settings.PreserveObjectReferences = false;
                settings.MaxItemsInObjectGraph = int.MaxValue;
                DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(ExportSettingsTemplate), settings);

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

    public class ExportSettingsTemplateConverter
    {
        public ExportSettingsTemplate GetExportSettingsTemplate(ExportDialogVM vm)
        {
            ExportSettingsTemplate temp = new ExportSettingsTemplate();
            ExportSettingsTemplateParams param = new ExportSettingsTemplateParams();

            param.MetricName = vm.SelectedMetric.GetType().Name;

            if (vm.SelectedMetric is TimeMetric timeMetric)
            {
                param.TimeStep = timeMetric.Step;
                param.CurrentFormatName = timeMetric.CurrentFormat.GetType().Name;
            }
            else if (vm.SelectedMetric is DepthMetric depthMetric)
                param.DepthStep = depthMetric.Step;

            param.FixStep = vm.FixStep;
            param.Interpolator = vm.CurrentInterpolator.GetType().Name;
            param.RemoveEmptyLines = vm.RemoveEmptyLines;
            param.SelectedEncoder = vm.SelectedEncoder;

            temp.Template = param;

            return temp;
        }

        public void ApplyExportSettingsTemplate(ExportDialogVM vm, ExportSettingsTemplate temp)
        {
            var param = temp.Template;

            var metric = vm.Metrics.FirstOrDefault(a => a.GetType().Name == param.MetricName);
            vm.SelectedMetric = metric;

            if (metric is TimeMetric timeMetric)
            {
                timeMetric.CurrentFormat = timeMetric.Formats.FirstOrDefault(a => a.GetType().Name == param.CurrentFormatName);
                timeMetric.Step = param.TimeStep;
            }
            else if (metric is DepthMetric depthMetric)
                depthMetric.Step = param.DepthStep;

            vm.FixStep = param.FixStep;
            vm.CurrentInterpolator = vm.Interpolators.FirstOrDefault(a => a.GetType().Name == param.Interpolator);
            vm.RemoveEmptyLines = param.RemoveEmptyLines;
            vm.SelectedEncoder = param.SelectedEncoder;
        }
    }

}
