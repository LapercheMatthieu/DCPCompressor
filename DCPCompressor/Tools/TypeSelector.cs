using DCPCompressor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizedProjectHandler.Datas.Tools
{
    /// <summary>
    /// Service de sélection du type optimal pour stocker des données
    /// </summary>
    public static class TypeSelector
    {
        /// <summary>
        /// Détermine le type optimal en fonction des métriques d'analyse
        /// </summary>
        public static Type DetermineOptimalType(in ArrayMetrics metrics)
        {
            if (metrics.IsInteger)
            {
                if (!metrics.HasNegative)
                {
                    if (metrics.MaxValue <= byte.MaxValue) return typeof(byte);
                    if (metrics.MaxValue <= ushort.MaxValue) return typeof(ushort);
                    if (metrics.MaxValue <= uint.MaxValue) return typeof(uint);
                    return typeof(double);
                }

                if (metrics.MinValue >= sbyte.MinValue && metrics.MaxValue <= sbyte.MaxValue) return typeof(sbyte);
                if (metrics.MinValue >= short.MinValue && metrics.MaxValue <= short.MaxValue) return typeof(short);
                if (metrics.MinValue >= int.MinValue && metrics.MaxValue <= int.MaxValue) return typeof(int);
                return typeof(double);
            }

            return metrics.NeedsDoublePresicion ? typeof(double) : typeof(float);
        }

        /// <summary>
        /// Détermine le type le plus englobant entre deux types
        /// </summary>
        public static Type GetMostEncompassingType(Type type1, Type type2)
        {
            // Si les types sont identiques, retourner l'un des deux
            if (type1 == type2) return type1;

            // Si l'un des types est double, le résultat est double
            if (type1 == typeof(double) || type2 == typeof(double))
                return typeof(double);

            // Si l'un des types est float, le résultat est float sauf si l'autre type est double
            if (type1 == typeof(float) || type2 == typeof(float))
                return typeof(float);

            // Pour les types entiers
            var integerTypes = new Dictionary<Type, int>
            {
                { typeof(sbyte), 1 },
                { typeof(byte), 2 },
                { typeof(short), 3 },
                { typeof(ushort), 4 },
                { typeof(int), 5 },
                { typeof(uint), 6 },
                { typeof(long), 7 },
                { typeof(ulong), 8 }
            };

            // Si les deux types sont des entiers
            if (integerTypes.ContainsKey(type1) && integerTypes.ContainsKey(type2))
            {
                // Cas spécial pour les types signés/non signés
                if ((type1 == typeof(uint) && type2 == typeof(int)) ||
                    (type2 == typeof(uint) && type1 == typeof(int)))
                    return typeof(long);

                if ((type1 == typeof(ulong) && type2 == typeof(long)) ||
                    (type2 == typeof(ulong) && type1 == typeof(long)))
                    return typeof(double);

                // Sinon, prendre le type avec le rang le plus élevé
                return integerTypes[type1] > integerTypes[type2] ? type1 : type2;
            }

            // Par défaut, retourner double pour garantir la précision
            return typeof(double);
        }

        /// <summary>
        /// Dictionnaire des valeurs représentant NULL pour chaque type
        /// </summary>
        private static readonly Dictionary<Type, object> NullRepresentationCache = new()
        {
            { typeof(byte), byte.MaxValue },
            { typeof(sbyte), sbyte.MaxValue },
            { typeof(short), short.MaxValue },
            { typeof(ushort), ushort.MaxValue },
            { typeof(int), int.MaxValue },
            { typeof(uint), uint.MaxValue },
            { typeof(float), float.MaxValue },
            { typeof(double), double.MaxValue }
        };

        /// <summary>
        /// Obtient la valeur représentant NULL pour un type donné
        /// </summary>
        public static object GetNullRepresentation(Type type)
        {
            if (NullRepresentationCache.TryGetValue(type, out var value))
                return value;

            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte => byte.MaxValue,
                TypeCode.SByte => sbyte.MaxValue,
                TypeCode.Int16 => short.MaxValue,
                TypeCode.UInt16 => ushort.MaxValue,
                TypeCode.Int32 => int.MaxValue,
                TypeCode.UInt32 => uint.MaxValue,
                TypeCode.Single => float.MaxValue,
                TypeCode.Double => double.MaxValue,
                _ => throw new NotSupportedException($"Type non pris en charge: {type}")
            };
        }
    }
}
