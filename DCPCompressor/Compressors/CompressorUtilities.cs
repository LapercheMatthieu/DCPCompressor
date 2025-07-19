using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Compressors
{
    public static class CompressorUtilities
    {
        /// <summary>
        /// Renvoie le nombre d'octets pour un type donné
        /// </summary>
        public static int GetBytesForType(Type type)
        {
            if (type == typeof(byte) || type == typeof(sbyte))
                return 1;
            else if (type == typeof(short) || type == typeof(ushort))
                return 2;
            else if (type == typeof(int) || type == typeof(uint) || type == typeof(float))
                return 4;
            else if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
                return 8;
            else
                return 4; // Par défaut
        }
        public static Dictionary<Type, (double Min, double Max, bool RequiresInteger)> TypeRanges()
        {
            return new Dictionary<Type, (double Min, double Max, bool RequiresInteger)>
                {
                    { typeof(double), (double.MinValue, double.MaxValue, false) },
                    { typeof(float),  (float.MinValue,  float.MaxValue,  false) },
                    { typeof(byte),   (byte.MinValue,   byte.MaxValue,   true) },
                    { typeof(sbyte),  (sbyte.MinValue,  sbyte.MaxValue,  true) },
                    { typeof(short),  (short.MinValue,  short.MaxValue,  true) },
                    { typeof(ushort), (ushort.MinValue, ushort.MaxValue, true) },
                    { typeof(int),    (int.MinValue,    int.MaxValue,    true) },
                    { typeof(uint),   (uint.MinValue,   uint.MaxValue,   true) },
                    { typeof(long),   (long.MinValue,   long.MaxValue,   true) },
                    { typeof(ulong),  (ulong.MinValue,  ulong.MaxValue,  true) }
                };
        }
        /// <summary>
        /// Vérifie si toutes les valeurs du tableau peuvent être représentées dans le type cible
        /// </summary>
        public static bool CheckValuesInTypeRange(double[] values, Type targetType)
        {
            // Structure qui définit les limites pour chaque type
            var typeRanges = TypeRanges();

            // Cas spécial pour float: vérifier la précision
            if (targetType == typeof(float))
            {
                return values.All(v => (double)(float)v == v || Math.Abs((double)(float)v - v) < 1e-6);
            }

            // Récupérer les limites du type cible, ou utiliser int par défaut
            if (!typeRanges.TryGetValue(targetType, out var range))
            {
                range = typeRanges[typeof(int)]; // Type par défaut est int
            }

            // Vérifier si toutes les valeurs sont dans la plage et si elles sont entières quand nécessaire
            return values.All(v =>
                v >= range.Min &&
                v <= range.Max &&
                (!range.RequiresInteger || Math.Floor(v) == v));
        }


        /// <summary>
        /// Convertit un type en code byte pour le stockage
        /// </summary>
        public static byte TypeToByte(Type type)
        {
            if (type == typeof(byte)) return 1;
            if (type == typeof(sbyte)) return 2;
            if (type == typeof(short)) return 3;
            if (type == typeof(ushort)) return 4;
            if (type == typeof(int)) return 5;
            if (type == typeof(uint)) return 6;
            if (type == typeof(long)) return 7;
            if (type == typeof(ulong)) return 8;
            if (type == typeof(float)) return 9;
            if (type == typeof(double)) return 10;

            return 5; // Par défaut int
        }

        /// <summary>
        /// Convertit un code byte en type
        /// </summary>
        public static Type ByteToType(byte typeCode)
        {
            switch (typeCode)
            {
                case 1: return typeof(byte);
                case 2: return typeof(sbyte);
                case 3: return typeof(short);
                case 4: return typeof(ushort);
                case 5: return typeof(int);
                case 6: return typeof(uint);
                case 7: return typeof(long);
                case 8: return typeof(ulong);
                case 9: return typeof(float);
                case 10: return typeof(double);
                default: return typeof(int);
            }
        }
        /// <summary>
        /// Détermine le type optimal pour une valeur individuelle
        /// </summary>
        public static Type GetTypeForValue(double value)
        {
            var typeRanges = TypeRanges();

            // Vérifier si c'est un entier
            bool isInteger = Math.Floor(value) == value;

            if (isInteger)
            {
                // Pour les entiers, parcourir les types entiers du plus petit au plus grand
                foreach (var typeInfo in typeRanges.Where(t => t.Value.RequiresInteger)
                                                  .OrderBy(t => GetBytesForType(t.Key)))
                {
                    if (value >= typeInfo.Value.Min && value <= typeInfo.Value.Max)
                    {
                        return typeInfo.Key;
                    }
                }
            }

            // Pour les valeurs décimales, vérifier si float est suffisant
            if (Math.Abs((double)(float)value - value) < 1e-6)
            {
                return typeof(float);
            }

            // Par défaut, utiliser double
            return typeof(double);
        }
    }
}
