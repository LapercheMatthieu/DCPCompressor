using DCPCompressor.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizedProjectHandler.Datas.Tools
{
    /// <summary>
    /// Service d'analyse des tableaux pour déterminer leurs caractéristiques
    /// Optimisé avec ReadOnlySpan pour réduire les allocations mémoire
    /// </summary>
    public static class ArrayAnalyzer
    {
        private const int PARALLEL_THRESHOLD = 100_000;

        /// <summary>
        /// Analyse un tableau de valeurs doubles pour déterminer ses caractéristiques
        /// </summary>
        public static ArrayMetrics Analyze(ReadOnlySpan<double> values)
        {
            if (values.IsEmpty)
                return ArrayMetrics.Empty;

            // Pour les petits tableaux, analyse directement le Span
            if (values.Length < PARALLEL_THRESHOLD)
            {
                return AnalyzeSequential(values);
            }

            // Pour les grands tableaux qui nécessitent parallélisme, 
            // nous devons utiliser un tableau (Span ne peut pas être capturé dans les lambdas)
            double[] array = values.ToArray();
            return AnalyzeParallel(array);
        }

        /// <summary>
        /// Surcharge pour la compatibilité avec les tableaux existants
        /// </summary>
        public static ArrayMetrics Analyze(double[] values)
        {
            return values == null || values.Length == 0
                ? ArrayMetrics.Empty
                : Analyze(values.AsSpan());
        }

        /// <summary>
        /// Analyse un tableau de valeurs en parallèle (pour les grands tableaux)
        /// </summary>
        private static ArrayMetrics AnalyzeParallel(double[] values)
        {
            // Optimisation du partitionnement pour maximiser l'utilisation du cache
            int processorCount = Environment.ProcessorCount;
            int optimalPartitionSize = Math.Max(4096, values.Length / processorCount);

            var partitioner = Partitioner.Create(0, values.Length, optimalPartitionSize);
            var results = new ConcurrentBag<ArrayMetrics>();

            Parallel.ForEach(partitioner, range =>
            {
                // Créer un Span sur le segment du tableau sans copie
                ReadOnlySpan<double> span = values.AsSpan(range.Item1, range.Item2 - range.Item1);
                results.Add(AnalyzeSequential(span));
            });

            // Combiner les résultats
            return ArrayMetrics.Combine(results.ToArray());
        }

        /// <summary>
        /// Analyse un segment de tableau de manière séquentielle
        /// Version optimisée avec ReadOnlySpan
        /// </summary>
        private static ArrayMetrics AnalyzeSequential(ReadOnlySpan<double> values)
        {
            if (values.IsEmpty)
                return ArrayMetrics.Empty;

            bool isInteger = true;
            bool hasNegative = false;
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            bool needsDoublePresicion = false;

            // Analyse en une seule passe
            for (int i = 0; i < values.Length; i++)
            {
                double v = values[i];
                if (double.IsNaN(v))
                    continue;

                // Mise à jour des statistiques
                if (v < minValue) 
                    minValue = v;
                if (v > maxValue)
                    maxValue = v;

                // Vérification des caractéristiques
                if (Math.Floor(v) != v) isInteger = false;
                if (v < 0) hasNegative = true;

                // Détection de la nécessité d'un double (plutôt qu'un float)
                if (!needsDoublePresicion && !isInteger)
                {
                    needsDoublePresicion = Math.Abs(v) > float.MaxValue ||
                        (v != 0 && Math.Abs(v) < float.MinValue) ||
                        v % 1 != 0 && (v * 1000000) % 1 != 0;
                }

                // Optimisation : sortie anticipée si on a trouvé toutes les caractéristiques limitantes
                if (!isInteger && hasNegative && needsDoublePresicion)
                    break;
            }

            return new ArrayMetrics(isInteger, hasNegative, minValue, maxValue, needsDoublePresicion);
        }

        /// <summary>
        /// Extrait les valeurs non nulles (non NaN) d'un span
        /// </summary>
        public static double[] ExtractNonNullValues(ReadOnlySpan<double> values, int precision = 0)
        {
            if (values.IsEmpty)
                return Array.Empty<double>();

            // Pour les grands tableaux, utiliser le parallélisme
            if (values.Length >= PARALLEL_THRESHOLD)
            {
                // Span ne peut pas être utilisé dans les lambdas, donc conversion en tableau
                return ExtractNonNullValuesParallel(values.ToArray(), precision);
            }

            // Pour les petits tableaux, traitement séquentiel
            return ExtractNonNullValuesSequential(values, precision);
        }

        /// <summary>
        /// Version parallèle pour l'extraction des valeurs non NaN
        /// </summary>
        private static double[] ExtractNonNullValuesParallel(double[] values, int precision)
        {
            // Première passe : compter les valeurs non-NaN pour préallouer
            int[] nonNanCounts = new int[Environment.ProcessorCount];

            Parallel.For(0, Environment.ProcessorCount, partitionIndex =>
            {
                int start = partitionIndex * values.Length / Environment.ProcessorCount;
                int end = (partitionIndex + 1) * values.Length / Environment.ProcessorCount;

                int count = 0;
                for (int i = start; i < end; i++)
                {
                    if (!double.IsNaN(values[i]))
                        count++;
                }

                nonNanCounts[partitionIndex] = count;
            });

            // Calculer le total et les offsets pour chaque partition
            int totalNonNanCount = 0;
            int[] offsets = new int[Environment.ProcessorCount];

            for (int i = 0; i < nonNanCounts.Length; i++)
            {
                offsets[i] = totalNonNanCount;
                totalNonNanCount += nonNanCounts[i];
            }

            // Préallouer le tableau de résultat
            double[] result = new double[totalNonNanCount];

            // Deuxième passe : remplir le tableau
            Parallel.For(0, Environment.ProcessorCount, partitionIndex =>
            {
                int start = partitionIndex * values.Length / Environment.ProcessorCount;
                int end = (partitionIndex + 1) * values.Length / Environment.ProcessorCount;
                int resultIndex = offsets[partitionIndex];

                // Fonction de transformation selon la précision
                if (precision == 0)
                {
                    for (int i = start; i < end; i++)
                    {
                        if (!double.IsNaN(values[i]))
                            result[resultIndex++] = values[i];
                    }
                }
                else
                {
                    for (int i = start; i < end; i++)
                    {
                        if (!double.IsNaN(values[i]))
                            result[resultIndex++] = Math.Round(values[i], precision);
                    }
                }
            });

            return result;
        }

        /// <summary>
        /// Version séquentielle pour l'extraction des valeurs non NaN
        /// Optimisée pour ReadOnlySpan et allocation minimale
        /// </summary>
        private static double[] ExtractNonNullValuesSequential(ReadOnlySpan<double> values, int precision)
        {
            // Première passe : compter les éléments non-NaN pour éviter les redimensionnements
            int nonNanCount = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (!double.IsNaN(values[i]))
                    nonNanCount++;
            }

            // Préallouer le tableau de résultat avec la taille exacte
            double[] result = new double[nonNanCount];

            // Deuxième passe : remplir le tableau
            int resultIndex = 0;

            // Sélection de la boucle selon la précision pour éviter la vérification à chaque itération
            if (precision == 0)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    double value = values[i];
                    if (!double.IsNaN(value))
                        result[resultIndex++] = value;
                }
            }
            else
            {
                for (int i = 0; i < values.Length; i++)
                {
                    double value = values[i];
                    if (!double.IsNaN(value))
                        result[resultIndex++] = Math.Round(value, precision);
                }
            }

            return result;
        }
    }
}
