using DCPCompressor.AbstractDataInformations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Models
{
    public class CompressedDataInfo : ICompressedDataInfos
    {
        public Type DataType { get; set; }
        public sbyte ComaFactor { get; set; }
        public sbyte ScaleFactor { get; set; }
        public object NullValue { get; set; }
        public CompressionEnums CompressionType { get; set; }
        public CompressionTypeEnums CompressionTypeEnum { get; set; }
        public double StartingValue { get; set; }
        public int StartingValueIndex { get; set; }
        public int NumberOfValues { get; set; }
        public bool StartingValueBegin { get; set; }
        public int Precision { get; set; }
    }
}
