using CommunityToolkit.Mvvm.DependencyInjection;
using DCPCompressor.AbstractDataInformations;
using DCPCompressor.AbstractDatas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Models
{
    public class CompressedData : ICompressedData
    {
        public string Name { get; set; }
        public IDataRangeInfo DataRangeInfos { get; set; }
        public ICompressedDataInfos CompressedDataInfos { get; set; }
        public byte[] MainDatas { get; set; }
        public byte[]? CountDatas { get; set; }

        public CompressedData()
        {
            DataRangeInfos = new DataRangeInfo();
            CompressedDataInfos = new CompressedDataInfo();
        }

    }
}
