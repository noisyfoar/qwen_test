using Elicom;
using NPFGEO;
using NPFGEO.Data;
using NPFGEO.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShellExtension.Formats
{
    public class Interval_Format : BaseFormat, IGEOFormat, IFileReader
    {
        public Interval_Format()
        {
        }

        public override bool IsValid(string fileName)
        {
            return true;
        }

        public Curves Scan(Stream source, FieldDictionary readParams)
        {
            Curves ReturnValue = new Curves();

            return ReturnValue;
        }

        private enum eTypeData { Version, Well, Curve, Parameter, Other, ASCIILogData }

        static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        const string SamplingRate = "Частота регистрации, мс";

        double? GetSamplingRate(Elicom.RecInterval RInterval)
        {
            try
            {
                string value = RInterval.GetReferInfo(SamplingRate);
                if (string.IsNullOrEmpty(value))
                    return null;
                return double.Parse(value, CultureInfo.InvariantCulture);
            }
            catch { return null; }
        }

        public Curves Read(string fileName1)
        {
            Elicom.RecInterval RInterval = new Elicom.RecInterval();
            RInterval.Load(fileName1);
            string directory = GetTemporaryDirectory();
            //RInterval.ExportAcousticToGDT(directory);
            //RInterval.Close();

            double? samplingRate = GetSamplingRate(RInterval);
            List<string> DeviceNames = new List<string>();

            Curves ReturnValue = new Curves();

            if (RInterval.RecordInterval.IntervalInfo.Device_Number.Inners.Count > 0)
            {
                foreach (var inner in RInterval.RecordInterval.IntervalInfo.Device_Number.Inners)
                {
                    DeviceNames.Add(inner.Name);
                }
                //ReturnValue.InfoFields["DeviceName"] = new Parameter<string>("DeviceName", RInterval.RecordInterval.IntervalInfo.Device_Number.Inners[0].Name);
                //ReturnValue.InfoFields["DeviceNumber"] = new Parameter<string>("DeviceNumber", RInterval.RecordInterval.IntervalInfo.Device_Number.Inners[0].Number);
            }


            int start = RInterval.StartDepth, end = RInterval.EndDepth, step = RInterval.Step;
            int rows = (end - start) / step + 1;

            foreach (Elicom.Registration.Curve src in (RInterval.Planshet as Plansh).Planshet.Curves)
            {
                Curve target = new Curve();
                target.Caption = src.Name;
                target.Category = Path.GetFileNameWithoutExtension(fileName1);
                target.Mnemonics = src.Mnemonic;
                target.Units = src.Unit;
                target.Description = GetInfoFromDesctiption(src.GetDescription());

                IMMatrix data = null;
                if (src is Elicom.Registration.LineCurve)
                {
                    data = new MemoryMatrix<double>(1, rows);
                }

                if (src is Elicom.Registration.Spectr)
                {
                    target.SetBegin(0.0);
                    target.SetEnd(Convert.ToDouble((src as Elicom.Registration.Spectr).SpectrLength * (src as Elicom.Registration.Spectr).Delta));
                    if (samplingRate != null) target.SetSamplingRate(samplingRate.Value);
                    if (DeviceNames.Count > 0)
                        for (int i = 0; i < DeviceNames.Count; i++)
                            target.SetDeviceName("deviceName_" + i, DeviceNames[i]);

                    var dvcs = ReturnValue.GetDevices();

                    int columns = (src as Elicom.Registration.Spectr).SpectrLength;

                    string fileName = directory + "\\" + src.Name + ".gdt.arr";
                    Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
                    stream.SetLength(columns * rows * sizeof(float));
                    data = new StreamMatrix<float>(stream, columns, rows);
                }
                if (src is Elicom.Registration.Acoustic)
                {
                    target.SetBegin((src as Elicom.Registration.Acoustic).WaveStartMks);
                    target.SetEnd((src as Elicom.Registration.Acoustic).WaveStartMks + (src as Elicom.Registration.Acoustic).WaveLengthMks);
                    target.Units = (src as Elicom.Registration.Acoustic).WaveUnits;

                    int columns = (src as Elicom.Registration.Acoustic).WaveSize;

                    string fileName = directory + "\\" + src.Name + ".gdt.arr";
                    Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
                    stream.SetLength(columns * rows * sizeof(short));
                    data = new StreamMatrix<short>(stream, columns, rows);
                }

                Matrix<double> depth = MemoryMatrix<double>.FromParams(start / 100.0, step / 100.0, rows);

                target.SetData(data, depth);

                var shiftValue = GetShiftValue(target.Description);

                ShiftCurve(target, shiftValue);

                ReturnValue.Add(target, eTypeConflictSolution.Rename);
            }

            // TODO: Исправить после появления полноценного импорта с возможностью выбора кривых и каналов для импорта
            //Необходимости в импорте каналов до этого не было
            //Появилась необходимость импортировать лишь канал "k_cgks_time" (цикл опроса прибора) для корректной обработки ГКС
            //Во время беседы с Галеевым Р.Р. и Дроновым О.В. было принято решение не делать полноценный импорт каналов, загружать только "k_cgks_time" при его наличии
            DataFrame testFrame = RInterval.GetData(0) as DataFrame; //Смотрим самый первый фрейм на наличие нужного канала
            if (testFrame != null && testFrame.Data.Chanels.Any(chanel => chanel.Key == "k_cgks_time"))
            {
                Curve target = new Curve();
                target.Caption = "k_cgks_time";
                target.Category = Path.GetFileNameWithoutExtension(fileName1);

                IMMatrix data = new MemoryMatrix<double>(1, rows);
                Matrix<double> depth = MemoryMatrix<double>.FromParams(start / 100.0, step / 100.0, rows);

                target.SetData(data, depth);

                var shiftValue = GetShiftValue(target.Description);

                ShiftCurve(target, shiftValue);

                ReturnValue.Add(target, eTypeConflictSolution.Rename);
            }

            for (int i = start, row = 0; i <= end; i += step, row++)
            {
                DataFrame frame = RInterval.GetData(i) as DataFrame;
                if (frame == null) continue;

                foreach (var pair in frame.Data.CurvesData)
                {
                    string name = pair.Key;

                    Curve curve = ReturnValue.FirstOrDefault(a => a.Caption == name);
                    if (curve == null) continue;

                    curve.DataMatrix[0, row] = double.IsNaN(pair.Value) ? curve.NullValue : pair.Value;
                }

                foreach (var pair in frame.Data.Chanels)
                {
                    string name = pair.Key;

                    Curve curve = ReturnValue.FirstOrDefault(a => a.Caption == name);
                    if (curve == null) continue;

                    curve.DataMatrix[0, row] = double.IsNaN(pair.Value) ? curve.NullValue : pair.Value;
                }

                foreach (var pair in frame.Data.FloatArray)
                {
                    string name = pair.Key;

                    Curve curve = ReturnValue.FirstOrDefault(a => a.Caption == name);
                    if (curve == null) continue;

                    float[,] temp = new float[1, pair.Value.Length];
                    Buffer.BlockCopy(pair.Value, 0, temp, 0, temp.Length * sizeof(float));

                    curve.DataMatrix.ReplaceRows(row, new MemoryMatrix<float>(temp));
                }
                foreach (var pair in frame.Data.AcData)
                {
                    string name = pair.Key;

                    Curve curve = ReturnValue.FirstOrDefault(a => a.Caption == name);
                    if (curve == null) continue;

                    short[,] temp = new short[1, pair.Value.Length];
                    Buffer.BlockCopy(pair.Value, 0, temp, 0, temp.Length * sizeof(short));

                    curve.DataMatrix.ReplaceRows(row, new MemoryMatrix<short>(temp));
                }
            }

            RInterval.Close();

            return ReturnValue;
        }

        private string GetInfoFromDesctiption(string fullDescription)
        {
            //Пока берется только точка записи, не стал всё считывать, так как при экспорте в LAS в Description будет длинная строка
            string regPoint = Regex.Match(fullDescription, @"Точка записи:(.*?)\r\n").Value.Replace("\r\n","");

            return regPoint;
        }

        private double GetShiftValue(string regPointString)
        {
            if (String.IsNullOrWhiteSpace(regPointString))
                return 0;

            var value = Regex.Matches(regPointString, @"-?\d+(?:\.\d+)?")[0].Value;
            Double.TryParse(value, out double result);

            return result;
        }

        private void ShiftCurve(Curve curve, double shiftValue)
        {
            var delta = (decimal)shiftValue;
            curve.Lock();

            for (int row = 0; row < curve.DepthMatrix.Rows; row++)
                curve.DepthMatrix[0, row] = (double)(Convert.ToDecimal(curve.DepthMatrix[0, row]) + delta);

            curve.Unlock();
        }

        public void Write(Curves curves, string fileName)
        {
            throw new NotImplementedException();
        }

        public override bool IsValid(Curve curve)
        {
            throw new NotImplementedException();
        }

        public eFormatType Type { get { return eFormatType.OnlyRead; } }
        //public string Name { get { return "Log ASCII Standard"; } }
        public string Name { get { return "Interval"; } }
        public IEnumerable<string> FileExtensions { get { return new string[] { "Interval" }; } }
    }
}
