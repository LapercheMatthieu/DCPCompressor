using DCPCompressor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.AbstractDataInformations
{
    public interface IDataRangeInfo
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public int NbValues { get; set; }
        public int NbNulls { get; set; }
        public int Length { get; set; }
        public bool HasNull { get; set; }
    }
}
