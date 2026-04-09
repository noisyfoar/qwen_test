using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPFGEO;
using NPFGEO.Data;
using NPFGEO.IO;
using ShellExtension.Formats.LIS;
using ShellExtension.Formats.LIS.Dialogs;
using ImportDialogLisView = NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.View.ImportDialogLIS;
using ImportDialogLisViewModel = NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel.ImportDialogLIS;

namespace ShellExtension.Formats
{
    public class DLIS_Format : BaseFormat, IGEOFormat, IFileReader, IFileWriter
    {
        public override bool IsValid(string fileName)
        {
            throw new NotImplementedException();
        }
        public override bool IsValid(Curve curve)
        {
            return true;
        }

        public Curves Scan(System.IO.Stream source, FieldDictionary readParams)
        {
            throw new NotImplementedException();
        }

        static string ConvertCurveName(string name)
        {
            return name.Replace("\0", string.Empty);
        }
        public Curves Read(string fileName)
        {
            LISReader reader = new LISReader(fileName);
            reader.Scan();

            Curves result = new Curves();
            // из Block128
            result.InfoFields.Add(new Field("File Name", reader.Block128Data.FileName));
            result.InfoFields.Add(new Field("Service Sub Level Name", reader.Block128Data.ServiceSubLevelName));
            result.InfoFields.Add(new Field("Version Number", reader.Block128Data.Version));
            result.InfoFields.Add(new Field("Date Of Generation", reader.Block128Data.ReceiveDate));
            result.InfoFields.Add(new Field("Maximum Physical Record Length", reader.Block128Data.MaxPhisRecLen));
            result.InfoFields.Add(new Field("File Type", reader.Block128Data.FileType));
            result.InfoFields.Add(new Field("Optional Previous File Name", reader.Block128Data.PrevFileName));

            string depthUnits = reader.Block64Data.DepthUnits;

            foreach (LIS.LIS.Block64Datum01 datum in reader.Block64Data.Datum01)
            {
                Curve crv = new Curve();
                String mnem = datum.Mnemonic;
                String WaveName = ConvertCurveName(mnem);
                Byte subType = reader.Block64Data.DatumSubType;

                // Параметры кривой

                crv.Caption = WaveName;
                crv.Category = Path.GetFileName(fileName);
                crv.NullValue = reader.Block64Data.NullValue;
                crv.SetSource(fileName);
                crv.SetDataUnit(datum.Units);

                // данные из подтипа 0 или 1 спецификации Datum
                crv.InfoFields.Add(new Field("Mnemonic", WaveName));
                crv.InfoFields.Add(new Field("Service ID", datum.Data_Size));
                crv.InfoFields.Add(new Field("Service Order Nb", datum.WorkNumber));
                crv.InfoFields.Add(new Field("Units", datum.WorkNumber));

                if (subType == 1)
                    crv.InfoFields.Add(new Field("APICodes", datum.APIType));
                else
                {
                    crv.InfoFields.Add(new Field("API Log Type", datum.APIWorkType));
                    crv.InfoFields.Add(new Field("API Curve Type", datum.APICrvType));
                    crv.InfoFields.Add(new Field("API Curve Class", datum.APICrvClass));
                    crv.InfoFields.Add(new Field("API Modifier", datum.APIType));
                }

                crv.InfoFields.Add(new Field("File Nb", datum.FileNumber));
                crv.InfoFields.Add(new Field("Size", datum.Data_Size));

                if (subType != 1)
                    crv.InfoFields.Add(new Field("Process Level", datum.ProcessLevel));

                crv.InfoFields.Add(new Field("Nb Samples", datum.DataCount));
                crv.InfoFields.Add(new Field("Representation Code", datum.ViewCode));

                if (subType == 1)
                    crv.InfoFields.Add(new Field("Process Indicators", datum.ProcessLevel));

                int byteColumns = reader.GetDataSize(mnem);
                int columns = reader.GetDataCount(mnem);

                List<LISRecord> records = reader.GetData(mnem);
                var depthUnitKoef = DetermineDepthKoef(depthUnits);

                if (records.Any(r => r.FramesCount > 1))
                    records = RecordsExpansion(records);

                records = records.OrderBy(a => a.Depth).ToList();
                int rows = records.Count - 1;
                result.Add(crv, eTypeConflictSolution.Rename);


                double[,] depth = new double[rows, 1];
                for (int i = 0; i < rows; i++)
                    depth[i, 0] = Math.Round(records[i].Depth / depthUnitKoef, 4);

                Matrix<double> DepthMatrix = new MemoryMatrix<double>(depth);
                bool reverse = DepthMatrix.IsDecreasing();

                IMMatrix data = null;
                var itemSize = byteColumns / columns;

                if (datum.ViewCode == 73)
                {
                    data = ToIntMatrix(records, columns, rows, reverse);
                }
                else
                    switch (itemSize)
                    {
                        case 1:
                            data = ToByteMatrix(records, columns, rows);
                            break;
                        case 2:
                            data = ToShortMatrix(records, columns, rows, reverse, WaveName);
                            break;
                        case 4:
                            data = ToFloatMatrix(records, columns, rows, reverse, WaveName);
                            break;
                    }

                if (columns > 1)
                    if(!newApplyBeginEnd(crv, reader, columns))
                    {
                        if(!oldApplyBeginEnd(crv, reader, columns))
                        {
                            // By Default
                            // Реализовать ручной ввод начала и/или дельты и/или конца
                            //(2 обязательны из 3)
                            crv.SetBegin(0);
                            crv.SetEnd((int)(columns - 1) * 3); //       End = (int)(Begin + ((columns - 1) * Delta)); begin = 0, delta = 2;
                        }
                    }

                if (reverse)
                    crv.SetData(data, new MemoryMatrix<double>(depth).Reverse(false, true));
                else
                    crv.SetData(data, new MemoryMatrix<double>(depth));

            }

            var importDialogVm = new ImportDialogLisViewModel(result);
            var importDialog = new ImportDialogLisView
            {
                DataContext = importDialogVm
            };

            var dialogResult = importDialog.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
                return importDialogVm.SelectedCurves;

            return null;
        }

        private IMMatrix ToIntMatrix(List<LISRecord> records, int columns, int rows, bool reverse)
        {
            return ToIntMemoryMatrix(records, columns, rows, reverse);
        }

        private IMMatrix ToIntMemoryMatrix(List<LISRecord> records, int columns, int rows, bool reverse)
        {
            var intData = new int[rows, columns];

            /* for (int i = 0; i < rows; i++)
             {
                 var record = records[rows - i - 1];
                 var maxColumns = Math.Min(record.Data.Length / 4, columns);
                 Buffer.BlockCopy(records[i].Data, 0, intData, maxColumns * i * 4, maxColumns * 4);
             }*/

            if (reverse)
                for (int i = 0; i < rows; i++)
                {
                    var record = records[rows - i - 1];
                    for (int j = 0; j < columns; j++)
                    {
                        int convert = BitConverter.ToInt32(record.Data.Skip(j * 4).Take(4).Reverse().ToArray(), 0);
                        intData[i, j] = convert;
                    }
                }
            else
                for (int i = 0; i < rows; i++)
                {
                    var record = records[i];
                    for (int j = 0; j < columns; j++)
                    {
                        int convert = BitConverter.ToInt32(record.Data.Skip(j * 4).Take(4).Reverse().ToArray(), 0);
                        intData[i, j] = convert;
                    }
                }

            return new MemoryMatrix<int>(intData);
        }

        private List<LISRecord> RecordsExpansion(List<LISRecord> records)
        {
            double step;
            bool isReverse;

            if (records[0].Depth > records[1].Depth)
            {
                isReverse = false;
                step = (records[0].Depth - records[1].Depth) / (records[0].FramesCount);
            }
            else
            {
                isReverse = true;
                step = (records[1].Depth - records[0].Depth) / (records[1].FramesCount);

                /*
                 * При написании данной функции у меня не было LIS-файлов, в которых хотя бы у одного LISRecord
                 * кол-во framesCount было больше 0 и порядок глубин был бы от меньшей к большей.
                 * Как следствие, я не могу гарантировать правильность выполнения функции RecordsExpansion(...)
                 * Если вы оказались в этом блоке else, отправьте LIS файл на podkalyukda@npf-geofizika.ru
                 * или отладьте функцию RecordsExpansion(...) самостоятельно
                */

                //throw new NotImplementedException();
            }

            var newCount = records.Select(r => r.FramesCount).Sum();
            List<LISRecord> newlist = new List<LISRecord>(newCount);

            foreach (var r in records)
                for (int i = 0; i < r.FramesCount; i++)
                {
                    var newData = new byte[r.FrameSize];
                    for (int j = 0; j < r.FrameSize; j++)
                        newData[j] = r.Data[i * r.FrameSize + j];
                    newlist.Add(new LISRecord(!isReverse ? r.Depth - i * step : r.Depth + i * step, newData, r.FrameSize, 1));
                }

            return newlist;
        }

        double DetermineDepthKoef(string unit)
        {
            switch (unit)
            {
                case "CM  ":
                    return 100.0;
                case "MM  ":
                    return 1000.0;
                default:
                    return 100.0;
            }
        }
        bool newApplyBeginEnd(NPFGEO.Data.Curve crv, LISReader reader, int columns)
        {
            double beginValue, endValue, deltaValue;


            string deltaString = reader.GetBlock34Param(crv.Caption, "delt", 4);
            string beginString = reader.GetBlock34Param(crv.Caption, "init", 4);

            if ((((deltaString != null) && (beginString != null)) &&
                 (int.TryParse(deltaString, out int deltaFromTable) && int.TryParse(beginString, out int beginFromTable))) &&
                (deltaFromTable > 0))
            {
                deltaValue = deltaFromTable;
                beginValue = beginFromTable;

                crv.SetBegin(beginValue);
                endValue = (int)(beginValue + ((columns - 1) * deltaValue));
                crv.SetEnd(endValue);
                return true;
            }
            return false;
        }
        bool oldApplyBeginEnd(NPFGEO.Data.Curve crv, LISReader reader, int columns)
        {
            double beginValue, endValue, deltaValue;
            uint? channelNumber = GetIndex(crv.Caption);
            if (channelNumber == null) channelNumber = 1;

            string deltaString = reader.GetBlock34Param("CONS", "WFS" + channelNumber.Value.ToString(), 4);
            string beginString = reader.GetBlock34Param("CONS", "WFZ" + channelNumber.Value.ToString(), 4); 

            if ((((deltaString != null) && (beginString != null)) 
                &&
                 (int.TryParse(deltaString, out int deltaFromTable)
                 && 
                 int.TryParse(beginString, out int beginFromTable)))
                 &&
                (deltaFromTable > 0))
            {
                deltaValue = deltaFromTable;
                beginValue = beginFromTable;
                endValue = (int)(beginValue + ((columns - 1) * deltaValue));

                crv.SetBegin(beginValue);
                crv.SetEnd(endValue);
                return true;
            }
            else if (Convert.ToInt32(reader.GetBlock34Param("CONS", "WFS", 4)) > 0)
            {
                deltaValue = Convert.ToInt32(reader.GetBlock34Param("CONS", "WFS", 4));
                beginValue = Convert.ToInt32(reader.GetBlock34Param("CONS", "WSD", 4));
                endValue = (int)(beginValue + ((columns - 1) * deltaValue));

                crv.SetBegin(beginValue);
                crv.SetEnd(endValue);
                return true;
            }
            return false;

        }

        uint? GetIndex(string WaveName)
        {
            WaveName = WaveName.Trim().Trim('_');

            uint? num8 = null;
            try { num8 = Convert.ToUInt32(WaveName[WaveName.Length - 1].ToString()); }
            catch (FormatException) { }
            return num8;
        }

        Matrix<byte> ToByteMatrix(IList<LISRecord> records, int columns, int rows)
        {
            byte[,] bytesData = new byte[rows, columns];
            for (int i = 0; i < rows; i++)
                Buffer.BlockCopy(records[i].Data, 0, bytesData, columns * i, columns);
            var data = new MemoryMatrix<byte>(bytesData);
            return data;
        }

        Matrix<short> ToShortMatrix(IList<LISRecord> records, int columns, int rows, bool reverse, string waveName)
        {
            if (columns > 1)
                return ToShortStreamMatrix(records, columns, rows, reverse, waveName);
            else
                return ToShortMemoryMatrix(records, columns, rows, reverse);
        }

        MemoryMatrix<short> ToShortMemoryMatrix(IList<LISRecord> records, int columns, int rows, bool reverse)
        {
            short[,] shortData = new short[rows, columns];

            int column;
            if (reverse)
                for (int i = 0; i < rows; i++)
                {
                    var record = records[rows - i - 1];

                    if (record.Data.Length < columns * 2)
                        for (column = 0; column < columns; column++)
                            shortData[i, column] = short.MinValue;
                    else
                        for (column = 0; column < columns; column++)
                            shortData[i, column] = (short)(record.Data[2 * column + 1] | record.Data[2 * column + 0] << 8);
                }
            else
                for (int i = 0; i < rows; i++)
                {
                    var record = records[i];

                    if (record.Data.Length < columns * 2)
                        for (column = 0; column < columns; column++)
                            shortData[i, column] = short.MinValue;
                    else
                        for (column = 0; column < columns; column++)
                            shortData[i, column] = (short)(record.Data[2 * column + 1] | record.Data[2 * column + 0] << 8);
                }
            var data = new MemoryMatrix<short>(shortData);
            return data;
        }

        StreamMatrix<short> ToShortStreamMatrix(IList<LISRecord> records, int columns, int rows, bool reverse, string waveName)
        {
            var dir = Path.GetTempPath() + @"Genesis";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var tmpFileName = dir + @"\" + waveName;

            if (File.Exists(tmpFileName))
            {
                for (int i = 1; ; i++)
                {
                    var newFileName = tmpFileName + "_" + i.ToString();
                    if (!File.Exists(newFileName))
                    {
                        tmpFileName = newFileName;
                        break;
                    }
                }
            }

            Stream stream = new FileStream(tmpFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
            stream.SetLength(columns * rows * 2);

            var data = new StreamMatrix<short>(stream, columns, rows);

            var line = new short[columns];
            int column;

            for (int i = 0; i < rows; i++)
            {
                var record = records[i];

                if (record.Data.Length < columns * 2)
                    for (column = 0; column < columns; column++)
                        line[column] = short.MinValue;
                else
                    for (column = 0; column < columns; column++)
                    {
                        line[column] = (short)(record.Data[2 * column + 1] | record.Data[2 * column + 0] << 8);
                    }
                if (!reverse)
                {
                    data.ReplaceRow(i, line);
                }
                else
                {
                    data.ReplaceRow(rows - i - 1, line);
                }

            }
            return data;
        }

        Matrix<float> ToFloatMatrix(IList<LISRecord> records, int columns, int rows, bool reverse, string waveName)
        {
            return ToFloatMemoryMatrix(records, columns, rows, reverse);
        }

        Matrix<float> ToFloatMemoryMatrix(IList<LISRecord> records, int columns, int rows, bool reverse)
        {
            float[,] floatData = new float[rows, columns];

            if (reverse)
            {
                for (int i = 0; i < rows; i++)
                {
                    var record = records[rows - i - 1];
                    var maxColumns = Math.Min(record.Data.Length / 4, columns);
                    Buffer.BlockCopy(records[i].Data, 0, floatData, maxColumns * i * 4, maxColumns * 4);
                }
            }
            else
            {
                //for (int i = 0; i < rows; i++)
                //{
                //    var record = records[i];
                //    var maxColumns = Math.Min(record.Data.Length / 4, columns);
                //    Buffer.BlockCopy(records[i].Data, 0, floatData, maxColumns * i * 4, maxColumns * 4);
                //}
                for (int i = 0; i < rows; i++)
                {
                    var record = records[i];
                    //for (int j = 0; j < record.Data.Length / 4; j++)
                    //{
                    //    if (cnt < floatData.Length)
                    //    {
                    //        var maxColumns = Math.Min(record.Data.Length / 4, columns);
                    //        uint firstConvert = BitConverter.ToUInt32(record.Data.Skip(maxColumns * j * 4).Take(4).ToArray(), 0);
                    //        float secondConvert = LIS.LIS.LISfloat2IEEE(firstConvert);
                    //        floatData[cnt, 0] = secondConvert;
                    //    }
                    //    else break;
                    //    cnt++;
                    //}
                    for (int j = 0; j < columns; j++)
                    {
                        uint firstConvert = BitConverter.ToUInt32(record.Data.Skip(j * 4).Take(4).ToArray(), 0);
                        float secondConvert = LIS.LIS.LISfloat2IEEE(firstConvert);
                        floatData[i, j] = secondConvert;
                    }
                }
            }


            var data = new MemoryMatrix<float>(floatData);
            return data;
        }

        public void Write(NPFGEO.Data.Curves curves, System.IO.Stream source, FieldDictionary writeParams)
        {
            throw new NotImplementedException();
        }

        public void Write(Curves curves, string fileName)
        {
            LisExportSettingsWindowVM vm = new LisExportSettingsWindowVM();
            LisExportSettingsWindow dialog = new LisExportSettingsWindow();
            dialog.DataContext = vm;

            var curvesWithStep = curves.Where(c => c.Step != 0);
            if (curvesWithStep.Any())
                vm.Step = curvesWithStep?.Min(c => c.Step) ?? 0.1;
            else
                vm.Step = 0.1;

            var resultDialog = dialog.ShowDialog();
            if (resultDialog.HasValue && resultDialog.Value)
            {
                RecSession session = new RecSession(vm.Step, fileName);
                session.SetStepKoef(vm.LISstepKoef);
                session.SetLISStepUnit(vm.LISstepUnit);

                var copyCurves = new Curves();
                copyCurves.AddRange(curves.Select(c => c.DeepClone()));

                PrepareCurves(copyCurves, vm.Step);

                var depthDiapason = new DepthDiapason((long)Math.Round(copyCurves.Min(c => c.Roof) * vm.LISstepKoef, MidpointRounding.AwayFromZero),
                                                      (long)Math.Round(copyCurves.Max(c => c.Foot) * vm.LISstepKoef, MidpointRounding.AwayFromZero));
                depthDiapason.SetDepthKoef(vm.LISstepKoef);

                session.SaveCurvesToLIS(depthDiapason, copyCurves, fileName, null, true);
                GC.Collect(2, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
            }
        }

        private void PrepareCurves(Curves curves, double minStep)
        {
            foreach (var c in curves)
            {
                if (c.Step == minStep)
                    continue;
                try
                {
                    var count = (int)((c.Foot - c.Roof) / minStep);
                    var ndArr = new List<double>(count);
                    var curDepth = c.Roof;
                    for (int i = 0; i < count; i++)
                    {
                        ndArr.Add(curDepth);
                        curDepth += minStep;
                    }

                    var newDepth = new MemoryMatrix<double>(ndArr.ToArray());
                    var newData = NPFGEO.Data.Interpolation.NextNeighbor_universal(c.DepthMatrix, c.DataMatrix, newDepth);
                    c.SetData(newData, newDepth);
                }
                catch { }
            }

            foreach (var c in curves)
            {
                if (c.DepthMatrix[0] / c.Step == Math.Round(c.DepthMatrix[0] / c.Step))
                    continue;

                var count = c.DepthMatrix.Count;
                var newDepth = new double[count];
                for (int i = 0; i < count; i++)
                {
                    var nd = Math.Round(c.DepthMatrix[i] / c.Step) * c.Step;

                    //отсекаем знаки 0.0000002 и тд возникшие из-за операций с типом double
                    newDepth[i] = Math.Round(nd, 6);
                }

                c.SetData(c.DataMatrix, new MemoryMatrix<double>(newDepth));
            }

        }

        public eFormatType Type { get { return eFormatType.ReadAndWrite; } }
        public string Name { get { return "LIS"; } }
        public IEnumerable<string> FileExtensions { get { return new string[] { "LIS" }; } }
    }
}
