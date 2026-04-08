using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellExtension.Formats.LIS
{
    public class LISWriter : LIS
    {
        // Fields
        private LIS.Block128 _Block128 = new LIS.Block128();
        private List<LIS.Block34Datum> _Block34;
        private List<LIS.Block34Table> _Block34Tables = new List<LIS.Block34Table>();
        private LIS.Block64 _Block64 = new LIS.Block64();
        private int _CodePage = 0x362;
        private byte[] _DataToWrite;
        private Encoding _Encoding;
        private string _FileName;
        private bool _Opened;
        private bool _WasBlock128;
        private bool _WasBlock64;
        private BinaryWriter bw;
        private FileStream fs;
        private int Prev;
        private int Start_Depth;

        // Methods
        public LISWriter(string fileName)
        {
            this._FileName = fileName;
            this._Encoding = Encoding.GetEncoding(this._CodePage);
            this.InitBlock128();
            this.InitBlock64();
        }

        private void Close()
        {
            this.Opened = false;
            this.bw.Flush();
            this.fs.Flush();
            this.bw.Close();
            this.fs.Close();
        }

        private void InitBlock128()
        {
            this._Block128.FileName = Path.GetFileNameWithoutExtension(this._FileName);
            this._Block128.FileType = "LO";
            this._Block128.MaxPhisRecLen = "16000";
            this._Block128.PrevFileName = "";
            this._Block128.ReceiveDate = DateTime.Now.ToShortDateString();
            this._Block128.ServiceSubLevelName = "REGIST";
            this._Block128.Version = "REG 002";
        }

        private void InitBlock64()
        {
            this._Block64.RecordType = 0;
            this._Block64.DatumSpec = 0;
            this._Block64.DataFrameSize = 100;
            this._Block64.UpDown = 1;
            this._Block64.Units = 0;
            this._Block64.SourcePoint = 0f;
            this._Block64.SourcePointUnits = "CM  ";
            this._Block64.CadreStep = 0f;
            this._Block64.CadreStepUnits = "CM  ";
            this._Block64.MaxCadre = 10;
            this._Block64.NullValue = 0f;
            this._Block64.DepthMode = 1;
            this._Block64.DepthUnits = "CM  ";
            this._Block64.DepthViewCode = 0x44;
        }

        private void Open()
        {
            this.fs = new FileStream(this.FileName, FileMode.Create);
            this.bw = new BinaryWriter(this.fs);
            this.Opened = true;
        }

        private void OpenResume()
        {
            this.fs = new FileStream(this.FileName, FileMode.Create);
            this.bw = new BinaryWriter(this.fs);
            this.fs.Seek(0L, SeekOrigin.End);
            this.Opened = true;
        }

        public void WriteBlock128()
        {
            if (!this.Opened)
            {
                throw new LISException("Файл не открыт. Запись невозможна.");
            }
            this.WriteBlockHeader(this._Block128.Size, 128);
            this.WriteStringParam(this._Block128.FileName, 10);
            this.WriteStringParam("  ", 2);
            this.WriteStringParam(this._Block128.ServiceSubLevelName, 6);
            this.WriteStringParam(this._Block128.Version, 8);
            this.WriteStringParam(this._Block128.ReceiveDate, 8);
            this.WriteStringParam(" ", 1);
            this.WriteStringParam(this._Block128.MaxPhisRecLen, 5);
            this.WriteStringParam("  ", 2);
            this.WriteStringParam(this._Block128.FileType, 2);
            this.WriteStringParam("  ", 2);
            this.WriteStringParam(this._Block128.PrevFileName, 10);
            this._WasBlock128 = true;
        }

        public void WriteBlock34(LIS.Block34Datum datum)
        {
            this.WriteBlockHeader(datum.ComponentSize, 0x22);
            LIS.Block34Component component = new LIS.Block34Component(0x49, 0x41, (byte)datum.Name.Length, 0, datum.Mnemonic, datum.Name);
            this.WriteBlock34Component(component);
            foreach (LIS.Block34Component component2 in datum.Components)
            {
                this.WriteBlock34Component(component2);
            }
        }

        private void WriteBlock34Component(LIS.Block34Component component)
        {
            this.bw.Write(component.Type);
            this.bw.Write(component.ViewCode);
            this.bw.Write(component.Size);
            this.bw.Write(component.Category);
            this.WriteStringParam(component.Mnemonic, 4);
            this.WriteStringParam(component.Uint, 4);
            switch (component.ViewCode)
            {
                case 0x49:
                    this.bw.Write(LIS.Swap((int)component.Component));
                    return;

                case 0x4f:
                    this.bw.Write(LIS.Swap((ushort)component.Component));
                    return;

                case 0x41:
                    this.WriteStringParam((string)component.Component, component.Size);
                    return;

                case 0x44:
                    this.bw.Write(LIS.IEEEfloat2LIS((float)component.Component));
                    return;
            }
            throw new LISException("Тип " + component.ViewCode + " для блока 34 не раелизован.");
        }

        public void WriteBlock64()
        {
            if (!this._WasBlock128)
            {
                throw new LISException("Необходимо сначала записать блок 128");
            }
            if (!this.Opened)
            {
                throw new LISException("Файл не открыт. Запись невозможна.");
            }
            this.WriteBlockHeader(this._Block64.Size, 0x40);
            this.WriteBlock64Param(this._Block64.RecordType, 1);
            this.WriteBlock64Param(this._Block64.DatumSpec, 2);
            this.WriteBlock64Param(this._Block64.DataFrameSize, 3);
            this.WriteBlock64Param(this._Block64.UpDown, 4);
            this.WriteBlock64Param(this._Block64.Units, 5);
            this.WriteBlock64Param(this._Block64.SourcePoint, 6);
            this.WriteBlock64Param(this._Block64.SourcePointUnits, 7);
            this.WriteBlock64Param(this._Block64.CadreStep, 8);
            this.WriteBlock64Param(this._Block64.CadreStepUnits, 9);
            this.WriteBlock64Param(this._Block64.MaxCadre, 11);
            this.WriteBlock64Param(this._Block64.NullValue, 12);
            this.WriteBlock64Param(this._Block64.DepthMode, 13);
            this.WriteBlock64Param(this._Block64.DepthUnits, 14);
            this.WriteBlock64Param(this._Block64.DepthViewCode, 15);
            this.WriteBlock64Param(this._Block64.DatumSubType, 0x10);
            this.WriteBlock64Param((byte)0, 0);
            foreach (LIS.Block64Datum01 datum in this._Block64.Datum01)
            {
                this.WriteBlock64Datum(datum);
            }
            this._WasBlock64 = true;
        }

        private void WriteBlock64Datum(LIS.Block64Datum01 datum)
        {
            this.WriteStringParam(datum.Mnemonic, 4);
            this.WriteStringParam(datum.WorkID, 6);
            this.WriteStringParam(datum.WorkNumber, 8);
            this.WriteStringParam(datum.Units, 4);
            this.bw.Write(datum.APIWorkType);
            this.bw.Write(datum.APICrvType);
            this.bw.Write(datum.APICrvClass);
            this.bw.Write(datum.APIType);
            this.bw.Write(LIS.Swap(datum.FileNumber));
            this.bw.Write(LIS.Swap(datum.Data_Size));
            this.bw.Write(LIS.Swap(datum.Reserve1));
            this.bw.Write(datum.ProcessLevel);
            this.bw.Write(datum.DataCount);
            this.bw.Write(datum.ViewCode);
            this.bw.Write(datum.Reserve2);
            this.bw.Write(LIS.Swap(datum.Reserve3));
        }

        private void WriteBlock64Param(object param, byte type)
        {
            this.bw.Write(type);
            if (param is byte)
            {
                this.bw.Write((byte)1);
                this.bw.Write((byte)0x42);
                this.bw.Write((byte)param);
            }
            else if (param is ushort)
            {
                this.bw.Write((byte)2);
                this.bw.Write((byte)0x4f);
                this.bw.Write(LIS.Swap((ushort)param));
            }
            else if (param is int)
            {
                this.bw.Write((byte)4);
                this.bw.Write((byte)0x49);
                this.bw.Write(LIS.Swap((int)param));
            }
            else if (param is string)
            {
                byte[] bytes = this._Encoding.GetBytes((string)param);
                this.bw.Write((byte)bytes.Length);
                this.bw.Write((byte)0x41);
                this.bw.Write(bytes);
            }
            else if (param is float)
            {
                this.bw.Write((byte)4);
                this.bw.Write((byte)0x44);
                this.bw.Write(LIS.IEEEfloat2LIS((float)param));
            }
        }

        private void WriteBlockHeader(int blockSize, byte blockType)
        {
            int position = (int)this.fs.Position;
            int next = (((position + blockSize) + LIS.TIF.Size) + LIS.PhisicalHeader.Size) + LIS.LogicalHeader.Size;
            this.WriteTif(0, this.Prev, next);
            this.Prev = position;
            this.WritePhisicalHeader((ushort)((blockSize + LIS.PhisicalHeader.Size) + LIS.LogicalHeader.Size), 0);
            this.WriteLogicalHeader(blockType, 0);
        }

        public void WriteDataBlock(LISRecord lisData)
        {
            if (!this._WasBlock64)
            {
                throw new LISException("Необходимо сначала записать блок 64");
            }
            if (!this.Opened)
            {
                throw new LISException("Файл не открыт. Запись невозможна.");
            }
            this.WriteBlockHeader(lisData.Data.Length + LIS.GetLISValueSize(this._Block64.DepthViewCode), 0);
            if (this._Block64.DepthViewCode == 0x44)
            {
                this.bw.Write(LIS.IEEEfloat2LIS((float)lisData.Depth));
            }
            else
            {
                if (this._Block64.DepthViewCode != 0x49)
                {
                    throw new LISException("Формат глубины не поддерживается: " + this._Block64.DepthViewCode.ToString());
                }
                this.bw.Write(LIS.Swap((int)lisData.Depth));
            }
            this.bw.Write(lisData.Data);
        }

        private void WriteFinalTIF()
        {
            this.WriteTif(0, this.Prev, ((int)this.fs.Position) + 12);
        }

        public void WriteLIS()
        {
            try
            {
                this.Open();
                if (((this.Block64Data.DataSize == 0) || (this.Block64Data.Datum01.Length == 0)) || ((this._DataToWrite.Length % this.Block64Data.DataSize) != 0))
                {
                    throw new LISException("Неверный размер данных");
                }
                this.WriteBlock128();
                foreach (LIS.Block34Table table in this.Block34Tables)
                {
                    this.WriteBlock34(table.MakeBlock34Datum());
                }
                if (this.Block34Data != null)
                {
                    foreach (LIS.Block34Datum datum in this.Block34Data)
                    {
                        this.WriteBlock34(datum);
                    }
                }
                this.WriteBlock64();
                int num = this._DataToWrite.Length / this.Block64Data.DataSize;
                int num2 = Convert.ToInt32(this.Block128Data.MaxPhisRecLen);
                if (num2 == 0)
                {
                    num2 = 0x3e80;
                }
                int framesCount = (int)Math.Floor((double)(((double)num2) / ((double)this.Block64Data.DataSize)));
                if (framesCount == 0)
                {
                    framesCount = 1;
                }
                if (framesCount > this.Block64Data.MaxCadre)
                {
                    framesCount = this.Block64Data.MaxCadre;
                }
                int num4 = (int)Math.Floor((double)(((double)num) / ((double)framesCount)));
                int num5 = num % framesCount;
                MemoryStream input = new MemoryStream(this._DataToWrite);
                BinaryReader reader = new BinaryReader(input);
                int num6 = this.Start_Depth;
                for (int i = 0; i < num4; i++) // TODO Разделить на фиксированный и нефиксированный шаг
                {
                    this.WriteDataBlock(new LISRecord((double)num6, reader.ReadBytes(framesCount * this.Block64Data.DataSize), this.Block64Data.DataSize, framesCount));
                    int num8 = (int)(this.Block64Data.CadreStep * framesCount);
                    if (this._Block64.UpDown == 1)
                    {
                        num6 -= num8;
                    }
                    else
                    {
                        num6 += num8;
                    }
                }
                this.WriteDataBlock(new LISRecord((double)num6, reader.ReadBytes(num5 * this.Block64Data.DataSize), this.Block64Data.DataSize, num5));
                this.WriteFinalTIF();
            }
            catch (Exception exception)
            {
                throw new LISException(exception.Message);
            }
            finally
            {
                this.Close();
            }
        }

        private void WriteLogicalHeader(byte type, byte reserve)
        {
            this.bw.Write(type);
            this.bw.Write(reserve);
        }

        private void WritePhisicalHeader(ushort length, ushort attributes)
        {
            this.bw.Write(LIS.Swap(length));
            this.bw.Write(LIS.Swap(attributes));
        }

        private void WriteStringParam(string param, int size)
        {
            byte[] bytes = this._Encoding.GetBytes(param);
            if (bytes.Length >= size)
            {
                this.bw.Write(bytes, 0, size);
            }
            else
            {
                this.bw.Write(bytes);
                byte[] buffer = new byte[size - bytes.Length];
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = this._Encoding.GetBytes(" ")[0];
                }
                this.bw.Write(buffer);
            }
        }

        private void WriteTif(int type, int prev, int next)
        {
            this.bw.Write(type);
            this.bw.Write(prev);
            this.bw.Write(next);
        }

        // Properties
        public LIS.Block128 Block128Data
        {
            get
            {
                return this._Block128;
            }
            set
            {
                this._Block128 = value;
            }
        }

        public List<LIS.Block34Datum> Block34Data
        {
            get
            {
                return this._Block34;
            }
            set
            {
                this._Block34 = value;
            }
        }

        public List<LIS.Block34Table> Block34Tables
        {
            get
            {
                return this._Block34Tables;
            }
            set
            {
                this._Block34Tables = value;
            }
        }

        public LIS.Block64 Block64Data
        {
            get
            {
                return this._Block64;
            }
            set
            {
                this._Block64 = value;
            }
        }

        public int CodePage
        {
            get
            {
                return this._CodePage;
            }
            set
            {
                this._CodePage = value;
                this._Encoding = Encoding.GetEncoding(this._CodePage);
            }
        }

        public byte[] DataToWrite
        {
            get
            {
                return this._DataToWrite;
            }
            set
            {
                this._DataToWrite = value;
            }
        }

        public string FileName
        {
            get
            {
                return this._FileName;
            }
        }

        public bool Opened
        {
            get
            {
                return this._Opened;
            }
            set
            {
                this._Opened = value;
            }
        }

        public int StartDepth
        {
            get
            {
                return this.Start_Depth;
            }
            set
            {
                this.Start_Depth = value;
            }
        }
        public void AddLasInfo(string tableName, LasInfoCollection col)
        {
            Block34Table block34Table = new Block34Table(tableName);
            block34Table.MakeColumns("MNEM", "UNIT", "DATA", "DESC");
            foreach (LasInfo_V2 item in col)
            {
                block34Table.AddRow(item.MNEM, item.UNITS, item.DATA, item.DESCRIPTION);
            }
            Block34Tables.Add(block34Table);
        }
    }
}
