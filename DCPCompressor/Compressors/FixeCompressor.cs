using DCPCompressor.AbstractDatas;
using DCPCompressor.Models;
using DCPCompressor.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace DCPCompressor.Compressors
{
    /// <summary>
    /// Classe de compression utilisant un type fixe pour toutes les valeurs
    /// </summary>
    public static class FixeCompressor
    {
        
        /// <summary>
        /// Compresse des valeurs double en un type fixe avec partitionnement
        /// (optimisé pour de grands tableaux)
        /// </summary>
        /// <param name="values">Tableau de valeurs à compresser</param>
        /// <param name="targetType">Type cible pour la compression</param>
        /// <returns>Tableau d'octets contenant les données compressées</returns>
        public static void FixedLengthCompressDelta(Memory<double> DeltaValues, CompressedData CompData)
        {
            if (DeltaValues.Length == 0)
                return;
            Type FinalType = CompData.CompressedDataInfos.DataType;
            int elementSize = CompressorUtilities.GetBytesForType(FinalType);

            byte[] buffer = new byte[(DeltaValues.Length) * elementSize];


            int PartitionSize = 100_000;

            // Pour petits tableaux: traitement séquentiel
            if (DeltaValues.Length < PartitionSize)
            {
                var DeltaValuesSpan = DeltaValues.Span;
                for (int i = 0; i < DeltaValues.Length; i++)
                {
                    ByteConversionUtility.WriteValueToBuffer(DeltaValuesSpan[i], FinalType, buffer, i * elementSize);
                }
            }
            else
            {
                Parallel.ForEach(
                    Partitioner.Create(0, DeltaValues.Length, PartitionSize),
                    range =>
                    {
                        // Obtenir une tranche Memory pour cette partition
                        Span<double> partitionSpan = DeltaValues.Slice(range.Item1, range.Item2 - range.Item1).Span;

                        // Traiter la partition
                        for (int i = 0; i < partitionSpan.Length; i++)
                        {
                            int globalIndex = range.Item1 + i;
                            ByteConversionUtility.WriteValueToBuffer(partitionSpan[i], FinalType, buffer, globalIndex * elementSize);
                        }
                    });
            }
            CompData.MainDatas = buffer;
        }

        public static void FixedLengthCompressDeltaCount(Span<double> DeltaValues, CompressedData CompData)
        {
            if (DeltaValues.Length == 0)
                return;
            Type FinalType = CompData.CompressedDataInfos.DataType;
            int elementSize = CompressorUtilities.GetBytesForType(FinalType);

            //Je vais passer par une liste de byte soyons fou
            List<byte> DeltaBuffer = new List<byte>(DeltaValues.Length*elementSize);
            List<byte> CountBuffer = new List<byte>(DeltaValues.Length*elementSize);

            int PartitionSize = 100_000;

            // Pour petits tableaux: traitement séquentiel
            // Prendre la première valeur comme référence
            double currentDelta = DeltaValues[0];
            byte repeatCount = 1;
            int WritingIndex = 0;
            int IndexJump = NumericRanges.GetByteSize(FinalType);
            // Parcourir les valeurs restantes
            for (int i = 1; i < DeltaValues.Length; i++)
            {
                // Si le delta est le même et qu'on n'a pas atteint la limite du compteur
                if (Math.Abs(DeltaValues[i] - currentDelta) < 1e-10 && repeatCount < 255)
                {
                    repeatCount++;
                }
                else
                {
                    //Ici on doit écrire dans le buffer
                    // Stocker le delta actuel et son compteur
                    ByteConversionUtility.WriteValueToBuffer(currentDelta, FinalType, DeltaBuffer, WritingIndex);
                    CountBuffer.Add(repeatCount);
                    WritingIndex += IndexJump;

                    // Commencer un nouveau groupe
                    currentDelta = DeltaValues[i];
                    repeatCount = 1;
                }
            }

            // Ajouter le dernier groupe
            ByteConversionUtility.WriteValueToBuffer(currentDelta, FinalType, DeltaBuffer, WritingIndex);
            CountBuffer.Add(repeatCount);
            CountBuffer.TrimExcess();
            DeltaBuffer.TrimExcess();

            CompData.MainDatas = DeltaBuffer.ToArray();
            CompData.CountDatas = CountBuffer.ToArray();

        }
    }
}
