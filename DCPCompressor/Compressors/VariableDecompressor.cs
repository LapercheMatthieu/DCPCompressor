using DCPCompressor.AbstractDatas;
using DCPCompressor.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Compressors
{
    /// <summary>
    /// Classe de décompression pour des données de longueur variable
    /// </summary>
    public static class VariableDecompressor
    {
        public static void VariableLengthDecompressDelta(Memory<double> Target, ICompressedData CompData)
        {
            if (CompData.MainDatas == null || CompData.MainDatas.Length == 0)
                return;

            Type SourceType = CompData.CompressedDataInfos.DataType;
            int elementSize = CompressorUtilities.GetBytesForType(SourceType);
            int dataLength = CompData.MainDatas.Length / elementSize;

            int PartitionSize = 100_000;

            // Pour petits tableaux: traitement séquentiel
            if (dataLength < PartitionSize)
            {
                Span<double> targetSpan = Target.Span;
                for (int i = 0; i < dataLength; i++)
                {
                    targetSpan[i] = ByteConversionUtility.ReadValueFromBuffer(CompData.MainDatas, i * elementSize, SourceType);
                }
            }
            else
            {
                Parallel.ForEach(
                    Partitioner.Create(0, dataLength, PartitionSize),
                    range =>
                    {
                        // Obtenir une tranche Memory pour cette partition
                        Span<double> partitionSpan = Target.Slice(range.Item1, range.Item2 - range.Item1).Span;

                        // Traiter la partition
                        for (int i = 0; i < partitionSpan.Length; i++)
                        {
                            int globalIndex = range.Item1 + i;
                            partitionSpan[i] = ByteConversionUtility.ReadValueFromBuffer(CompData.MainDatas, globalIndex * elementSize, SourceType);
                        }
                    });
            }
        }

        public static void VariableLengthDecompressDeltaCount(Span<double> Target, ICompressedData CompData)
        {
            if (CompData.MainDatas == null || CompData.MainDatas.Length == 0 || CompData.CountDatas == null || CompData.CountDatas.Length == 0)
                return;

            Type SourceType = CompData.CompressedDataInfos.DataType;
            int elementSize = CompressorUtilities.GetBytesForType(SourceType);

            int ReadingIndex = 0;
            int TargetIndex = 0;
            int IndexJump = NumericRanges.GetByteSize(SourceType);

            // Parcourir les données compressées
            for (int i = 0; i < CompData.CountDatas.Length; i++)
            {
                // Lire la valeur delta actuelle
                double currentDelta = ByteConversionUtility.ReadValueFromBuffer(CompData.MainDatas, ReadingIndex, SourceType);

                // Lire le nombre de répétitions
                byte repeatCount = CompData.CountDatas[i];

                // Écrire la valeur delta 'repeatCount' fois dans le tableau cible
                for (int j = 0; j < repeatCount; j++)
                {
                    if (TargetIndex < Target.Length)
                    {
                        Target[TargetIndex] = currentDelta;
                        TargetIndex++;
                    }
                }

                // Passer à la prochaine valeur delta dans le buffer
                ReadingIndex += IndexJump;
            }
        }

        /// <summary>
        /// Décompresse un tableau d'octets à longueur variable en tableau de double
        /// Version avec partitionnement pour de grands tableaux
        /// </summary>
        /// <param name="compressedData">Données compressées</param>
        /// <param name="resultLength">Longueur attendue du tableau résultant</param>
        /// <returns>Tableau de valeurs double décompressées</returns>
        public static double[] DecompressByteWithPartitioning(byte[] compressedData, int resultLength)
        {
            if (compressedData == null || compressedData.Length == 0 || resultLength == 0)
                return Array.Empty<double>();

            double[] result = new double[resultLength];

            // Calculer les offsets de chaque élément compressé
            int[] offsets = new int[resultLength + 1];
            offsets[0] = 0;

            // Première passe - calculer les offsets (séquentielle car chaque offset dépend du précédent)
            for (int i = 0; i < resultLength; i++)
            {
                Type type = CompressorUtilities.ByteToType(compressedData[offsets[i]]);
                int elementSize = CompressorUtilities.GetBytesForType(type);
                offsets[i + 1] = offsets[i] + elementSize + 1;
            }

            // Deuxième passe - décompresser les valeurs en parallèle avec partitionnement
            int partitionSize = 100000;

            Parallel.ForEach(
                Partitioner.Create(0, resultLength, partitionSize),
                range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        int offset = offsets[i];
                        Type type = CompressorUtilities.ByteToType(compressedData[offset]);
                        result[i] = ByteConversionUtility.ReadValueFromBuffer(compressedData, offset + 1, type);
                    }
                });

            return result;
        }
    }
}
