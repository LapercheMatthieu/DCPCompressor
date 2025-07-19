using DCPCompressor.AbstractDataInformations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Compressors
{
    /// <summary>
    /// Classe qui compare différentes stratégies de compression pour déterminer la plus efficace
    /// </summary>
    public static class CompressionComparator
    {
        /// <summary>
        /// Compare les différentes méthodes de compression et retourne la plus efficace
        /// </summary>
        /// <param name="deltasValues">Valeurs delta à compresser</param>
        /// <param name="dataType">Type de données optimal pour le tableau</param>
        /// <param name="sampleSize">Taille de l'échantillon à utiliser</param>
        /// <param name="ratio">Ratio de compression requis pour choisir DeltaCount</param>
        /// <returns>Tuple contenant les types de compression recommandés</returns>
        public static (CompressionEnums, CompressionTypeEnums) Compare(
            ReadOnlySpan<double> deltasValues,
            Type dataType,
            int sampleSize = 10000,
            double ratio = 0.8)
        {
            // Prendre un échantillon des données si nécessaire
            sampleSize = Math.Min(sampleSize, 100_000);
            double[] sample = new double[sampleSize];
            for(int i = 0 ; i < sampleSize; i++)
            {
                sample[i] = deltasValues[i];
            }
            
            // Comparer les méthodes de compression
            CompressionEnums deltaMethod = CompareDeltaDeltaCount(sample, ratio);
            CompressionTypeEnums dataStructure = CompareFixeVariable(sample, dataType);

            return (deltaMethod, dataStructure);
        }

        /// <summary>
        /// Compare les méthodes de compression Delta et DeltaCount
        /// </summary>
        /// <param name="sample">Échantillon de données</param>
        /// <param name="ratio">Ratio de compression requis pour choisir DeltaCount</param>
        /// <returns>Méthode de compression recommandée</returns>
        private static CompressionEnums CompareDeltaDeltaCount(double[] sample, double ratio = 0.8)
        {
            // Si l'échantillon est trop petit, utiliser Delta simple
            if (sample.Length < 2)
                return CompressionEnums.Delta;

            // Calculer les deltas
            var deltas = new List<double>();
            var counts = new List<byte>();

            double currentDelta = sample[0];
            byte repeatCount = 1;

            // Parcourir l'échantillon pour compter les répétitions
            for (int i = 1; i < sample.Length; i++)
            {
                // Si le delta est le même
                if (Math.Abs(sample[i] - currentDelta) < double.Epsilon && repeatCount < 255)
                {
                    repeatCount++;
                }
                else
                {
                    deltas.Add(currentDelta);
                    counts.Add(repeatCount);
                    currentDelta = sample[i];
                    repeatCount = 1;
                }
            }

            // Ajouter le dernier groupe
            deltas.Add(currentDelta);
            counts.Add(repeatCount);

            // Calculer le taux de compression estimé
            int standardSize = sample.Length * sizeof(double);
            int deltaCountSize = deltas.Count * (sizeof(double) + sizeof(byte));

            // Calculer le ratio
            double compressionRatio = (double)deltaCountSize / standardSize;

            // Si le système delta-count est suffisamment efficace, le choisir
            return compressionRatio < ratio ? CompressionEnums.DeltaCount : CompressionEnums.Delta;
        }

        /// <summary>
        /// Compare les méthodes de compression fixe et variable pour déterminer la plus efficace
        /// </summary>
        /// <param name="samples">Échantillon de données</param>
        /// <param name="dataType">Type de données optimal pour l'ensemble</param>
        /// <returns>Méthode de compression recommandée</returns>
        public static CompressionTypeEnums CompareFixeVariable(double[] samples, Type dataType)
        {
            // Types qui bénéficient toujours de la compression fixe
            var smallTypes = new Type[]
            {
                typeof(byte), typeof(sbyte), typeof(short), typeof(ushort)
            };

            // Si le type optimal est déjà petit, la compression fixe est forcément meilleure
            if (smallTypes.Contains(dataType))
            {
                return CompressionTypeEnums.Fixe;
            }

            // Cas pour les doubles avec forte variabilité (éviter l'octet de type)
            if (dataType == typeof(double) && HasHighVariability(samples))
            {
                return CompressionTypeEnums.Fixe;
            }

            // Calculer le pourcentage de valeurs qui peuvent être stockées avec un type plus petit
            int smallerTypeCount = CountSmallerTypeValues(samples, dataType);
            double smallerTypePercentage = (double)smallerTypeCount / samples.Length;

            // Si un nombre significatif de valeurs peut être stocké plus efficacement
            const double threshold = 0.30; // Seuil de 30%
            return smallerTypePercentage > threshold
                ? CompressionTypeEnums.Variable
                : CompressionTypeEnums.Fixe;
        }

        /// <summary>
        /// Compte le nombre de valeurs qui peuvent être stockées avec un type plus petit
        /// </summary>
        private static int CountSmallerTypeValues(double[] samples, Type dataType)
        {
            int count = 0;
            int baseTypeSize = CompressorUtilities.GetBytesForType(dataType);

            foreach (var value in samples)
            {
                Type valueType = CompressorUtilities.GetTypeForValue(value);
                if (CompressorUtilities.GetBytesForType(valueType) < baseTypeSize)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Détermine si les données ont une forte variabilité
        /// </summary>
        private static bool HasHighVariability(double[] values)
        {
            // Calculer écart-type
            double mean = values.Average();

            // Éviter les divisions par zéro
            if (Math.Abs(mean) < double.Epsilon)
            {
                return true;
            }

            double sumOfSquares = values.Sum(x => Math.Pow(x - mean, 2));
            double standardDeviation = Math.Sqrt(sumOfSquares / values.Length);
            double coefficient = standardDeviation / Math.Abs(mean);

            // Si coefficient de variation élevé, forte variabilité
            const double variabilityThreshold = 0.5;
            return coefficient > variabilityThreshold;
        }
    }
}
