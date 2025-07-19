using DCPCompressor.AbstractDataInformations;
using DCPCompressor.AbstractDatas;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Tools
{
    public static class SeriesConverter
    {
        private const int PARALLEL_THRESHOLD = 100_000;

        /// <summary>
        /// Reconstruit le signal original à partir des deltas directement dans le tableau fourni
        /// </summary>
        /// <param name="deltas">Les deltas qui seront convertis en valeurs originales</param>
        /// <param name="datainfos">Les informations sur les données compressées, incluant la valeur initiale</param>
        public static void ReconstructFromDeltasInPlace(Memory<double> deltas, ICompressedData datainfos)
        {
            // Vérification des entrées
            if (deltas.IsEmpty || deltas.Length <= 1)
            {
                return;
            }

            if (true)//(deltas.Length < PARALLEL_THRESHOLD)
            {
                // Algorithme optimisé pour les tableaux moyens
                ReconstructFromDeltasMediumInPlace(deltas.Span, datainfos);
            }
            else
            {
                // Algorithme parallélisé pour les grands tableaux
              //  ReconstructFromDeltasLargeInPlace(deltas, datainfos);
            }
        }

        /// <summary>
        /// Algorithme optimisé pour les tableaux de taille moyenne avec modification in-place
        /// </summary>
        private static void ReconstructFromDeltasMediumInPlace(Span<double> deltas, ICompressedData datainfos)
        {
            //Si full Nan
            if (datainfos.DataRangeInfos.NbValues == 0)
            {
                return;
            }
            // Obtenir la valeur initiale et son index
            double currentValue = datainfos.CompressedDataInfos.StartingValue;
            int startIndex = datainfos.CompressedDataInfos.StartingValueIndex;

            // La première valeur non-NaN est la valeur initiale
            deltas[startIndex] = currentValue;

            // Reconstruire le signal original
            for (int i = startIndex + 1; i < deltas.Length; i++)
            {
                if (double.IsNaN(deltas[i]))
                {
                    continue;
                }

                currentValue += deltas[i];
                deltas[i] = Math.Round(currentValue, datainfos.CompressedDataInfos.Precision);
            }
        }

        /// <summary>
        /// Algorithme parallélisé pour les grands tableaux avec modification minimale
        /// </summary>
        private static void ReconstructFromDeltasLargeInPlace(Memory<double> deltas, ICompressedDataInfos datainfos)
        {
            // Structures pour stocker les valeurs de début et de fin de chaque partition
            ConcurrentDictionary<int, double> partitionValues = new ConcurrentDictionary<int, double>();

            // Obtenir la valeur initiale et son index
            double initialValue = datainfos.StartingValue;
            int startIndex = datainfos.StartingValueIndex;

            // Mettre à jour la première valeur
            var deltasSpan = deltas.Span;
            deltasSpan[startIndex] = initialValue;
            partitionValues.TryAdd(startIndex, initialValue);

            // Identifier les limites des partitions
            List<(int Start, int End)> partitions = new List<(int, int)>();
            for (int i = 0; i < deltas.Length; i += CompressorSettings.Default.PARALLEL_THRESHOLD)
            {
                int end = Math.Min(i + CompressorSettings.Default.PARALLEL_THRESHOLD, deltas.Length);
                partitions.Add((i, end));
            }

            // Première phase: Reconstruire chaque partition individuellement
            Parallel.ForEach(
                partitions,
                partition =>
                {
                    int partitionStart = partition.Start;
                    int partitionEnd = partition.End;
                    Span<double> partitionSpan = deltas.Slice(partitionStart, partitionEnd - partitionStart).Span;

                    // Recherche la première valeur non-NaN du segment
                    double firstValue = double.NaN;
                    int firstIndex = 0;

                    while (firstIndex < partitionSpan.Length && double.IsNaN(partitionSpan[firstIndex]))
                    {
                        firstIndex++;
                    }

                    // Si on a trouvé une valeur non-NaN dans cette partition
                    if (firstIndex < partitionSpan.Length)
                    {
                        // Si c'est la première partition avec la valeur initiale
                        if (partitionStart + firstIndex == startIndex)
                        {
                            firstValue = initialValue;
                            partitionSpan[firstIndex] = firstValue;
                        }
                        else
                        {
                            // Pour les autres partitions, on garde le delta pour l'instant
                            firstValue = partitionSpan[firstIndex];
                        }

                        // Reconstruire la partition à partir de la première valeur
                        double currentValue = firstValue;
                        for (int i = firstIndex + 1; i < partitionSpan.Length; i++)
                        {
                            if (double.IsNaN(partitionSpan[i]))
                            {
                                continue;
                            }

                            currentValue += partitionSpan[i];
                            partitionSpan[i] = currentValue;
                        }

                        // Stocker la dernière valeur non-NaN de la partition
                        int lastIndex = partitionSpan.Length - 1;
                        while (lastIndex >= 0 && double.IsNaN(partitionSpan[lastIndex]))
                        {
                            lastIndex--;
                        }

                        if (lastIndex >= 0)
                        {
                            partitionValues.TryAdd(partitionStart + lastIndex, partitionSpan[lastIndex]);
                        }
                    }
                });

            // Deuxième phase: Propager les valeurs cumulées à travers les partitions
            var orderedPartitionValues = partitionValues.OrderBy(kv => kv.Key).ToList();

            // Si on a plus d'une partition avec des valeurs
            if (orderedPartitionValues.Count > 1)
            {
                // Commencer à partir de la deuxième partition
                double cumulativeValue = orderedPartitionValues[0].Value;

                for (int i = 1; i < partitions.Count; i++)
                {
                    int partitionStart = partitions[i].Start;
                    int partitionEnd = partitions[i].End;
                    Span<double> partitionSpan = deltas.Slice(partitionStart, partitionEnd - partitionStart).Span;

                    // Trouver la première valeur non-NaN de cette partition
                    int firstNonNanIndex = 0;
                    while (firstNonNanIndex < partitionSpan.Length && double.IsNaN(partitionSpan[firstNonNanIndex]))
                    {
                        firstNonNanIndex++;
                    }

                    // Si on a trouvé une valeur non-NaN
                    if (firstNonNanIndex < partitionSpan.Length)
                    {
                        // Calculer la nouvelle valeur cumulative
                        double baseValue = cumulativeValue;
                        double currentDelta = partitionSpan[firstNonNanIndex];
                        double newStartValue = baseValue + currentDelta;

                        // Mettre à jour la première valeur de la partition
                        partitionSpan[firstNonNanIndex] = newStartValue;

                        // Recalculer toutes les valeurs de cette partition
                        double runningValue = newStartValue;
                        for (int j = firstNonNanIndex + 1; j < partitionSpan.Length; j++)
                        {
                            if (double.IsNaN(partitionSpan[j]))
                            {
                                continue;
                            }

                            runningValue += partitionSpan[j];
                            partitionSpan[j] = runningValue;
                        }

                        // Mettre à jour la valeur cumulative pour la prochaine partition
                        int lastValidIndex = partitionSpan.Length - 1;
                        while (lastValidIndex >= 0 && double.IsNaN(partitionSpan[lastValidIndex]))
                        {
                            lastValidIndex--;
                        }

                        if (lastValidIndex >= 0)
                        {
                            cumulativeValue = partitionSpan[lastValidIndex];
                        }
                    }
                }
            }
        }
    }
}
