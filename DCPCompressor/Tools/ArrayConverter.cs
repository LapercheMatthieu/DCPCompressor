using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OptimizedProjectHandler.Datas.Tools
{
    /// <summary>
    /// Service de conversion entre différents types de tableaux
    /// </summary>
    public static class ArrayConverter
    {
        private static int PARALLEL_THRESHOLD = 100_000;

        /// <summary>
        /// Convertit un tableau de doubles vers un tableau du type spécifié
        /// </summary>
        public static Array ConvertToType(double[] values, Type targetType)
        {
            if (values == null || values.Length == 0)
                return Array.CreateInstance(targetType, 0);

            Array result = Array.CreateInstance(targetType, values.Length);

            if (values.Length >= PARALLEL_THRESHOLD)
            {
                var partitioner = Partitioner.Create(0, values.Length);
                Parallel.ForEach(partitioner, range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        result.SetValue(Convert.ChangeType(values[i], targetType), i);
                    }
                });
            }
            else
            {
                for (int i = 0; i < values.Length; i++)
                {
                    result.SetValue(Convert.ChangeType(values[i], targetType), i);
                }
            }

            return result;
        }

        /// <summary>
        /// Convertit un tableau optimisé en tableau de doubles
        /// </summary>
        public static double[] ConvertToDoubleArray(Array optimizedArray, int scaleFactor, object nullValue)
        {
            if (optimizedArray == null || optimizedArray.Length == 0)
                return Array.Empty<double>();

            List<double> finalArray = new List<double>();

            for (int i = 0; i < optimizedArray.Length; i++)
            {
                var value = optimizedArray.GetValue(i);

                // Vérifier si la valeur correspond à la représentation NULL
                if (Convert.ToDouble(value) != Convert.ToDouble(nullValue))
                {
                    finalArray.Add(Convert.ToDouble(value) / scaleFactor);
                }
            }

            return finalArray.ToArray();
        }

        /// <summary>
        /// Convertit un tableau optimisé en tableau de doubles nullables
        /// </summary>
        public static double?[] ConvertToNullableDoubleArray(Array optimizedArray, int scaleFactor, object nullValue)
        {
            if (optimizedArray == null || optimizedArray.Length == 0)
                return Array.Empty<double?>();

            double?[] result = new double?[optimizedArray.Length];

            for (int i = 0; i < optimizedArray.Length; i++)
            {
                var value = optimizedArray.GetValue(i);

                // Vérifier si la valeur correspond à la représentation NULL
                if (Convert.ToDouble(value) == Convert.ToDouble(nullValue))
                {
                    result[i] = null;
                }
                else
                {
                    // Convertir et mettre à l'échelle
                    result[i] = Convert.ToDouble(value) / scaleFactor;
                }
            }

            return result;
        }

        /// <summary>
        /// Calcule la taille mémoire d'un tableau
        /// </summary>
        public static long GetArrayMemorySize(Array array)
        {
            if (array == null) return 0;

            Type elementType = array.GetType().GetElementType();
            int elementSize = Marshal.SizeOf(elementType);
            return array.Length * elementSize;
        }
    }
}
