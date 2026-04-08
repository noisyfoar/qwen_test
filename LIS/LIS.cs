using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShellExtension.Formats.LIS
{
    public class LISException : Exception
    {
        // Methods
        public LISException(string message) : base(message)
        {
        }
    }

    public abstract class LIS
    {
        // Fields
        public const int Block64Datum01Size = 40;

        // Methods
        protected LIS()
        {
        }

        public static float[] ConvertToFloat32Data(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(input);
            float[] numArray = new float[data.Length / 4];
            for (int i = 0; i < numArray.Length; i++)
            {
                numArray[i] = LISfloat2IEEE(reader.ReadUInt32());
            }
            return numArray;
        }

        public static int[] ConvertToINT32Data(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(input);
            int[] numArray = new int[data.Length / 4];
            for (int i = 0; i < numArray.Length; i++)
            {
                numArray[i] = Swap(reader.ReadInt32());
            }
            return numArray;
        }

        public static ushort[] ConvertToUINT16Data(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(input);
            ushort[] numArray = new ushort[data.Length / 2];
            for (int i = 0; i < numArray.Length; i++)
            {
                numArray[i] = Swap(reader.ReadUInt16());
            }
            return numArray;
        }

        public static string GetFormatName(byte viewCode)
        {
            switch (viewCode)
            {
                case 0x31:
                    return "16-битное число с плав. точкой";

                case 50:
                    return "32-битное число с плав. точкой низкого расширения";

                case 0x38:
                    return "8-битное целое";

                case 0x41:
                    return "Алфавитно-цифровое";

                case 0x42:
                    return "Байтовый формат";

                case 0x43:
                    return "Маска";

                case 0x44:
                    return "32-битовое число с плав. точкой";

                case 70:
                    return "32-битовое число с фикс. точкой";

                case 0x49:
                    return "32-битное  целое";

                case 0x4f:
                    return "16-битное целое";
            }
            return "";
        }

        public static byte GetLISValueSize(int viewCode)
        {
            switch (viewCode)
            {
                case 0x31:
                    return 2;

                case 50:
                    return 4;

                case 0x38:
                    return 1;

                case 0x41:
                    return 0;

                case 0x42:
                    return 1;

                case 0x44:
                    return 4;

                case 70:
                    return 4;

                case 0x49:
                    return 4;

                case 0x4d:
                    return 0;

                case 0x4f:
                    return 2;

                case 0x80:
                    return 0;
            }
            return 0;
        }

        public static unsafe uint IEEEfloat2LIS(float value)
        {
            uint num;
            byte* numPtr = (byte*)&num;
            uint* numPtr2 = &num;
            float* numPtr3 = (float*)numPtr2;
            numPtr3[0] = value;
            uint num2 = num & 0x80000000;
            uint val = num & 0x7fffff;
            num = num >> 0x17;
            uint num3 = num & 0xff;
            num = num >> 8;
            if (num3 != 0)
            {
                val = val >> 1;
                val |= 0x400000;
            }
            int num5 = (((int)num3) - 0x7f) + 1;
            if (num2 > 0)
            {
                val = NotUint32(val) + 1;
                val &= 0x7fffff;
                num3 = (uint)(0x7f - num5);
            }
            else
            {
                num3 = (uint)(num5 - 0x80);
            }
            num3 &= 0xff;
            num = 0;
            num |= val;
            num |= num3 << 0x17;
            num |= num2;
            byte num6 = numPtr[0];
            numPtr[0] = numPtr[3];
            numPtr[3] = num6;
            num6 = numPtr[1];
            numPtr[1] = numPtr[2];
            numPtr[2] = num6;
            return num;
        }

        public static unsafe float LISfloat2IEEE(uint value)
        {
            float num;
            byte* numPtr = (byte*)&value;
            byte num5 = numPtr[0];
            numPtr[0] = numPtr[3];
            numPtr[3] = num5;
            num5 = numPtr[1];
            numPtr[1] = numPtr[2];
            numPtr[2] = num5;
            uint val = value & 0x7fffff;
            uint num2 = (uint)((value & -2147483648) >> 0x1f);
            value = value << 1;
            uint num3 = (uint)((value & -16777216) >> 0x18);
            if (num2 == 1)
            {
                val = NotUint32(val) + 1;
                val &= 0x7fffff;
                num = num3;
                num = (127f - num) - 23f;
                return -((float)(val * Math.Pow(2.0, (double)num)));
            }
            num = num3;
            num = (num - 128f) - 23f;
            return (float)(val * Math.Pow(2.0, (double)num));
        }

        public static uint NotUint32(uint val)
        {
            uint num2 = 0;
            for (int i = 0; i < 0x20; i++)
            {
                if (((val >> i) & 1) == 0)
                {
                    num2 += ((uint)1) << i;
                }
            }
            return num2;
        }

        public static int Swap(int value)
        {
            int num = Swap((ushort)(value >> 0x10));
            return ((Swap((ushort)((value << 0x10) >> 0x10)) << 0x10) + num);
        }

        public static ushort Swap(ushort value)
        {
            int num = value >> 8;
            int num2 = value << 8;
            return (ushort)(num2 + num);
        }

        // Nested Types
        public class Block128
        {
            // Fields
            public string FileName;
            public string FileType;
            public string MaxPhisRecLen;
            public string PrevFileName;
            public string ReceiveDate;
            public string ServiceSubLevelName;
            public string Version;

            // Properties
            public int Size
            {
                get
                {
                    return 0x38;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Block34Component
        {
            public byte Type;
            public byte ViewCode;
            public byte Size;
            public byte Category;
            public string Mnemonic;
            public string Uint;
            public object Component;
            public Block34Component(byte type, byte viewCode, byte size, byte category, string mnemonic, string comp_uint, object component)
            {
                this.Type = type;
                this.ViewCode = viewCode;
                this.Size = size;
                this.Category = category;
                this.Mnemonic = mnemonic;
                this.Uint = comp_uint;
                this.Component = component;
            }

            public Block34Component(byte type, byte viewCode, byte size, byte category, string mnemonic, string Name)
            {
                this.Type = type;
                this.ViewCode = viewCode;
                this.Size = size;
                this.Category = category;
                this.Mnemonic = mnemonic;
                this.Component = Name;
                this.Uint = "";
            }

            public int ComponentSize
            {
                get
                {
                    return (12 + this.Size);
                }
            }
        }

        public class Block34Datum
        {
            // Fields
            private List<LIS.Block34Component> _Components = new List<LIS.Block34Component>();
            private string _Mnemonic;
            private string _Name;

            // Properties
            public List<string> ColumnHeaders
            {
                get
                {
                    List<string> list = new List<string>();
                    for (int i = 0; i < this.Components.Count; i++)
                    {
                        LIS.Block34Component component = this.Components[i];
                        bool flag = false;
                        foreach (string str in list)
                        {
                            if (str == component.Mnemonic)
                            {
                                flag = true;
                            }
                        }
                        if (!flag)
                        {
                            list.Add(component.Mnemonic);
                        }
                    }
                    return list;
                }
            }

            public List<LIS.Block34Component> Components
            {
                get
                {
                    return this._Components;
                }
                set
                {
                    this._Components = value;
                }
            }

            public int ComponentSize
            {
                get
                {
                    int num = 0;
                    foreach (LIS.Block34Component component in this.Components)
                    {
                        num += component.ComponentSize;
                    }
                    return ((num + 12) + this.Name.Length);
                }
            }

            public int ItemsCount
            {
                get
                {
                    if (this.ColumnHeaders.Count > 0)
                    {
                        return (this._Components.Count / this.ColumnHeaders.Count);
                    }
                    return 0;
                }
            }

            public ListViewItem[] ListViewItems
            {
                get
                {
                    List<string> columnHeaders = this.ColumnHeaders;
                    ListViewItem[] itemArray = new ListViewItem[this.ItemsCount];
                    if (this.ItemsCount != 0)
                    {
                        ListViewItem item = new ListViewItem();
                        int index = 0;
                        for (int i = 0; i < this.Components.Count; i++)
                        {
                            LIS.Block34Component component = this.Components[i];
                            if (component.Mnemonic == columnHeaders[0])
                            {
                                if (i != 0)
                                {
                                    itemArray[index] = item;
                                    index++;
                                }
                                item = new ListViewItem(component.Component.ToString());
                            }
                            else
                            {
                                item.SubItems.Add(component.Component.ToString());
                            }
                        }
                        itemArray[index] = item;
                    }
                    return itemArray;
                }
            }

            public string Mnemonic
            {
                get
                {
                    return this._Mnemonic;
                }
                set
                {
                    this._Mnemonic = value;
                }
            }

            public string Name
            {
                get
                {
                    return this._Name;
                }
                set
                {
                    this._Name = value;
                }
            }
        }

        public class Block34Table
        {
            // Fields
            private List<string> _Columns = new List<string>();
            private string _Mnemonic = "TYPE";
            private string _Name;
            private List<ArrayList> Rows = new List<ArrayList>();

            // Methods
            public Block34Table(string tableName)
            {
                this._Name = tableName;
            }

            public void Add5ColumnRow(object o1, object o2, object o3, object o4, object o5)
            {
                ArrayList item = new ArrayList();
                item.Add(o1); // название
                item.Add(o2); //???
                item.Add(o3); //???
                item.Add(o4); //???
                item.Add(o5); // значение
                this.Rows.Add(item);
            }

            public void AddRow(params object[] rows)
            {
                ArrayList item = new ArrayList();
                foreach (object obj2 in rows)
                {
                    item.Add(obj2);
                }
                this.Rows.Add(item);
            }

            public void AddRow(ArrayList Row)
            {
                if (Row.Count != this.Columns.Count)
                {
                    throw new LISException("Число элементов не соответствуйт числу колонок.");
                }
                this.Rows.Add(Row);
            }

            public void Make5Columns(string col1, string col2, string col3, string col4, string col5)
            {
                this.Columns = new List<string>();
                this.Columns.Add(col1);
                this.Columns.Add(col2);
                this.Columns.Add(col3);
                this.Columns.Add(col4);
                this.Columns.Add(col5);
            }

            public LIS.Block34Datum MakeBlock34Datum()
            {
                LIS.Block34Datum datum = new LIS.Block34Datum
                {
                    Name = this.Name,
                    Mnemonic = this.Mnemonic
                };
                foreach (ArrayList list in this.Rows)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        byte type = 0x45;
                        if (i == 0)
                        {
                            type = 0;
                        }
                        byte size = 0;
                        byte viewCode = 0;
                        if (list[i] is string)
                        {
                            string str = list[i] as string;
                            if (str.Length > 0xff)
                            {
                                list[i] = str = str.Substring(0, 0xfe);
                            }
                            size = (byte)str.Length;
                            viewCode = 0x41;
                        }
                        else if (list[i] is int)
                        {
                            size = 4;
                            viewCode = 0x49;
                        }
                        else
                        {
                            if (!(list[i] is float))
                            {
                                throw new LISException("Тип " + list[i].GetType().Name + " для блока 34  не поддерживается.");
                            }
                            size = 4;
                            viewCode = 0x44;
                        }
                        LIS.Block34Component item = new LIS.Block34Component(type, viewCode, size, 0, this.Columns[i], "", list[i]);
                        datum.Components.Add(item);
                    }
                }
                return datum;
            }

            public void MakeColumns(params string[] cols)
            {
                foreach (string str in cols)
                {
                    this.Columns.Add(str);
                }
            }

            // Properties
            public List<string> Columns
            {
                get
                {
                    return this._Columns;
                }
                set
                {
                    this._Columns = value;
                }
            }

            public string Mnemonic
            {
                get
                {
                    return this._Mnemonic;
                }
                set
                {
                    this._Mnemonic = value;
                }
            }

            public string Name
            {
                get
                {
                    return this._Name;
                }
                set
                {
                    this._Name = value;
                }
            }
        }

        public class Block64
        {
            // Fields
            public float CadreStep;
            public string CadreStepUnits;
            public ushort DataFrameSize;
            public LIS.Block64Datum01[] Datum01;
            public byte DatumSpec;
            public byte DatumSubType;
            public byte DepthMode;
            public string DepthUnits;
            public byte DepthViewCode;
            public ushort MaxCadre;
            public float NullValue;
            public byte RecordType;
            public float SourcePoint;
            public string SourcePointUnits;
            public byte Units;
            public byte UpDown;

            // Properties
            public int DataSize
            {
                get
                {
                    int num = 0;
                    foreach (LIS.Block64Datum01 datum in this.Datum01)
                    {
                        num += datum.Data_Size;
                    }
                    return num;
                }
            }

            public int Size
            {
                get
                {
                    return ((((((((((0x3b + this.SourcePointUnits.Length) + 4) + this.CadreStepUnits.Length) + 2) + 4) + 1) + this.DepthUnits.Length) + 1) + 1) + (this.Datum01.Length * 40));
                }
            }
        }

        public class Block64Datum01
        {
            // Fields
            public byte APICrvClass;
            public byte APICrvType;
            public byte APIType;
            public byte APIWorkType;
            public ushort Data_Size;
            public byte DataCount;
            public ushort FileNumber;
            public string Mnemonic;
            public byte ProcessLevel;
            public ushort Reserve1;
            public byte Reserve2;
            public int Reserve3;
            public string Units;
            
            /// <summary>
            /// Код кодировки. Смотреть 101 страницу спецификации.
            /// 73 - Int32
            /// 79 - Int16
            /// 68 - Float32
            /// и др.
            /// </summary>
            public byte ViewCode;
            
            public string WorkID;
            public string WorkNumber;

            // Properties
            public static int size
            {
                get
                {
                    return 40;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Block64Header
        {
            public byte CType;
            public byte Size;
            public byte ViewCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LogicalHeader
        {
            public byte Type;
            public byte Reserve;
            public static int Size
            {
                get
                {
                    return 2;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PhisicalHeader
        {
            public ushort Length;
            public ushort Attributes;
            public static int Size
            {
                get
                {
                    return 4;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TIF
        {
            public int Type;
            public int Prev;
            public uint Next;
            public static int Size
            {
                get
                {
                    return 12;
                }
            }
        }
    }
}
