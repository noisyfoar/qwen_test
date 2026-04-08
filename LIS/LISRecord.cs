using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellExtension.Formats.LIS
{
    public class LISRecord
    {
        // Fields
        private byte[] _Data;
        private double _Depth;
        public int FramesCount;
        public int FrameSize;

        // Methods
        public LISRecord(double depth, byte[] data, int frameSize, int framesCount)
        {
            if (((data.Length % frameSize) != 0) || (data.Length != (frameSize * framesCount)))
            {
                throw new LISException("Неверный размер данных");
            }
            this.FrameSize = frameSize;
            this.FramesCount = framesCount;
            this._Depth = depth;
            this._Data = data;
        }

        // Properties
        public byte[] Data
        {
            get
            {
                return this._Data;
            }
        }

        public double Depth
        {
            get
            {
                return this._Depth;
            }
        }
    }
}
