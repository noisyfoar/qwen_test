using Elicom;
using Elicom.Registration;
using NPFGEO.Data;
using NPFGEO.IO.Gdt;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace ShellExtension.Formats.LIS
{

    public enum SamplingType
    {
        [Description("По глубине")]
        Depth,
        [Description("По времени")]
        Time,
        [Description("По счётчику")]
        Counter
    }
    public enum TextEncoding
    {
        Windows = 1251,
        Dos = 866
    }

    public class RecSession
    {
        private Dictionary<long, RecordIndexer> _Data = new Dictionary<long, RecordIndexer>();
        private static TextEncoding _LasEncoding = TextEncoding.Dos;
        private long _LastDepth;
        private long? _MaxIndex = null;
        private long? _MinIndex = null;
        private List<string> _PluginComents;
        private long? _PrevSkDepth = null;
        private bool _RegistredByTime;
        private SamplingType _SamplingType;
        private double _Step;
        private double _StepKoef;
        private string _StepUnit;
        private string _StoreFileName = "";
        private DepthDiapason _TrueDiapason = new DepthDiapason();
        private ExtistIndexes ExtistIndexes = new ExtistIndexes();
        private BinaryReader StoreFileBR;
        private BinaryWriter StoreFileBW;

        public DateTime StartTime = DateTime.Now;
        public FileStream tempFileStream;
        public int Count => _Data.Count;
        private Dictionary<long, RecordIndexer> Data
        {
            get { return _Data; }
            set { _Data = value; }
        }
        public static TextEncoding LasEncoding
        {
            get { return _LasEncoding; }
            set { _LasEncoding = value; }
        }
        public long LastDepth => _LastDepth;
        public long MaxIndex
        {
            get
            {
                if (!_MaxIndex.HasValue)
                {
                    return 0L;
                }
                return _MaxIndex.Value;
            }
            set { _MaxIndex = value; }
        }
        public long MinIndex
        {
            get
            {
                if (!_MinIndex.HasValue)
                {
                    return 0L;
                }
                return _MinIndex.Value;
            }
            set { _MinIndex = value; }
        }
        public List<string> PluginComents
        {
            get { return _PluginComents; }
            set { _PluginComents = value; }
        }
        public long? PrevSkDepth
        {
            get { return _PrevSkDepth; }
            set { _PrevSkDepth = value; }
        }
        public bool RegistredByTime
        {
            get { return _RegistredByTime; }
            set { _RegistredByTime = value; }
        }
        public SamplingType SamplingType
        {
            get { return _SamplingType; }
            set { _SamplingType = value; }
        }
        public double Step
        {
            get { return _Step; }
            set { _Step = value; }
        }
        public int depthStep
        {
            get { return (int)(Step*_StepKoef); }
        }
        private string tempFilePath
        {
            get { return _StoreFileName; }
            set { _StoreFileName = value; }
        }
        public DepthDiapason TrueDiapason
        {
            get { return _TrueDiapason; }
            set { _TrueDiapason = value; }
        }


        public RecSession(double step, string fileName)
        {
            _Step = step;
            SamplingType = SamplingType.Depth;

            string name = Path.GetFileNameWithoutExtension(fileName) + "_StepRecords";
            string extention = Path.GetExtension(fileName);
            fileName = Path.GetDirectoryName(fileName) + "\\" + name + extention;
            tempFilePath = fileName;
            tempFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite);
            StoreFileBW = new BinaryWriter(tempFileStream);
            StoreFileBR = new BinaryReader(tempFileStream);
        }

        public void SetStepKoef(int StepKoef)
        {
            _StepKoef = StepKoef;
        }
        public void SetLISStepUnit(string stepUnit)
        {
            _StepUnit = stepUnit;
        }
        public static long RoundToStep(long depth, int st)
        {
            return depth / st * st;
        }

        public void SaveCurvesToLIS(DepthDiapason depthRange, Curves curves, string outputFilePath, IntervalInfo info, bool interpolateValues)
        {
            var lisNullValue = (float)curves.FirstOrDefault().NullValue;
            FillData(curves, lisNullValue);

            try
            {
                long startDepth = Math.Min(depthRange.Up, depthRange.Down);
                long endDepth = Math.Max(depthRange.Up, depthRange.Down); // Foot или Roof?
                LISWriter liSWriter = new LISWriter(outputFilePath);
                LIS.Block64Datum01[] curveDescriptors = new LIS.Block64Datum01[curves.Count];
                uint channelIndex = 1u;
                string[] usedMnemonics = new string[curves.Count];

                LIS.Block34Table DescritpionParametersBlock34Table = new LIS.Block34Table("CONS");
                DescritpionParametersBlock34Table.Make5Columns("NAME", "STAT", "PUNI", "TUNI", "VALU");
                DescritpionParametersBlock34Table.Add5ColumnRow("hole", "ALLO", "US", "US", "Где-то");
                DescritpionParametersBlock34Table.Add5ColumnRow("name", "ALLO", "US", "US", "Кто-то");
                DescritpionParametersBlock34Table.Add5ColumnRow("date", "ALLO", "US", "US", "Когда-то");
                liSWriter.Block34Tables.Add(DescritpionParametersBlock34Table);

                bool isTimeRegistred = false;
                if (info != null)
                {
                    liSWriter.AddLasInfo("WELL", info.GetWellInfo());
                    liSWriter.AddLasInfo("CRVS", info.GetCurveInfo());
                    liSWriter.AddLasInfo("REG", info.RegistratorInfo);
                    isTimeRegistred = info.RegistredByTime;
                }
                for (int curveIndex = 0; curveIndex < curveDescriptors.Length; curveIndex++)
                {
                    NPFGEO.Data.Curve currentCurve = curves[curveIndex];
                    string shortMnemonic = LISMnemonicsLib.Get4SymbolName(currentCurve.Caption, usedMnemonics);



                    usedMnemonics[curveIndex] = shortMnemonic;
                    curveDescriptors[curveIndex] = new LIS.Block64Datum01();
                    LIS.Block64Datum01 curveDescriptor = curveDescriptors[curveIndex];
                    if (string.IsNullOrEmpty(shortMnemonic))
                        curveDescriptor.Mnemonic = "";
                    else curveDescriptor.Mnemonic = Translit.Translate(shortMnemonic);
                    curveDescriptor.WorkID = " ";
                    curveDescriptor.WorkNumber = " ";
                    curveDescriptor.Units = "";
                    curveDescriptor.APICrvClass = 1;
                    curveDescriptor.DataCount = 1;
                    if (currentCurve.DataMatrix.Columns == 1)
                    {
                        curveDescriptor.Data_Size = 4;
                        curveDescriptor.ViewCode = 68;
                    }
                    else if (currentCurve.DataMatrix.Columns > 1)
                    {
                        LIS.Block34Table currentCurveParametersBlock34Table = new LIS.Block34Table(shortMnemonic);
                        currentCurveParametersBlock34Table.Make5Columns("MNEM", "STAT", "PUNI", "TUNI", "VALU");
                        currentCurveParametersBlock34Table.Add5ColumnRow("init", "ALLO", "US", "US", Convert.ToInt32(currentCurve.InfoFields["Begin"].Value));
                        currentCurveParametersBlock34Table.Add5ColumnRow("delt", "ALLO", "US", "US", Convert.ToInt32(GDTConvertHelper.GetDelta(currentCurve)));
                        
                        channelIndex++;

                        if (currentCurve.DataMatrix is Matrix<float>)
                        {
                            curveDescriptor.ViewCode = 68;
                            curveDescriptor.Data_Size = (ushort)(currentCurve.DataMatrix.Columns * 4);
                        }
                        else
                        {
                            curveDescriptor.ViewCode = 79;
                            curveDescriptor.Data_Size = (ushort)(currentCurve.DataMatrix.Columns * 2);
                        }
                        liSWriter.Block34Tables.Add(currentCurveParametersBlock34Table);
                    }
                }
                liSWriter.Block64Data.Datum01 = curveDescriptors;
                liSWriter.Block64Data.CadreStep = depthStep;
                liSWriter.Block64Data.CadreStepUnits = _StepUnit;
                liSWriter.Block64Data.DepthUnits = _StepUnit;
                liSWriter.Block64Data.DataFrameSize = (ushort)liSWriter.Block64Data.DataSize;
                liSWriter.Block64Data.UpDown = byte.MaxValue;
                liSWriter.Block64Data.NullValue = (float)curves.FirstOrDefault().NullValue;
                liSWriter.Block64Data.MaxCadre = 1;

                int currentMaxRecordLen = Convert.ToInt32(liSWriter.Block128Data.MaxPhisRecLen);
                if (currentMaxRecordLen < liSWriter.Block64Data.DataSize)
                {
                    liSWriter.Block128Data.MaxPhisRecLen = liSWriter.Block64Data.DataSize.ToString();
                }
                liSWriter.StartDepth = (int)startDepth;
                int depthSamplesCount = (int)((endDepth - startDepth) / depthStep + 1);
                int recordByteSize = liSWriter.Block64Data.DataSize;
                byte[] dataBuffer = new byte[depthSamplesCount * recordByteSize];
                MemoryStream bufferStream = new MemoryStream(dataBuffer);
                BinaryWriter bufferBinaryWriter = new BinaryWriter(bufferStream);
                for (long currentDepth = startDepth; currentDepth <= endDepth; currentDepth += depthStep)
                {
                    foreach (NPFGEO.Data.Curve outputCurve in curves)
                    {
                        long adjustedDepth = currentDepth;
                        if (isTimeRegistred)
                        {
                            adjustedDepth = currentDepth;
                        }
                        OneStepRecord rawDepthRecord = GetData(adjustedDepth);
                        object convertedCurveValue = GetDataForGdt(rawDepthRecord, interpolateValues, outputCurve);
                        if (convertedCurveValue is double doubleValue)
                        {
                            uint lisEncodedFloat = LIS.IEEEfloat2LIS((float)doubleValue);
                            bufferBinaryWriter.Write(lisEncodedFloat);
                        }
                        else if (convertedCurveValue is ushort[])
                        {
                            ushort[] ushortChannelValues = convertedCurveValue as ushort[];
                            for (int ushortIndex = 0; ushortIndex < ushortChannelValues.Length; ushortIndex++)
                            {
                                bufferBinaryWriter.Write(LIS.Swap(ushortChannelValues[ushortIndex]));
                            }
                        }
                        else if (convertedCurveValue is float[])
                        {
                            float[] floatChannelValues = convertedCurveValue as float[];
                            for (int floatIndex = 0; floatIndex < floatChannelValues.Length; floatIndex++)
                            {
                                bufferBinaryWriter.Write(LIS.IEEEfloat2LIS(floatChannelValues[floatIndex]));
                            }
                        }
                    }
                }
                liSWriter.DataToWrite = dataBuffer;
                liSWriter.WriteLIS();
            }
            catch (OutOfMemoryException)
            {
                string memoryErrorMessage = "Недостаточно памяти для создания LIS файла.\r\nПопробуйте создать несколько файлов LIS на разных диапазонах глубин.";
                MessageBox.Show(memoryErrorMessage, "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            catch (Exception exportException)
            {
                string genericErrorMessage = "Не удалось создать LIS\r\n" + exportException.Message;
                MessageBox.Show(genericErrorMessage, "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                tempFileStream.Flush();
                tempFileStream.Close();
                File.Delete(tempFilePath);
                Clear();
            }
        }

        public interface IOneStepRecordFiller
        {
            void Fill(OneStepRecord record, double depth, double step, float lisNullValue);
        }

        public class LineCurveFiller : IOneStepRecordFiller
        {
            NPFGEO.Data.Curve _curve;
            int index = 0;

            public LineCurveFiller(NPFGEO.Data.Curve curve)
            {
                _curve = curve;
            }

            public void Fill(OneStepRecord record, double depth, double step, float lisNullValue)
            {
                if (_curve.DataMatrix.Rows == 0)
                {
                    record.CurvesData.Add(_curve.Caption, lisNullValue);
                    return;
                }

                if (depth < _curve.Roof || depth > _curve.Foot)
                {
                    record.CurvesData.Add(_curve.Caption, lisNullValue);
                    return;
                }

                var endDepth = Math.Round(depth + step, 3);
                var aboveIndex = _curve.DepthMatrix.BinarySearchAbove(depth);
                var afterIndex = _curve.DepthMatrix.BinarySearchAfter(depth);

                if (aboveIndex >= _curve.DepthMatrix.Rows || afterIndex >= _curve.DepthMatrix.Rows)
                {
                    record.CurvesData.Add(_curve.Caption, lisNullValue);
                    return;
                }

                var aboveDepth = _curve.DepthMatrix[aboveIndex];
                var afterDepth = _curve.DepthMatrix[afterIndex];
                double curDepth;

                if (Math.Abs(aboveDepth - depth) < Math.Abs(afterDepth - depth))
                    curDepth = aboveDepth;
                else
                    curDepth = afterDepth;

                if (curDepth > endDepth)
                {
                    record.CurvesData.Add(_curve.Caption, lisNullValue);
                    return;
                }

                var values = new List<double>();
                while (true)
                {
                    if (index >= _curve.DepthMatrix.Rows) break;
                    curDepth = Math.Round(_curve.DepthMatrix[index], 3);
                    if (curDepth >= endDepth) break;
                    var value = System.Convert.ToSingle(_curve.DataMatrix[0, index]);
                    values.Add(value);
                    index++;
                }

                if (values.Count > 0)
                {
                    var value = values.Average();
                    record.CurvesData.Add(_curve.Caption, value);
                }
                else
                {
                    record.CurvesData.Add(_curve.Caption, lisNullValue);
                }
            }
        }

        public class TwoDimCurveFiller : IOneStepRecordFiller
        {
            NPFGEO.Data.Curve _curve;
            int index = 0;

            public TwoDimCurveFiller(NPFGEO.Data.Curve curve)
            {
                _curve = curve;
            }

            public void Fill(OneStepRecord record, double depth, double step, float lisNullValue)
            {
                if (_curve.DataMatrix.Rows == 0)
                {
                    if (_curve.DataMatrix.Columns == 1)
                        record.CurvesData.Add(_curve.Caption, lisNullValue);
                    else
                        record.AcData.Add(_curve.Caption, new ushort[_curve.DataMatrix.Columns]);
                    return;
                }

                if (depth < _curve.Roof || depth > _curve.Foot)
                {
                    if (_curve.DataMatrix.Columns == 1)
                        record.CurvesData.Add(_curve.Caption, lisNullValue);
                    else if (_curve.DataMatrix is Matrix<float>)
                        record.FloatArray.Add(_curve.Caption, new float[_curve.DataMatrix.Columns]);
                    else
                        record.AcData.Add(_curve.Caption, new ushort[_curve.DataMatrix.Columns]);
                    return;
                }

                var endDepth = Math.Round(depth + step, 3);
                var curDepth = _curve.DepthMatrix[index];

                if (curDepth > endDepth)
                {
                    if (_curve.DataMatrix.Columns == 1)
                        record.CurvesData.Add(_curve.Caption, lisNullValue);
                    else if (_curve.DataMatrix is Matrix<float>)
                        record.FloatArray.Add(_curve.Caption, new float[_curve.DataMatrix.Columns]);
                    else
                        record.AcData.Add(_curve.Caption, new ushort[_curve.DataMatrix.Columns]);
                    return;
                }

                var lastIndex = -1;
                while (true)
                {
                    if (index >= _curve.DepthMatrix.Rows) break;
                    curDepth = _curve.DepthMatrix[index];
                    if (curDepth >= endDepth) break;
                    lastIndex = index;
                    index++;
                }

                if (lastIndex != -1)
                {
                    var columns = _curve.DataMatrix.Columns;
                    var data = new ushort[columns];
                    float[] floatData = new float[columns];

                    if (_curve.DataMatrix is Matrix<short>)
                    {
                        var tmp = new short[1, columns];
                        (_curve.DataMatrix as Matrix<short>).GetRows(lastIndex, tmp, 0, 1);
                        Buffer.BlockCopy(tmp, 0, data, 0, columns * 2);
                        record.AcData.Add(_curve.Caption, data);
                    }
                    else if (_curve.DataMatrix is Matrix<ushort>)
                    {
                        var tmp = new ushort[1, columns];
                        (_curve.DataMatrix as Matrix<ushort>).GetRows(lastIndex, tmp, 0, 1);
                        Buffer.BlockCopy(tmp, 0, data, 0, columns * 2);
                        record.AcData.Add(_curve.Caption, data);
                    }
                    else if (_curve.DataMatrix is Matrix<float>)
                    {
                        var tmp = new float[1, columns];
                        (_curve.DataMatrix as Matrix<float>).GetRows(lastIndex, tmp, 0, 1);
                        Buffer.BlockCopy(tmp, 0, floatData, 0, columns * 4);
                        record.FloatArray.Add(_curve.Caption, floatData);
                    }
                    else
                    {
                        for (int j = 0; j < columns; j++)
                            data[j] = (ushort)Convert.ToInt16(_curve.DataMatrix[j, lastIndex]);
                        record.AcData.Add(_curve.Caption, data);
                    }
                }
                else
                {
                    if (_curve.DataMatrix.Columns == 1)
                        record.CurvesData.Add(_curve.Caption, lisNullValue);
                    else if (_curve.DataMatrix is Matrix<float>)
                        record.FloatArray.Add(_curve.Caption, new float[_curve.DataMatrix.Columns]);
                    else
                        record.AcData.Add(_curve.Caption, new ushort[_curve.DataMatrix.Columns]);
                }
            }
        }

        IOneStepRecordFiller GetFiller(NPFGEO.Data.Curve curve)
        {
            if (curve.DataMatrix.Columns == 1)
                return new LineCurveFiller(curve);
            else
                return new TwoDimCurveFiller(curve);
        }

        private void FillData(Curves curves, float lisNullValue)
        {
            var step = Step;
            var depths = GetFixStepDepths(curves, step);

            var fillers = curves.Select(a => GetFiller(a)).ToArray();

            for (int i = 0; i < depths.Length; i++)
            {
                var depth = depths[i];
                long depthCM = (long)Math.Round(depth * _StepKoef);

                OneStepRecord oneStepRecord = new OneStepRecord(depthCM);

                for (int j = 0; j < fillers.Length; j++)
                    fillers[j].Fill(oneStepRecord, depth, step, lisNullValue);

                long length = tempFileStream.Length;
                tempFileStream.Seek(0L, SeekOrigin.End);
                oneStepRecord.Serialize(tempFileStream);

                AddDataToIndexer(length, oneStepRecord);
            }
        }

        private double[] GetFixStepDepths(NPFGEO.Data.Curves curves, double step)
        {
            var start = curves.Where(a => a.DepthMatrix.Rows > 0).Select(a => a.Roof).Min();
            var stop = curves.Where(a => a.DepthMatrix.Rows > 0).Select(a => a.Foot).Max();

            decimal dStart = (decimal)start, dStop = (decimal)stop, dStep = (decimal)step;
            int count = Convert.ToInt32((dStop - dStart) / dStep + 1);
            double[] depths = new double[count];
            for (int i = 0; i < count; i++)
            {
                depths[i] = Convert.ToDouble(dStart) + (Convert.ToDouble(dStep) * i);
            }

            return depths;
        }

        private void AddDataToIndexer(long pos, OneStepRecord rec)
        {
            ExtistIndexes[rec.DestIndex] = true;
            if (rec.DestIndex > MaxIndex || !_MaxIndex.HasValue)
            {
                _MaxIndex = rec.DestIndex;
            }
            if (rec.DestIndex < MinIndex || !_MinIndex.HasValue)
            {
                _MinIndex = rec.DestIndex;
            }
            if (SamplingType == SamplingType.Time)
            {
                rec.DestIndex = (long)(rec.Time - StartTime).TotalMilliseconds;
            }
            RecordIndexer value = new RecordIndexer(pos, StoreFileBR, rec);
            if (Data.ContainsKey(rec.DestIndex))
            {
                Data[rec.DestIndex] = value;
            }
            else
            {
                Data.Add(rec.DestIndex, value);
            }
            _LastDepth = rec.DestIndex;
        }

        private double prevData = short.MinValue;
        private ushort[] prevUshortData = null;
        private float[] prevArray = null;
        public object GetDataForGdt(OneStepRecord r, bool previousIfEmpty, NPFGEO.Data.Curve curve)
        {
            string name = curve.Caption;

            if (curve.DataMatrix.Rows == 0)
            {
                if (curve.DataMatrix.Columns == 1)
                    return short.MinValue; // null value
                else
                    return new ushort[curve.DataMatrix.Columns];
            }

            if (curve.DataMatrix.Columns == 1)
            {
                if (r != null && r.CurvesData.ContainsKey(name) && !double.IsNaN(r.CurvesData[name]))
                {
                    prevData = r.CurvesData[name];
                    return prevData;
                }
                if (previousIfEmpty)
                {
                    return prevData;
                }
                return short.MinValue; // null value
            }
            else if (curve.DataMatrix is Matrix<float> == false)
            {
                if (prevUshortData == null)
                {
                    prevUshortData = new ushort[curve.DataMatrix.Columns];
                    for (int i = 0; i < curve.DataMatrix.Columns; i++)
                        prevUshortData[i] = 0;
                }
                if (r != null && r.AcData.ContainsKey(name))
                {
                    prevUshortData = r.AcData[name];
                    return r.AcData[name];
                }
                if (previousIfEmpty)
                {
                    return prevUshortData;
                }
                return prevUshortData;
            }
            else
            {
                if (prevArray == null)
                {
                    prevArray = new float[curve.DataMatrix.Columns];
                    for (int i = 0; i < curve.DataMatrix.Columns; i++)
                        prevArray[i] = 0;
                }
                if (r != null && r.FloatArray.ContainsKey(name))
                {
                    prevArray = r.FloatArray[name];
                    return r.FloatArray[name];
                }
                if (previousIfEmpty)
                {
                    return prevArray;
                }
                return prevArray;
            }
        }

        public OneStepRecord GetData(long depth)
        {
           // long num = RoundToStep(depth);
            if (!ExtistIndexes[depth])
            {
                return null;
            }
            if (!Data.ContainsKey(depth))
            {
                return null;
            }
            return _Data[depth].GetData();
        }
        public long RoundToStep(long depth)
        {
            return (long)((decimal)depth / (decimal)depthStep) * depthStep;
        }

        public void Clear()
        {
            var dataToClear = Data.Values.ToArray();
            foreach (var item in dataToClear)
            {
                item.Clear();
            }
            GC.Collect(1, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }
    }

    public class RecordIndexer
    {
        private BinaryReader BR;

        private WeakReference WeekData;

        private long _Adress;

        private static Dictionary<long, OneStepRecord> Buf = new Dictionary<long, OneStepRecord>();

        private static List<OneStepRecord> Buffer = new List<OneStepRecord>();

        public long Adress
        {
            get
            {
                return _Adress;
            }
            set
            {
                _Adress = value;
            }
        }

        public RecordIndexer(long adr, BinaryReader br, OneStepRecord data)
        {
            BR = br;
            _Adress = adr;
            WeekData = new WeakReference(data);
        }

        public OneStepRecord GetData()
        {
            if (WeekData.IsAlive && WeekData.Target is OneStepRecord result)
            {
                return result;
            }
            BR.BaseStream.Seek(Adress, SeekOrigin.Begin);
            OneStepRecord oneStepRecord = OneStepRecord.Deserialize(BR);
            AddToBuffer(oneStepRecord);
            WeekData.Target = oneStepRecord;
            return oneStepRecord;
        }

        private static void AddToBuffer(OneStepRecord r)
        {
            if (!Buffer.Contains(r))
            {
                Buffer.Add(r);
            }
            while (Buffer.Count > 2000)
            {
                Buffer.RemoveAt(0);
            }
        }

        public void Clear()
        {
            Buffer.Clear();
            _Adress = 0;
            WeekData = null;
        }
    }

    public class DepthDiapason
    {
        private long _Up;

        private long _Down;

        private int _koef;

        public decimal UpM => (decimal)Up / (decimal)_koef;

        public decimal DownM => (decimal)Down / (decimal)_koef;

        public long Up
        {
            get
            {
                return _Up;
            }
            set
            {
                _Up = value;
            }
        }

        public long Down
        {
            get
            {
                return _Down;
            }
            set
            {
                _Down = value;
            }
        }

        public DepthDiapason()
        {
        }

        public DepthDiapason(VScrollBar scrollBar, UnitConvert uc)
        {
            Up = 2 * scrollBar.Value * uc.Scale;
            Down = (scrollBar.Value * 2 + (int)uc.PointsToCM(scrollBar.Height)) * uc.Scale;
        }

        public DepthDiapason(long depth1, long depth2)
        {
            Up = Math.Min(depth1, depth2);
            Down = Math.Max(depth1, depth2);
        }

        public DepthDiapason(long depth1, long depth2, int step)
        {
            depth1 = RecSession.RoundToStep(depth1, step);
            depth2 = RecSession.RoundToStep(depth2, step);
            Up = Math.Min(depth1, depth2);
            Down = Math.Max(depth1, depth2);
        }

        public bool DepthInDiapason(long depth)
        {
            if (depth > Up)
            {
                return depth < Down;
            }
            return false;
        }

        public void SetDepthKoef(int koef)
        {
            _koef = koef;
        }

        public override string ToString()
        {
            return Up + " - " + Down;
        }

        public string ToStringM()
        {
            int floatPartCount = (int)Math.Floor(Math.Log10(_koef) + 1) - 1;
            decimal num = Math.Round((decimal)Up / (decimal)_koef, floatPartCount);

            string stringFormat = GenerateStringFormat(floatPartCount);
            
            return string.Concat(str2: Math.Round((decimal)Down / (decimal)_koef, floatPartCount).ToString(stringFormat), str0: num.ToString(stringFormat), str1: "m - ", str3: "m");
        }

        private string GenerateStringFormat(int floatPartCount)
        {
            var resultString = "0.";

            for (int i = 0; i < floatPartCount; i++)
            {
                resultString += "0";
            }

            return resultString;
        }
    }
    public class ExtistIndexes
    {
        private const long offset = 1000L;

        private const long DataSize = 1000000L;

        private const byte yes = 1;

        private const byte no = 0;

        private byte[] data = new byte[999000];

        public bool this[long index]
        {
            get
            {
                long num = index + 1000;
                if (num < 0 || num >= data.Length)
                {
                    return true;
                }
                return data[num] != 0;
            }
            set
            {
                long num = index + 1000;
                if (num >= 0 && num < data.Length)
                {
                    data[num] = (byte)(value ? 1 : 0);
                }
            }
        }

        public void Clear()
        {
            data = new byte[1000000];
        }
    }
    [TypeConverter(typeof(ItemsListArrayConvertor))]
    [ExpandProperties]
    public class LasInfoCollection : List<LasInfo_V2>, IItemsCollection
    {
        public string CollectionName => "";

        public string ToList()
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (Enumerator enumerator = GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    LasInfo_V2 current = enumerator.Current;
                    stringBuilder.AppendLine(current.ToLine());
                }
            }
            return stringBuilder.ToString();
        }

        public void SetData(LasInfoCollection col)
        {
            foreach (LasInfo_V2 item in col)
            {
                bool flag = false;
                using (Enumerator enumerator2 = GetEnumerator())
                {
                    while (enumerator2.MoveNext())
                    {
                        LasInfo_V2 current2 = enumerator2.Current;
                        if (item.MNEM == current2.MNEM)
                        {
                            current2.DATA = item.DATA;
                            flag = true;
                            break;
                        }
                    }
                }
                if (!flag)
                {
                    Add(item);
                }
            }
        }

        public bool DataTypeSupported(Type type)
        {
            return type == typeof(LasInfo_V2);
        }

        public IList GetList()
        {
            return this;
        }

        public IItemsCollection Clone()
        {
            return this.CloneXml() as LasInfoCollection;
        }

        public object NewItem()
        {
            return new LasInfo_V2("New", "");
        }

        public void SomePropertyChanged(PropertyValueChangedEventArgs e)
        {
        }

        public object GetItem(string name)
        {
            using (Enumerator enumerator = GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    LasInfo_V2 current = enumerator.Current;
                    if (current.MNEM == name)
                    {
                        return current;
                    }
                }
            }
            return null;
        }
    }
    public class LasInfo_V2
    {
        private const int ColonPosition = 35;

        private string _MNEM = "";

        private string _UNITS = "";

        private string _DATA = "";

        private string _DESCRIPTION = "";

        [DisplayName("Мнемоника")]
        [PropertyOrder(10)]
        public string MNEM
        {
            get
            {
                return _MNEM;
            }
            set
            {
                _MNEM = value;
            }
        }

        [DisplayName("Ед. измерения")]
        [PropertyOrder(20)]
        public string UNITS
        {
            get
            {
                return _UNITS;
            }
            set
            {
                _UNITS = value;
            }
        }

        [PropertyOrder(30)]
        [DisplayName("Данные")]
        public string DATA
        {
            get
            {
                return _DATA;
            }
            set
            {
                _DATA = value;
            }
        }

        [PropertyOrder(40)]
        [DisplayName("Примечание")]
        public string DESCRIPTION
        {
            get
            {
                return _DESCRIPTION;
            }
            set
            {
                _DESCRIPTION = value;
            }
        }

        public void ApplyVrible(LasInfo_V2 info, VaribleCollection col)
        {
            string name = MNEM.ToLower().Trim();
            if (col.Contains(name))
            {
                string data = col.Get(name).Data;
                string[] array = data.Split(new char[1] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (array.Length == 2)
                {
                    info.DATA = array[0];
                    info.DESCRIPTION = array[1];
                }
                else
                {
                    info.DATA = data;
                }
            }
        }

        public LasInfo_V2 GetCloneWithData(VaribleCollection col)
        {
            LasInfo_V2 lasInfo = this.CloneXml() as LasInfo_V2;
            ApplyVrible(lasInfo, col);
            return lasInfo;
        }

        public LasInfo_V2()
        {
        }

        public override string ToString()
        {
            string text = MNEM;
            if (DESCRIPTION != null && DESCRIPTION.Trim().Length > 0)
            {
                text = text + " (" + DESCRIPTION + ")";
            }
            return text;
        }

        public LasInfo_V2(string mnem, string data)
        {
            MNEM = mnem;
            DATA = data;
        }

        public LasInfo_V2(string mnem, string units, string data, string desc)
        {
            MNEM = mnem;
            UNITS = units;
            DATA = data;
            DESCRIPTION = desc;
        }

        public string ToLine()
        {
            StringBuilder stringBuilder = new StringBuilder("");
            stringBuilder.Append(MNEM);
            stringBuilder.Append(".");
            if (UNITS != string.Empty)
            {
                stringBuilder.Append(UNITS);
            }
            else
            {
                stringBuilder.Append(" ");
            }
            int num = 35 - stringBuilder.Length;
            string format = "{0," + num + "}";
            stringBuilder.AppendFormat(format, DATA, num);
            stringBuilder.Append(":");
            if (DESCRIPTION != string.Empty)
            {
                stringBuilder.Append(DESCRIPTION);
            }
            stringBuilder.Replace(",", ".");
            return stringBuilder.ToString();
        }

        public LasInfo_V2(string line)
        {
            string[] array = line.Split(new char[1] { ' ' }, 2);
            if (array.Length > 1)
            {
                int num = line.IndexOf('.');
                int num2 = line.LastIndexOf(':');
                MNEM = line.Substring(0, num).Trim();
                MNEM.Split(' ');
                int num3 = line.IndexOf(' ', num);
                if (num3 == num + 1)
                {
                    UNITS = "";
                }
                else
                {
                    UNITS = line.Substring(num + 1, num3 - num + 1).Trim();
                }
                DATA = line.Substring(num3, num2 - num3).Trim();
                if (num2 == line.Length)
                {
                    DESCRIPTION = "";
                }
                else
                {
                    DESCRIPTION = line.Substring(num2 + 1, line.Length - num2 - 1).Trim();
                }
            }
        }

        public ListViewItem ToListViewItem()
        {
            ListViewItem listViewItem = new ListViewItem(MNEM);
            listViewItem.SubItems.Add(UNITS);
            listViewItem.SubItems.Add(DATA);
            listViewItem.SubItems.Add(DESCRIPTION);
            return listViewItem;
        }
    }
    [TypeConverter(typeof(PropertySorter))]
    public class IntervalInfo : GlobalizedObject
    {
        private string _FileName;

        private DepthDiapason _TrueDiapason = new DepthDiapason();

        private Planshet _Planshet;

        private DeviceLengthes _DeviceLengthes = new DeviceLengthes();

        private LasInfoCollection _RegistratorInfo = new LasInfoCollection();

        private WellInfo _Well = new WellInfo();

        private double _AvgSpeed;

        private int _Step;

        private DepthDiapason _Diapason = new DepthDiapason();

        private DateTime _StartDateTime = DateTime.Now;

        private DateTime _EndDateTime = DateTime.Now;

        private RecDirection _Direction;

        private bool _Modifed;

        private bool _RegistredByTime;

        private bool _ReplaceDepthToTrueDepth;

        private List<BinaryTag> _Tag = new List<BinaryTag>();

        private List<string> _OtherFormatFiles = new List<string>();

        [Browsable(false)]
        public string FileName
        {
            get
            {
                return _FileName;
            }
            set
            {
                _FileName = value;
            }
        }

        [Browsable(false)]
        public DepthDiapason TrueDiapason
        {
            get
            {
                return _TrueDiapason;
            }
            set
            {
                _TrueDiapason = value;
            }
        }

        [Browsable(false)]
        public Planshet Planshet
        {
            get
            {
                return _Planshet;
            }
            set
            {
                _Planshet = value;
            }
        }

        [GDisplayName("Тип прибора")]
        [PropertyOrder(5)]
        [ReadOnly(true)]
        public string DeviceName
        {
            get
            {
                return Planshet.PriborName;
            }
            set
            {
                Planshet.PriborName = value;
            }
        }

        [ReadOnly(true)]
        [GDisplayName("Номер прибора")]
        [PropertyOrder(7)]
        public PriborNumber Device_Number
        {
            get
            {
                return Planshet.PriborNumber;
            }
            set
            {
            }
        }

        [GDisplayName("Device length", "Длина прибора")]
        [PropertyOrder(8)]
        [ReadOnly(true)]
        [TypeConverter(typeof(DeviceLengthesTypeConverter))]
        public DeviceLengthes DeviceLengthes
        {
            get
            {
                return _DeviceLengthes;
            }
            set
            {
                _DeviceLengthes = value;
            }
        }

        [ReadOnly(true)]
        [PropertyOrder(10)]
        [GDisplayName("Информация о регистраторе")]
        public LasInfoCollection RegistratorInfo
        {
            get
            {
                return _RegistratorInfo;
            }
            set
            {
                _RegistratorInfo = value;
            }
        }

        [ReadOnly(true)]
        [GDisplayName("Информация о скважине")]
        [PropertyOrder(20)]
        public WellInfo Well
        {
            get
            {
                return _Well;
            }
            set
            {
                _Well = value;
            }
        }

        [GDisplayName("Данные по исследованию")]
        [PropertyOrder(21)]
        [ReadOnly(true)]
        public ReferInfo ReferInfo => Well.ReferInfo;

        [ReadOnly(true)]
        [GDisplayName("Средняя скорость, м/ч")]
        [PropertyOrder(30)]
        public double AvgSpeed
        {
            get
            {
                return _AvgSpeed;
            }
            set
            {
                _AvgSpeed = Math.Round(value, 2);
            }
        }

        [PropertyOrder(40)]
        [GDisplayName("Шаг квантования, см")]
        [ReadOnly(true)]
        public virtual int Step
        {
            get
            {
                return _Step;
            }
            set
            {
                _Step = value;
            }
        }

        [Browsable(false)]
        public virtual DepthDiapason Diapason
        {
            get
            {
                return _Diapason;
            }
            set
            {
                _Diapason = value;
            }
        }

        [PropertyOrder(50)]
        [ReadOnly(true)]
        [GDisplayName("Depth diapason, m", "Интервал записи, м")]
        public virtual string DiapasonM
        {
            get
            {
                return (RegistredByTime ? TrueDiapason : Diapason).ToStringM();
            }
            set
            {
            }
        }

        [PropertyOrder(60)]
        [ReadOnly(true)]
        [GDisplayName("Время начала записи")]
        public DateTime StartDateTime
        {
            get
            {
                return _StartDateTime;
            }
            set
            {
                _StartDateTime = value;
            }
        }

        [GDisplayName("Время окончания записи")]
        [ReadOnly(true)]
        [PropertyOrder(70)]
        public DateTime EndDateTime
        {
            get
            {
                return _EndDateTime;
            }
            set
            {
                _EndDateTime = value;
            }
        }

        [Browsable(false)]
        public virtual RecDirection Direction
        {
            get
            {
                if (RegistredByTime)
                {
                    return RecDirection.ByTime;
                }
                return _Direction;
            }
            set
            {
                _Direction = value;
            }
        }

        [GDisplayName("Направление записи")]
        [ReadOnly(true)]
        public virtual string DirectionStr
        {
            get
            {
                return EnumTypeConverter.GetEnumName(Direction);
            }
            set
            {
            }
        }

        [ReadOnly(true)]
        [GDisplayName("Интервал изменён")]
        [TypeConverter(typeof(BooleanTypeConverter))]
        public bool Modifed
        {
            get
            {
                return _Modifed;
            }
            set
            {
                _Modifed = value;
            }
        }

        [Browsable(false)]
        public bool RegistredByTime
        {
            get
            {
                return _RegistredByTime;
            }
            set
            {
                _RegistredByTime = value;
            }
        }

        [Browsable(false)]
        public bool ReplaceDepthToTrueDepth
        {
            get
            {
                return _ReplaceDepthToTrueDepth;
            }
            set
            {
                _ReplaceDepthToTrueDepth = value;
            }
        }

        [Browsable(false)]
        public List<LasInfo> AdditionalGDTInfo { get; set; }

        [Browsable(false)]
        public List<BinaryTag> Tag
        {
            get
            {
                return _Tag;
            }
            set
            {
                _Tag = value;
            }
        }



        [Browsable(false)]
        public List<string> OtherFormatFiles
        {
            get
            {
                return _OtherFormatFiles;
            }
            set
            {
                _OtherFormatFiles = value;
            }
        }

        public IntervalInfo()
        {
        }

        public void Init(Planshet plt, DepthDiapason diap, RecSession s)
        {
            _TrueDiapason = s.TrueDiapason;
            DeviceLengthes = plt.GetLengthes();
            Step = s.depthStep;
            Planshet = plt;
            Diapason = diap;
            StartDateTime = DateTime.Now;
            double num = 0.0;
            int num2 = 0;
            DateTime? dateTime = null;
            DateTime? dateTime2 = null;
            for (long num3 = diap.Up; num3 < diap.Down; num3 += s.depthStep)
            {
                OneStepRecord data = s.GetData(num3);
                if (data != null)
                {
                    if (!dateTime.HasValue)
                    {
                        dateTime = data.Time;
                    }
                    dateTime2 = data.Time;
                    num += data.Speed / 1000.0;
                    num2++;
                }
            }
            if (!dateTime.HasValue)
            {
                dateTime = DateTime.Now;
            }
            if (!dateTime2.HasValue)
            {
                dateTime2 = DateTime.Now;
            }
            StartDateTime = DateTime.FromOADate(Math.Min(dateTime.Value.ToOADate(), dateTime2.Value.ToOADate()));
            EndDateTime = DateTime.FromOADate(Math.Max(dateTime.Value.ToOADate(), dateTime2.Value.ToOADate()));
            AvgSpeed = num / (double)num2 * 1000.0;
            Direction = RecDirection.Up;
            if (dateTime.HasValue && dateTime2.HasValue)
            {
                Direction = ((!(dateTime < dateTime2)) ? RecDirection.Up : RecDirection.Down);
            }
        }

        public IntervalInfo(Planshet plt, DepthDiapason diap, RecSession s)
        {
            Init(plt, diap, s);
        }

        public IntervalInfo(Planshet plt, DepthDiapason diap, RecSession s, WellInfo well)
        {
            Init(plt, diap, s);
            Well = well;
        }

        public LasInfoCollection GetWellInfo()
        {
            LasInfoCollection lasInfoCollection = new LasInfoCollection();
            lasInfoCollection.Add(new LasInfo_V2("WELL", "", Well.Well, "СКВАЖИНА"));
            lasInfoCollection.Add(new LasInfo_V2("KUST", "", Well.Kust, "КУСТ"));
            lasInfoCollection.Add(new LasInfo_V2("FLD", "", Well.Fld, "ПЛОЩАДЬ"));
            lasInfoCollection.Add(new LasInfo_V2("MEST", "", Well.Mest, "МЕСТОРОЖДЕНИЕ"));
            lasInfoCollection.Add(new LasInfo_V2("COMM", "", Well.Description, "ПРИМЧАНИЕ"));
            lasInfoCollection.Add(new LasInfo_V2("OPER", "", Well.Operator, "ОПЕРАТОР"));
            lasInfoCollection.Add(new LasInfo_V2("DATE", "", StartDateTime.ToShortDateString().Replace('.', '/'), "ДАТА РЕГИСТРАЦИИ"));
            lasInfoCollection.Add(new LasInfo_V2("TIME", "", StartDateTime.ToLongTimeString().Replace(':', '-'), "ВРЕМЯ РЕГИСТРАЦИИ"));
            lasInfoCollection.Add(new LasInfo_V2("DATO", "", EndDateTime.ToShortDateString().Replace('.', '/'), "ДАТА ОКОНЧАНИЯ РЕГИСТРАЦИИ"));
            lasInfoCollection.Add(new LasInfo_V2("TIMO", "", EndDateTime.ToLongTimeString().Replace(':', '-'), "ВРЕМЯ ОКОНЧАНИЯ РЕГИСТРАЦИИ"));
            return lasInfoCollection;
        }

        public LasInfoCollection GetCurveInfo()
        {
            LasInfoCollection lasInfoCollection = new LasInfoCollection();
            foreach (Elicom.Registration.Curve curf in Planshet.Curves)
            {
                LasInfo_V2 lasInfo = new LasInfo_V2(curf.Name, curf.Unit, "", "");
                if (curf is LineCurve)
                {
                    lasInfo.DESCRIPTION = (curf as LineCurve).Expression;
                }
                lasInfoCollection.Add(lasInfo);
            }
            return lasInfoCollection;
        }

        public virtual void Save(Stream s)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(GetType());
            xmlSerializer.Serialize(s, this);
        }

        public static IntervalInfo Load(Stream s)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(IntervalInfo));
            return (IntervalInfo)xmlSerializer.Deserialize(s);
        }

        public override string ToString()
        {
            return Planshet.Name.ToUpper() + " [" + EnumTypeConverter.GetEnumName(Direction).ToLower() + "] (" + Diapason.ToStringM() + ") [" + Utils.GetDateString(StartDateTime, forFileName: true) + "]";
        }
    }
    internal class ComLoc
    {
        private static ResourceManager resourceMan;

        private static CultureInfo resourceCulture;

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    ResourceManager resourceManager = new ResourceManager("Elicom.Properties.ComLoc", typeof(ComLoc).Assembly);
                    resourceMan = resourceManager;
                }
                return resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture
        {
            get
            {
                return resourceCulture;
            }
            set
            {
                resourceCulture = value;
            }
        }

        internal static string Acoustic => ResourceManager.GetString("Acoustic", resourceCulture);

        internal static string ADC => ResourceManager.GetString("ADC", resourceCulture);

        internal static string AssemblyFiles => ResourceManager.GetString("AssemblyFiles", resourceCulture);

        internal static string AssemblySettings => ResourceManager.GetString("AssemblySettings", resourceCulture);

        internal static string CalibrationFiles => ResourceManager.GetString("CalibrationFiles", resourceCulture);

        internal static string CalibrationInfo => ResourceManager.GetString("CalibrationInfo", resourceCulture);

        internal static string Calibrations => ResourceManager.GetString("Calibrations", resourceCulture);

        internal static string CanNotConnectToVulcan => ResourceManager.GetString("CanNotConnectToVulcan", resourceCulture);

        internal static string CanNotOpenFile => ResourceManager.GetString("CanNotOpenFile", resourceCulture);

        internal static string ClbCollection => ResourceManager.GetString("ClbCollection", resourceCulture);

        internal static string ConnectingToGektor => ResourceManager.GetString("ConnectingToGektor", resourceCulture);

        internal static string ConnectingToVulcan => ResourceManager.GetString("ConnectingToVulcan", resourceCulture);

        internal static string Constants => ResourceManager.GetString("Constants", resourceCulture);

        internal static string ConstantsFiles => ResourceManager.GetString("ConstantsFiles", resourceCulture);

        internal static string Count => ResourceManager.GetString("Count", resourceCulture);

        internal static string CurveName => ResourceManager.GetString("CurveName", resourceCulture);

        internal static string Curves => ResourceManager.GetString("Curves", resourceCulture);

        internal static string CurvesCollection => ResourceManager.GetString("CurvesCollection", resourceCulture);

        internal static string DepthMMSet => ResourceManager.GetString("DepthMMSet", resourceCulture);

        internal static string DepthModeSet => ResourceManager.GetString("DepthModeSet", resourceCulture);

        internal static string DepthSet => ResourceManager.GetString("DepthSet", resourceCulture);

        internal static string EndOfFileReached => ResourceManager.GetString("EndOfFileReached", resourceCulture);

        internal static string ErrCanNotConnectToGektor => ResourceManager.GetString("ErrCanNotConnectToGektor", resourceCulture);

        internal static string ErrDrvLoadNoAnswer => ResourceManager.GetString("ErrDrvLoadNoAnswer", resourceCulture);

        internal static string ErrNoAnswerOn => ResourceManager.GetString("ErrNoAnswerOn", resourceCulture);

        internal static string ErrNoResponseFromReg => ResourceManager.GetString("ErrNoResponseFromReg", resourceCulture);

        internal static string Error => ResourceManager.GetString("Error", resourceCulture);

        internal static string ErrRegistratonNotConnected => ResourceManager.GetString("ErrRegistratonNotConnected", resourceCulture);

        internal static string ErrVulcanNotFound => ResourceManager.GetString("ErrVulcanNotFound", resourceCulture);

        internal static string ExchangeStoped => ResourceManager.GetString("ExchangeStoped", resourceCulture);

        internal static string FileIsEmpty => ResourceManager.GetString("FileIsEmpty", resourceCulture);

        internal static string FileReadingStoped => ResourceManager.GetString("FileReadingStoped", resourceCulture);

        internal static string FileReadStarted => ResourceManager.GetString("FileReadStarted", resourceCulture);

        internal static string Files => ResourceManager.GetString("Files", resourceCulture);

        internal static string Formula => ResourceManager.GetString("Formula", resourceCulture);

        internal static string GainNSet => ResourceManager.GetString("GainNSet", resourceCulture);

        internal static string GektorConnected => ResourceManager.GetString("GektorConnected", resourceCulture);

        internal static string IfNoDataClear => ResourceManager.GetString("IfNoDataClear", resourceCulture);

        internal static string InterpalateExtremPoints => ResourceManager.GetString("InterpalateExtremPoints", resourceCulture);

        internal static string KMStepSet => ResourceManager.GetString("KMStepSet", resourceCulture);

        internal static string LoadingDriver => ResourceManager.GetString("LoadingDriver", resourceCulture);

        internal static string LoadingParams => ResourceManager.GetString("LoadingParams", resourceCulture);

        internal static string MakingLAS => ResourceManager.GetString("MakingLAS", resourceCulture);

        internal static string MarkNotPresent => ResourceManager.GetString("MarkNotPresent", resourceCulture);

        internal static string MarkPresent => ResourceManager.GetString("MarkPresent", resourceCulture);

        internal static string Mks => ResourceManager.GetString("Mks", resourceCulture);

        internal static string MMDistanceSet => ResourceManager.GetString("MMDistanceSet", resourceCulture);

        internal static string Name => ResourceManager.GetString("Name", resourceCulture);

        internal static string NewCurve => ResourceManager.GetString("NewCurve", resourceCulture);

        internal static string OpeningFile => ResourceManager.GetString("OpeningFile", resourceCulture);

        internal static string OSCDriverLoading => ResourceManager.GetString("OSCDriverLoading", resourceCulture);

        internal static string OSCDriverRusuming => ResourceManager.GetString("OSCDriverRusuming", resourceCulture);

        internal static string Oscilloscope => ResourceManager.GetString("Oscilloscope", resourceCulture);

        internal static string ParamC6Set => ResourceManager.GetString("ParamC6Set", resourceCulture);

        internal static string Paused => ResourceManager.GetString("Paused", resourceCulture);

        internal static string PhisValue => ResourceManager.GetString("PhisValue", resourceCulture);

        internal static string PorogNSet => ResourceManager.GetString("PorogNSet", resourceCulture);

        internal static string Q_ResumeData => ResourceManager.GetString("Q_ResumeData", resourceCulture);

        internal static string Q_WantExit => ResourceManager.GetString("Q_WantExit", resourceCulture);

        internal static string QRemoveAll => ResourceManager.GetString("QRemoveAll", resourceCulture);

        internal static string RecalculatingCurves => ResourceManager.GetString("RecalculatingCurves", resourceCulture);

        internal static string RecordingToFile => ResourceManager.GetString("RecordingToFile", resourceCulture);

        internal static string RecoveringData => ResourceManager.GetString("RecoveringData", resourceCulture);

        internal static string ReleSet => ResourceManager.GetString("ReleSet", resourceCulture);

        internal static string Remove => ResourceManager.GetString("Remove", resourceCulture);

        internal static string RemoveElements => ResourceManager.GetString("RemoveElements", resourceCulture);

        internal static string Resumed => ResourceManager.GetString("Resumed", resourceCulture);

        internal static string ReversSet => ResourceManager.GetString("ReversSet", resourceCulture);

        internal static string RoadsCollection => ResourceManager.GetString("RoadsCollection", resourceCulture);

        internal static string SavingCanceled => ResourceManager.GetString("SavingCanceled", resourceCulture);

        internal static string SavingCurves => ResourceManager.GetString("SavingCurves", resourceCulture);

        internal static string SelectCurves => ResourceManager.GetString("SelectCurves", resourceCulture);

        internal static string SelectDepthDiapason => ResourceManager.GetString("SelectDepthDiapason", resourceCulture);

        internal static string SelectFileName => ResourceManager.GetString("SelectFileName", resourceCulture);

        internal static string ServerExchangeStopped => ResourceManager.GetString("ServerExchangeStopped", resourceCulture);

        internal static string Settings => ResourceManager.GetString("Settings", resourceCulture);

        internal static string SourseValue => ResourceManager.GetString("SourseValue", resourceCulture);

        internal static string Spectr => ResourceManager.GetString("Spectr", resourceCulture);

        internal static string Start => ResourceManager.GetString("Start", resourceCulture);

        internal static string StartingServer => ResourceManager.GetString("StartingServer", resourceCulture);

        internal static string StartRecording => ResourceManager.GetString("StartRecording", resourceCulture);

        internal static string Stop => ResourceManager.GetString("Stop", resourceCulture);

        internal static string StopRecording => ResourceManager.GetString("StopRecording", resourceCulture);

        internal static string TCpServerError => ResourceManager.GetString("TCpServerError", resourceCulture);

        internal static string TCPServerStarted => ResourceManager.GetString("TCPServerStarted", resourceCulture);

        internal static string TCPServerStoped => ResourceManager.GetString("TCPServerStoped", resourceCulture);

        internal static string Threshold => ResourceManager.GetString("Threshold", resourceCulture);

        internal static string Time => ResourceManager.GetString("Time", resourceCulture);

        internal static string TurnOffAktor => ResourceManager.GetString("TurnOffAktor", resourceCulture);

        internal static string TurnOffBKK => ResourceManager.GetString("TurnOffBKK", resourceCulture);

        internal static string TurnOffGekat => ResourceManager.GetString("TurnOffGekat", resourceCulture);

        internal static string UISet => ResourceManager.GetString("UISet", resourceCulture);

        internal static string Voltage => ResourceManager.GetString("Voltage", resourceCulture);

        internal static string VulcanConnected => ResourceManager.GetString("VulcanConnected", resourceCulture);

        internal static string Waves => ResourceManager.GetString("Waves", resourceCulture);

        internal static string Window => ResourceManager.GetString("Window", resourceCulture);

        internal ComLoc()
        {
        }
    }
}
