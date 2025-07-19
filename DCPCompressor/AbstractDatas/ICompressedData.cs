using DCPCompressor.AbstractDataInformations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.AbstractDatas
{
    public interface ICompressedData
    {
        public string Name { get; set; }
        public IDataRangeInfo DataRangeInfos { get; set; }
        public ICompressedDataInfos CompressedDataInfos { get; set; }
        public byte[] MainDatas { get; set; }
        public byte[]? CountDatas { get; set; }

    }
}
