using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Models
{
    /// <summary>
    /// Structure immuable contenant les métriques d'analyse d'un tableau de valeurs
    /// </summary>
    public readonly struct ArrayMetrics
    {
        /// <summary>
        /// Indique si toutes les valeurs sont des entiers
        /// </summary>
        public readonly bool IsInteger;

        /// <summary>
        /// Indique si au moins une valeur est négative
        /// </summary>
        public readonly bool HasNegative;

        /// <summary>
        /// Valeur minimale dans le tableau
        /// </summary>
        public readonly double MinValue;

        /// <summary>
        /// Valeur maximale dans le tableau
        /// </summary>
        public readonly double MaxValue;

        /// <summary>
        /// Indique si la précision double est nécessaire (vs float)
        /// </summary>
        public readonly bool NeedsDoublePresicion;

        /// <summary>
        /// Crée une nouvelle instance de métriques d'analyse
        /// </summary>
        public ArrayMetrics(bool isInteger, bool hasNegative, double minValue, double maxValue, bool needsDoublePresicion)
        {
            IsInteger = isInteger;
            HasNegative = hasNegative;
            MinValue = minValue;
            MaxValue = maxValue;
            NeedsDoublePresicion = needsDoublePresicion;
        }

        /// <summary>
        /// Combine plusieurs métriques d'analyse (par exemple pour des analyses parallèles)
        /// </summary>
        public static ArrayMetrics Combine(ArrayMetrics[] metrics)
        {
            if (metrics == null || metrics.Length == 0)
                return new ArrayMetrics(true, false, 0, 0, false);

            bool isInteger = true;
            bool hasNegative = false;
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            bool needsDoublePresicion = false;

            foreach (var metric in metrics)
            {
                isInteger &= metric.IsInteger;
                hasNegative |= metric.HasNegative;
                minValue = Math.Min(minValue, metric.MinValue);
                maxValue = Math.Max(maxValue, metric.MaxValue);
                needsDoublePresicion |= metric.NeedsDoublePresicion;
            }

            return new ArrayMetrics(isInteger, hasNegative, minValue, maxValue, needsDoublePresicion);
        }

        /// <summary>
        /// Crée une métrique vide (pour les tableaux vides)
        /// </summary>
        public static ArrayMetrics Empty => new(true, false, 0, 0, false);
    }
}
