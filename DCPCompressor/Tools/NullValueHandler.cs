using DCPCompressor.AbstractDataInformations;
using DCPCompressor.Compressors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Tools
{
    /// <summary>
    /// Gestionnaire pour le traitement des valeurs nulles (NaN) dans les données
    /// Optimisé pour l'utilisation de Span
    /// </summary>
    public static class NullValueHandler
    {
        private const int PARALLEL_THRESHOLD = 100_000;

        /// <summary>
        /// Injecte une valeur spécifique pour représenter les NaN dans le tableau compressé
        /// </summary>
        /// <param name="delta">Tableau des différences delta</param>
        /// <param name="dataToOptimize">Données originales contenant potentiellement des NaN</param>
        /// <param name="infos">Informations sur la compression</param>
        /// <param name="offset">Décalage initial (généralement 1 pour la valeur de départ)</param>
        /// <returns>Tableau avec les valeurs nulles injectées</returns>
        public static void InjectNullValue(Span<double> dataToOptimize, ICompressedDataInfos infos, int offset = 1)
        {

            // Trouver la valeur à utiliser pour représenter NaN
            infos.NullValue = FindNullValue(dataToOptimize, infos);
            double nullValue = Convert.ToDouble(infos.NullValue);


            // Variables pour le suivi de l'injection
            bool firstValueFound = false;
            int resultIndex = 0;
            int deltaIndex = 0;

            // Traitement séquentiel de l'injection
            for (int i = 0; i < dataToOptimize.Length; i++)
            {
                if (double.IsNaN(dataToOptimize[i]))
                {
                    // Injecter la valeur spéciale pour NaN
                    dataToOptimize[i] = nullValue;
                }
            }
        }

        /// <summary>
        /// Trouve une valeur optimale pour représenter les NaN dans le type cible
        /// </summary>
        /// <param name="delta">Tableau des différences delta</param>
        /// <param name="infos">Informations sur la compression</param>
        /// <returns>Valeur à utiliser pour représenter les NaN</returns>
        public static double FindNullValue(ReadOnlySpan<double> delta, ICompressedDataInfos infos)
        {
            if (delta.IsEmpty)
                return double.NaN;

            // Obtenir les bornes du type cible
            var datatypeRange = NumericRanges.GetRange(infos.DataType);
            double typeMax = Convert.ToDouble(datatypeRange.Max);
            double typeMin = Convert.ToDouble(datatypeRange.Min);

            // Trouver les valeurs min et max dans le tableau delta
            double min = double.MaxValue;
            double max = double.MinValue;
            bool containsZero = false;

            for (int i = 0; i < delta.Length; i++)
            {
                double value = delta[i];

                if (value < min) min = value;
                if (value > max) max = value;
                if (Math.Abs(value) < 1e-10) containsZero = true;
            }

            // Vérifier si la valeur max du type peut être utilisée
            if (max < typeMax)
            {
                return typeMax;
            }
            // Sinon, vérifier la valeur min
            else if (min > typeMin)
            {
                return typeMin;
            }
            // Sinon, essayer avec zéro
            else if (!containsZero)
            {
                return 0;
            }
            // En dernier recours, passer au type supérieur
            else
            {
                infos.DataType = NumericRanges.GetHigherType(infos.DataType);
                return Convert.ToDouble(NumericRanges.GetRange(infos.DataType).Max);
            }
        }

        /// <summary>
        /// Remplace les valeurs spéciales représentant NaN par de vrais NaN
        /// </summary>
        /// <param name="values">Tableau de valeurs potentiellement contenant des valeurs spéciales</param>
        /// <param name="nullValue">Valeur spéciale représentant NaN</param>
        /// <returns>Tableau avec les NaN restaurés</returns>
        public static void ReplaceNullValues(Memory<double> values, object nullValue)
        {
            if (values.IsEmpty)
                return;

            double nullValueDouble = Convert.ToDouble(nullValue);

            // Optimisation pour les grands tableaux
            if (values.Length >= PARALLEL_THRESHOLD)
            {

                Parallel.ForEach(
                Partitioner.Create(0, values.Length, CompressorSettings.Default.PARALLEL_THRESHOLD),
                range =>
                {
                    // Obtenir une tranche Memory pour cette partition
                    Span<double> partitionSpan = values.Slice(range.Item1, range.Item2 - range.Item1).Span;

                    // Traiter la partition
                    for (int i = 0; i < partitionSpan.Length; i++)
                    {
                        if(partitionSpan[i] == nullValueDouble) partitionSpan[i] = double.NaN;
                    }
                });
            }
            else
            {
                // Version séquentielle optimisée
                Span<double> partitionSpan = values.Span;
                for (int i = 0; i < values.Length; i++)
                {
                    if (partitionSpan[i] == nullValueDouble) partitionSpan[i] = double.NaN;
                }
            }
        }


    }
}
