using Microsoft.Windows.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OptimizedProjectHandler.Datas.Tools
{
    /// <summary>
    /// Service for scaling arrays to optimize storage
    /// Optimisé avec Span pour minimiser les allocations mémoire
    /// </summary>
    public static class ArrayScaler
    {
        private const int PARALLEL_THRESHOLD = 100_000;
        private const double EPSILON = 1e-12; // Valeur de précision pour les comparaisons

        /// <summary>
        /// Stratégie simple pour avoir le meilleur rendu, on regarde avec les min et max si une solution pourrait être intéressante
        /// puis on essaye uniquement cette solution
        /// </summary>
        public static (sbyte scaleFactor, sbyte comaFactor) FindFactors(ReadOnlySpan<double> values)
        {
            if (values.IsEmpty)
                return (1, 0);

            sbyte foundComa = FindComa(values);

            var result = FindFactor(values, foundComa);
            if (result.newComa < 0) result.newComa = 0;
            return result;
        }

        /// <summary>
        /// Cette fonction va trouver le meilleur facteur qui permet d'avoir la valeur max dans les doubles sans valeur décimale
        /// </summary>
        private static sbyte FindComa(ReadOnlySpan<double> values)
        {
            if (values.IsEmpty)
                return 0;

            // On commence avec une échelle de -16
            for (sbyte scale = -16; scale <= 15; scale++)
            {
                bool allValuesAreIntegers = true;
                double scalePower = Math.Pow(10, scale);

                for (int i = 0; i < values.Length; i++)
                {
                    if (double.IsNaN(values[i]))
                    {
                        continue;
                    }
                    double value = values[i];

                    if (value == 0) continue; // Ignorer les zéros

                    // Mise à l'échelle
                    double scaled = value * scalePower;

                    // On compare avec la représentation entière
                    // mais on évite les problèmes d'arrondi en vérifiant avec une précision relative
                    double roundedValue = Math.Round(scaled);
                    double relativeError = Math.Abs((scaled - roundedValue) / scaled);

                    // Si l'erreur relative est significative, alors ce n'est pas un entier
                    if (relativeError > EPSILON)
                    {
                        allValuesAreIntegers = false;
                        break;
                    }
                }

                if (allValuesAreIntegers)
                {
                    return scale;
                }
            }

            return 0; // Aucune échelle appropriée trouvée
        }

        /// <summary>
        /// Recherche le meilleur facteur pour optimiser le stockage
        /// </summary>
        private static (sbyte factor, sbyte newComa) FindFactor(ReadOnlySpan<double> values, sbyte comaFactor)
        {
            // Exemple : facteur 6 pour avoir que des entiers, je regarde si 
            // facteur 5 et x2 mon max est passé sous une autre variable,
            // facteur 4 et x4 pareil et facteur 4 et x5

            if (RecursiveFactorTest(values, (sbyte)(comaFactor - 1), 2))
            {
                return (2, (sbyte)(comaFactor - 1));
            }

            if (RecursiveFactorTest(values, (sbyte)(comaFactor - 2), 4))
            {
                return (4, (sbyte)(comaFactor - 2));
            }

            if (RecursiveFactorTest(values, (sbyte)(comaFactor - 2), 5))
            {
                return (5, (sbyte)(comaFactor - 2));
            }

            return (1, comaFactor);
        }

        /// <summary>
        /// Teste si un facteur spécifique est applicable à toutes les valeurs
        /// </summary>
        private static bool RecursiveFactorTest(ReadOnlySpan<double> values, sbyte testComaFactor, sbyte factorTest)
        {
            double comaPower = Math.Pow(10, testComaFactor);
            double multiplier = comaPower * factorTest;

            for (int i = 0; i < values.Length; i++)
            {
                double value = values[i];
                double calculatedValue = value * multiplier;

                double relativeError = Math.Abs((calculatedValue - Math.Round(calculatedValue)) / calculatedValue);

                // Si l'erreur relative est significative, alors ce n'est pas un entier
                if (relativeError > EPSILON)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Applies scale and coma factors to values in-place
        /// </summary>
        public static void ApplyFactors(Span<double> values, sbyte scaleFactor, sbyte comaFactor)
        {
            if (values.IsEmpty || (scaleFactor == 1 && comaFactor == 0))
                return;

            double multiplier = scaleFactor * Math.Pow(10, comaFactor);

            if (Vector.IsHardwareAccelerated && values.Length >= PARALLEL_THRESHOLD)
            {
                // Traitement vectorisé pour les parties alignées
                int vectorSize = Vector<double>.Count;
                int vectorizedLength = values.Length - (values.Length % vectorSize);

                Vector<double> multiplierVector = new Vector<double>(multiplier);

                for (int i = 0; i < vectorizedLength; i += vectorSize)
                {
                    var vec = new Vector<double>(values.Slice(i, vectorSize));
                    vec = Vector.Multiply(vec, multiplierVector);
                    // Application de l'arrondi (complexe en SIMD, peut nécessiter une approche différente)

                    // Copie des résultats dans le span
                    for (int j = 0; j < vectorSize; j++)
                    {
                        values[i + j] = Math.Round(vec[j]);
                    }
                }

                // Traitement des éléments restants
                for (int i = vectorizedLength; i < values.Length; i++)
                {
                    values[i] = Math.Round(values[i] * multiplier);
                }
            }
            else
            {
                // Traitement direct sur le span
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = Math.Round(values[i] * multiplier);
                }
            }
        }

        
        /// <summary>
        /// Unscales a span of values by dividing by the scale and coma factors
        /// </summary>
        public static void UnscaleArray(Span<double> scaledArray, sbyte scaleFactor, sbyte comaFactor)
        {
            if (scaledArray.IsEmpty || (scaleFactor == 1 && comaFactor == 0))
                return;

            double multiplier = 1.0 / scaleFactor * Math.Pow(10, -comaFactor);
            int precision = comaFactor >= 0 ? comaFactor : 0;

            if (scaledArray.Length >= PARALLEL_THRESHOLD)
            {
                // Span ne peut pas être utilisé dans Parallel.For, donc conversion en tableau
                double[] array = scaledArray.ToArray();

                Parallel.For(0, array.Length, i =>
                {
                    if (!double.IsNaN(array[i]))
                    {
                        array[i] = Math.Round(array[i] * multiplier, precision);
                    }
                });

                // Copier les résultats dans le span original
                array.AsSpan().CopyTo(scaledArray);
            }
            else
            {
                for (int i = 0; i < scaledArray.Length; i++)
                {
                    if (!double.IsNaN(scaledArray[i]))
                    {
                        scaledArray[i] = Math.Round(scaledArray[i] * multiplier, precision);
                    }
                }
            }
        }

        /// <summary>
        /// Surcharge pour la compatibilité avec les tableaux
        /// </summary>
        public static void UnscaleArray(double[] scaledArray, sbyte scaleFactor, sbyte comaFactor)
        {
            if (scaledArray == null || scaledArray.Length == 0)
                return;

            UnscaleArray(scaledArray.AsSpan(), scaleFactor, comaFactor);
        }

        /// <summary>
        /// Creates a scaled array with handling of NaN values
        /// </summary>
        public static double[] CreateScaledArray(ReadOnlySpan<double> originalValues, sbyte scaleFactor, sbyte comaFactor)
        {
            if (originalValues.IsEmpty)
                return Array.Empty<double>();

            double[] result = new double[originalValues.Length];
            double multiplier = scaleFactor * Math.Pow(10, comaFactor);

            if (originalValues.Length >= PARALLEL_THRESHOLD)
            {
                // Conversion en tableau pour Parallel.For
                double[] inputArray = originalValues.ToArray();

                Parallel.For(0, inputArray.Length, i =>
                {
                    double value = inputArray[i];
                    result[i] = double.IsNaN(value)
                        ? double.NaN
                        : Math.Round(value * multiplier);
                });
            }
            else
            {
                for (int i = 0; i < originalValues.Length; i++)
                {
                    double value = originalValues[i];
                    result[i] = double.IsNaN(value)
                        ? double.NaN
                        : Math.Round(value * multiplier);
                }
            }

            return result;
        }
    }
}
