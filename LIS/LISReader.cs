using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShellExtension.Formats.LIS
{
    public class LISReader : LIS
    {
        // Fields
        private LIS.Block128 _Block128 = new LIS.Block128();
        private List<LIS.Block34Datum> _Block34 = new List<LIS.Block34Datum>();
        private LIS.Block64 _Block64 = new LIS.Block64();
        private int _CodePage = 0x362;
        private Encoding _Encoding;
        private string _FileName;
        private int _FramesCount;
        private int _FrameSize;
        private int _RecordsCount;
        private bool _Scaned;
        private LIS.TIF _Tif;
        private BinaryReader br;
        private FileStream fs;

        // Methods
        public LISReader(string fileName)
        {
            this._FileName = fileName;
            this._Encoding = Encoding.GetEncoding(this._CodePage);
        }

        public void CloseFile()
        {
            if (this.br != null)
            {
                this.br.Close();
            }
            if (this.fs != null)
            {
                this.fs.Close();
            }
        }

        public string GetBlock34Param(string typeName, string paramName, int paramIndex)
        {
            foreach (LIS.Block34Datum datum in this.Block34Data)
            {
                if (datum.Name.Trim() == typeName.Trim()) // БЛОК 'CONS' и так далее
                {
                    foreach (ListViewItem item in datum.ListViewItems)
                    {
                        if (item.Text.Trim() == paramName) // Название параметра 
                        {
                            if (paramIndex > (item.SubItems.Count - 1)) // колонки
                            {
                                return null;
                            }
                            return item.SubItems[paramIndex].Text;
                        }
                    }
                }
            }
            return null;
        }

        public List<LISRecord> GetData(string mnemonic)
        {
            this.OpenFile();
            List<LISRecord> list = new List<LISRecord>();
            int count = 0;
            int index = 0;
            for (int i = 0; i < this._Block64.Datum01.Length; i++)
            {
                if (this._Block64.Datum01[i].Mnemonic == mnemonic)
                {
                    count = this._Block64.Datum01[i].Data_Size;
                    break;
                }
                index += this._Block64.Datum01[i].Data_Size;
            }
            bool flag = false;
            while (true)
            {
                this.ReadTIF();
                if (this._Tif.Next >= this.fs.Length)
                {
                    break;
                }
                this.ReadPhisicalHeader();
                LIS.LogicalHeader header = this.ReadLogicalHeader();
                if (header.Type == 0x40)
                {
                    flag = true;
                }
                if (flag && (header.Type == 0))
                {
                    int framesCountInRecord = this.GetFramesCountInRecord();
                    byte[] buffer = new byte[framesCountInRecord * count];
                    MemoryStream output = new MemoryStream(buffer);
                    BinaryWriter writer = new BinaryWriter(output);
                    double depth = 0.0;
                    if (this._Block64.DepthMode == 1)
                    {
                        object obj2 = this.ReadLISValue(this._Block64.DepthViewCode, LIS.GetLISValueSize(this._Block64.DepthViewCode));
                        if (obj2 is float)
                        {
                            depth = (float)obj2;
                        }
                        else if (obj2 is int)
                        {
                            depth = (int)obj2;
                        }
                    }
                    for (int j = 0; j < framesCountInRecord; j++)
                    {
                        byte[] buffer2 = this.br.ReadBytes(this._FrameSize);
                        writer.Write(buffer2, index, count);
                    }
                    list.Add(new LISRecord(depth, buffer, count, framesCountInRecord));
                }
                this.fs.Seek((long)this._Tif.Next, SeekOrigin.Begin);
            }
            this.CloseFile();
            return list;
        }

        public int GetDataCount(string mnemonic)
        {
            foreach (LIS.Block64Datum01 datum in this.Block64Data.Datum01)
            {
                if (datum.Mnemonic == mnemonic)
                {
                    return (datum.Data_Size / LIS.GetLISValueSize(datum.ViewCode));
                }
            }
            throw new LISException("Мнемоника " + mnemonic + " не найдена");
        }

        public int GetDataSize(string mnemonic)
        {
            foreach (LIS.Block64Datum01 datum in this.Block64Data.Datum01)
            {
                if (datum.Mnemonic == mnemonic)
                {
                    return datum.Data_Size;
                }
            }
            throw new LISException("Мнемоника " + mnemonic + " не найдена");
        }

        public LIS.Block64Datum01 GetDatum(string mnemonic)
        {
            foreach (LIS.Block64Datum01 datum in this.Block64Data.Datum01)
            {
                if (datum.Mnemonic == mnemonic)
                {
                    return datum;
                }
            }
            throw new LISException("Мнемоника " + mnemonic + " не найдена");
        }

        private int GetFramesCountInRecord()
        {
            int num = (int)(this._Tif.Next - ((int)this.fs.Position));
            if (this._Block64.DepthMode == 1)
            {
                num -= LIS.GetLISValueSize(this._Block64.DepthViewCode);
            }
            int num2 = num / this.FrameSize;
            if ((num2 * this.FrameSize) > num)
            {
                num2--;
            }
            return num2;
        }

        public int GetMnemonicPosition(string mnemonic)
        {
            int num = 0;
            for (int i = 0; i < this._Block64.Datum01.Length; i++)
            {
                if (this._Block64.Datum01[i].Mnemonic == mnemonic)
                {
                    ushort num1 = this._Block64.Datum01[i].Data_Size;
                    return num;
                }
                num += this._Block64.Datum01[i].Data_Size;
            }
            throw new LISException("Мнемоника не найдена:" + mnemonic);
        }

        private void OpenFile()
        {
            this.CloseFile();
            this.fs = new FileStream(this.FileName, FileMode.Open, FileAccess.Read);
            this.br = new BinaryReader(this.fs);
        }

        private void ReadBlock128()
        {
            this._Block128.FileName = this._Encoding.GetString(this.br.ReadBytes(12)).Trim();
            this._Block128.ServiceSubLevelName = this._Encoding.GetString(this.br.ReadBytes(6)).Trim();
            this._Block128.Version = this._Encoding.GetString(this.br.ReadBytes(8)).Trim();
            this._Block128.ReceiveDate = this._Encoding.GetString(this.br.ReadBytes(9)).Trim();
            this._Block128.MaxPhisRecLen = this._Encoding.GetString(this.br.ReadBytes(7)).Trim();
            this._Block128.FileType = this._Encoding.GetString(this.br.ReadBytes(4)).Trim();
            this._Block128.PrevFileName = this._Encoding.GetString(this.br.ReadBytes(10)).Trim();
        }

        private LIS.Block34Datum ReadBlock34()
        {
            LIS.Block34Datum datum = new LIS.Block34Datum();
            while (this.fs.Position < this._Tif.Next)
            {
                LIS.Block34Component item = this.ReadBlock34Component();
                if (item.Type == 0x49)
                {
                    datum.Mnemonic = item.Mnemonic;
                    datum.Name = item.Component.ToString();
                }
                else
                {
                    datum.Components.Add(item);
                }
            }
            return datum;
        }

        private LIS.Block34Component ReadBlock34Component()
        {
            LIS.Block34Component component;
            component.Type = this.br.ReadByte();
            component.ViewCode = this.br.ReadByte();
            component.Size = this.br.ReadByte();
            component.Category = this.br.ReadByte();
            component.Mnemonic = (string)this.ReadLISValue(0x41, 4);
            component.Uint = (string)this.ReadLISValue(0x41, 4);
            component.Component = this.ReadLISValue(component.ViewCode, component.Size);
            return component;
        }

        private void ReadBlock64()
        {
            LIS.Block64Header header;
            header.CType = 1;
            while (header.CType != 0)
            {
                header.CType = this.br.ReadByte();
                header.Size = this.br.ReadByte();
                header.ViewCode = this.br.ReadByte();
                object obj2 = this.ReadLISValue(header.ViewCode, header.Size);
                switch (header.CType)
                {
                    case 1:
                        {
                            this._Block64.RecordType = Convert.ToByte(obj2);
                            continue;
                        }
                    case 2:
                        {
                            this._Block64.DatumSpec = Convert.ToByte(obj2);
                            continue;
                        }
                    case 3:
                        {
                            this._Block64.DataFrameSize = Convert.ToUInt16(obj2);
                            continue;
                        }
                    case 4:
                        {
                            this._Block64.UpDown = Convert.ToByte(obj2);
                            continue;
                        }
                    case 5:
                        {
                            this._Block64.Units = Convert.ToByte(obj2);
                            continue;
                        }
                    case 6:
                        {
                            this._Block64.SourcePoint = Convert.ToSingle(obj2);
                            continue;
                        }
                    case 7:
                        {
                            this._Block64.SourcePointUnits = (string)obj2;
                            continue;
                        }
                    case 8:
                        {
                            if (!(obj2 is float))
                            {
                                break;
                            }
                            this._Block64.CadreStep = (float)obj2;
                            continue;
                        }
                    case 9:
                        {
                            this._Block64.CadreStepUnits = (string)obj2;
                            continue;
                        }
                    case 10:
                        {
                            continue;
                        }
                    case 11:
                        {
                            this._Block64.MaxCadre = Convert.ToUInt16(obj2);
                            continue;
                        }
                    case 12:
                        {
                            this._Block64.NullValue = Convert.ToSingle(obj2);
                            continue;
                        }
                    case 13:
                        {
                            this._Block64.DepthMode = Convert.ToByte(obj2);
                            continue;
                        }
                    case 14:
                        {
                            this._Block64.DepthUnits = (string)obj2;
                            continue;
                        }
                    case 15:
                        {
                            this._Block64.DepthViewCode = Convert.ToByte(obj2);
                            continue;
                        }
                    case 0x10:
                        {
                            this._Block64.DatumSubType = Convert.ToByte(obj2);
                            continue;
                        }
                    default:
                        {
                            continue;
                        }
                }
                int num = (int)obj2;
                this._Block64.CadreStep = num;
            }
            int num2 = (int)((this._Tif.Next - ((int)this.fs.Position)) / 40);
            this._Block64.Datum01 = new LIS.Block64Datum01[num2];
            this._FrameSize = 0;
            for (int i = 0; i < num2; i++)
            {
                this._Block64.Datum01[i] = this.ReadBlock64Datum01();
                this._FrameSize += this._Block64.Datum01[i].Data_Size;
            }
        }

        private LIS.Block64Datum01 ReadBlock64Datum01()
        {
            return new LIS.Block64Datum01 { Mnemonic = this._Encoding.GetString(this.br.ReadBytes(4)).Trim(), 
                WorkID = this._Encoding.GetString(this.br.ReadBytes(6)).Trim(), 
                WorkNumber = this._Encoding.GetString(this.br.ReadBytes(8)).Trim(), 
                Units = this._Encoding.GetString(this.br.ReadBytes(4)).Trim(), 
                APIWorkType = this.br.ReadByte(), APICrvType = this.br.ReadByte(), 
                APICrvClass = this.br.ReadByte(), APIType = this.br.ReadByte(), 
                FileNumber = LIS.Swap(this.br.ReadUInt16()), 
                Data_Size = LIS.Swap(this.br.ReadUInt16()), 
                Reserve1 = LIS.Swap(this.br.ReadUInt16()), 
                ProcessLevel = this.br.ReadByte(), 
                DataCount = this.br.ReadByte(), 
                ViewCode = this.br.ReadByte(), 
                Reserve2 = this.br.ReadByte(), 
                Reserve3 = this.br.ReadInt32() };
        }

        private object ReadLISValue(byte viewCode, byte size)
        {
            switch (viewCode)
            {
                case 0x31:
                    this.fs.Seek((long)size, SeekOrigin.Current);
                    break;

                case 50:
                    this.fs.Seek((long)size, SeekOrigin.Current);
                    break;

                case 0x38:
                    this.fs.Seek((long)size, SeekOrigin.Current);
                    break;

                case 0x41:
                    return this._Encoding.GetString(this.br.ReadBytes(size));

                case 0x42:
                    return this.br.ReadByte();

                case 0x44:
                    return LIS.LISfloat2IEEE(this.br.ReadUInt32());

                case 70:
                    this.fs.Seek((long)size, SeekOrigin.Current);
                    break;

                case 0x49:
                    return LIS.Swap(this.br.ReadInt32());

                case 0x4d:
                    this.fs.Seek((long)size, SeekOrigin.Current);
                    break;

                case 0x4f:
                    return LIS.Swap(this.br.ReadUInt16());

                case 0x80:
                    this.fs.Seek((long)size, SeekOrigin.Current);
                    break;

                default:
                    return this.br.ReadBytes(size);
            }
            return null;
        }

        private LIS.LogicalHeader ReadLogicalHeader()
        {
            LIS.LogicalHeader header;
            header.Type = this.br.ReadByte();
            header.Reserve = this.br.ReadByte();
            return header;
        }

        private LIS.PhisicalHeader ReadPhisicalHeader()
        {
            LIS.PhisicalHeader header;
            header.Length = LIS.Swap(this.br.ReadUInt16());
            header.Attributes = LIS.Swap(this.br.ReadUInt16());
            return header;
        }

        private void ReadTIF()
        {
            this._Tif.Type = this.br.ReadInt32();
            this._Tif.Prev = this.br.ReadInt32();
            this._Tif.Next = this.br.ReadUInt32();
            if ((this._Tif.Next <= this._Tif.Prev) || (this._Tif.Type < 0))
            {
                throw new LISException("Ошибка чтения заголовка LIS");
            }
        }

        public void Scan()
        {
            bool flag = false;
            this._Scaned = false;
            this._FramesCount = 0;
            this._RecordsCount = 0;
            this.OpenFile();
            while (true)
            {
                if (this.fs.Position == this.fs.Length)
                {
                    break;
                }
                this.ReadTIF();
                if (this._Tif.Next >= this.fs.Length)
                {
                    break;
                }
                this.ReadPhisicalHeader();
                switch (this.ReadLogicalHeader().Type)
                {
                    case 0x40:
                        this.ReadBlock64();
                        flag = true;
                        break;

                    case 0x80:
                        this.ReadBlock128();
                        break;

                    case 0:
                        if (flag)
                        {
                            this._FramesCount += this.GetFramesCountInRecord();
                            this._RecordsCount++;
                        }
                        break;

                    case 0x22:
                        this._Block34.Add(this.ReadBlock34());
                        break;
                }
                this.fs.Seek((long)this._Tif.Next, SeekOrigin.Begin);
            }
            this.CloseFile();
            if (!flag)
            {
                throw new LISException("Не найден блок 64");
            }
            this._Scaned = true;
        }

        // Properties
        public LIS.Block128 Block128Data
        {
            get
            {
                return this._Block128;
            }
        }

        public List<LIS.Block34Datum> Block34Data
        {
            get
            {
                return this._Block34;
            }
        }

        public LIS.Block64 Block64Data
        {
            get
            {
                return this._Block64;
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

        public string FileName
        {
            get
            {
                return this._FileName;
            }
        }

        public int FramesCount
        {
            get
            {
                return this._FramesCount;
            }
        }

        public int FrameSize
        {
            get
            {
                return this._FrameSize;
            }
        }

        public int RecordsCount
        {
            get
            {
                return this._RecordsCount;
            }
        }

        public bool Scaned
        {
            get
            {
                return this._Scaned;
            }
        }
    }
}
