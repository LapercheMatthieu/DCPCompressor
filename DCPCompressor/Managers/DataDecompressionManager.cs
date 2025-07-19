using DCPCompressor.AbstractDataInformations;
using DCPCompressor.AbstractDatas;
using DCPCompressor.Compressors;
using DCPCompressor.Debugger;
using DCPCompressor.Models;
using DCPCompressor.Tools;
using OptimizedProjectHandler.Datas.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Managers
{
    /// <summary>
    /// Gestionnaire pour la décompression des données
    /// Version optimisée avec Span pour réduire les allocations mémoire
    /// </summary>
    public static class DataDecompressionManager
    {
        /// <summary>
        /// Décompresse les données à partir d'un objet ICompressedData
        /// </summary>
        /// <param name="compressedData">Les données compressées à décompresser</param>
        /// <returns>Un tableau de doubles contenant les données décompressées</returns>
        public static double[] DecompressDatas(ICompressedData compressedData)
        {
            //La decompression est beaucoup plus simple
            //Le plus compliqué est de refabriquer depuis le deltacount
            if (compressedData == null)
                return Array.Empty<double>();

            double[] MainDatas = new double[compressedData.DataRangeInfos.Length];
            Memory<double> DataMemory = MainDatas;
            // Reconstruction en fonction du type de compression
            if (compressedData.CompressedDataInfos.CompressionType == CompressionEnums.DeltaCount)
            {
                if (compressedData.CompressedDataInfos.CompressionTypeEnum == CompressionTypeEnums.Fixe)
                {
                    FixeDecompressor.FixedLengthDecompressDeltaCount(DataMemory.Span, compressedData);
                }
                else
                {
                    VariableDecompressor.VariableLengthDecompressDeltaCount(DataMemory.Span, compressedData);
                }
            }
            else
            {
                if (compressedData.CompressedDataInfos.CompressionTypeEnum == CompressionTypeEnums.Fixe)
                {
                    FixeDecompressor.FixedLengthDecompressDelta(DataMemory, compressedData);
                }
                else
                {
                    VariableDecompressor.VariableLengthDecompressDelta(DataMemory, compressedData);
                }
            }
            //On injecte les valeurs null
            NullValueHandler.ReplaceNullValues(DataMemory, compressedData.CompressedDataInfos.NullValue);

            // Déscaler le tableau en place
            ArrayScaler.UnscaleArray(DataMemory.Span, compressedData.CompressedDataInfos.ScaleFactor, compressedData.CompressedDataInfos.ComaFactor);

            // On passe en mode normal
            SeriesConverter.ReconstructFromDeltasInPlace(DataMemory, compressedData);

            return MainDatas;
        }

    }
}
