using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Tools
{
    /// <summary>
    /// Classe utilitaire qui fournit les plages (valeurs minimales et maximales) 
    /// pour tous les types numériques primitifs en C#.
    /// </summary>
    public static class NumericRanges
    {
        /// <summary>
        /// Obtient la valeur minimale et maximale pour un type numérique donné.
        /// </summary>
        /// <param name="type">Le type pour lequel obtenir la plage</param>
        /// <returns>Un tuple contenant (min, max) pour le type spécifié</returns>
        /// <remarks>
        /// Plages des types numériques:
        /// - byte: 0 à 255 (non signé sur 8 bits)
        /// - sbyte: -128 à 127 (signé sur 8 bits)
        /// - short: -32,768 à 32,767 (signé sur 16 bits)
        /// - ushort: 0 à 65,535 (non signé sur 16 bits)
        /// - int: -2,147,483,648 à 2,147,483,647 (signé sur 32 bits)
        /// - uint: 0 à 4,294,967,295 (non signé sur 32 bits)
        /// - long: -9,223,372,036,854,775,808 à 9,223,372,036,854,775,807 (signé sur 64 bits)
        /// - ulong: 0 à 18,446,744,073,709,551,615 (non signé sur 64 bits)
        /// - float: ±1.5 × 10^-45 à ±3.4 × 10^38 (précision ~7 chiffres)
        /// - double: ±5.0 × 10^-324 à ±1.7 × 10^308 (précision ~15-16 chiffres)
        /// - decimal: ±1.0 × 10^-28 à ±7.9 × 10^28 (précision 28-29 chiffres)
        /// </remarks>
        public static (object Min, object Max) GetRange(Type type)
        {
            if (type == typeof(byte))
                return (byte.MinValue, byte.MaxValue);       // 0 à 255
            else if (type == typeof(sbyte))
                return (sbyte.MinValue, sbyte.MaxValue);     // -128 à 127
            else if (type == typeof(short))
                return (short.MinValue, short.MaxValue);     // -32,768 à 32,767
            else if (type == typeof(ushort))
                return (ushort.MinValue, ushort.MaxValue);   // 0 à 65,535
            else if (type == typeof(int))
                return (int.MinValue, int.MaxValue);         // -2,147,483,648 à 2,147,483,647
            else if (type == typeof(uint))
                return (uint.MinValue, uint.MaxValue);       // 0 à 4,294,967,295
            else if (type == typeof(long))
                return (long.MinValue, long.MaxValue);       // -9,223,372,036,854,775,808 à 9,223,372,036,854,775,807
            else if (type == typeof(ulong))
                return (ulong.MinValue, ulong.MaxValue);     // 0 à 18,446,744,073,709,551,615
            else if (type == typeof(float))
                return (float.MinValue, float.MaxValue);     // ±1.5 × 10^-45 à ±3.4 × 10^38
            else if (type == typeof(double))
                return (double.MinValue, double.MaxValue);   // ±5.0 × 10^-324 à ±1.7 × 10^308
            else if (type == typeof(decimal))
                return (decimal.MinValue, decimal.MaxValue); // ±1.0 × 10^-28 à ±7.9 × 10^28
            else
                throw new ArgumentException($"Le type {type.Name} n'est pas un type numérique pris en charge.");
        }

        /// <summary>
        /// Détermine si une valeur double peut être représentée par un type numérique spécifié.
        /// </summary>
        /// <param name="value">La valeur à vérifier</param>
        /// <param name="type">Le type cible</param>
        /// <returns>True si la valeur peut être représentée par le type, false sinon</returns>
        public static bool CanFitInType(double value, Type type)
        {
            if (type == typeof(byte))
                return value >= byte.MinValue && value <= byte.MaxValue && Math.Floor(value) == value;
            else if (type == typeof(sbyte))
                return value >= sbyte.MinValue && value <= sbyte.MaxValue && Math.Floor(value) == value;
            else if (type == typeof(short))
                return value >= short.MinValue && value <= short.MaxValue && Math.Floor(value) == value;
            else if (type == typeof(ushort))
                return value >= ushort.MinValue && value <= ushort.MaxValue && Math.Floor(value) == value;
            else if (type == typeof(int))
                return value >= int.MinValue && value <= int.MaxValue && Math.Floor(value) == value;
            else if (type == typeof(uint))
                return value >= uint.MinValue && value <= uint.MaxValue && Math.Floor(value) == value;
            else if (type == typeof(long))
                return value >= long.MinValue && value <= long.MaxValue && Math.Floor(value) == value;
            else if (type == typeof(ulong))
                return value >= 0 && value <= ulong.MaxValue && Math.Floor(value) == value;
            else if (type == typeof(float))
                return !double.IsNaN(value) && !double.IsInfinity(value) &&
                       value >= float.MinValue && value <= float.MaxValue;
            else if (type == typeof(double))
                return !double.IsNaN(value) && !double.IsInfinity(value);
            else if (type == typeof(decimal))
                return value >= (double)decimal.MinValue && value <= (double)decimal.MaxValue;
            else
                return false;
        }

        /// <summary>
        /// Suggère le type numérique le plus petit qui peut contenir toutes les valeurs dans un tableau.
        /// </summary>
        /// <param name="values">Tableau de valeurs à analyser</param>
        /// <returns>Le type suggéré, ou double si aucun type plus petit ne convient</returns>
        public static Type SuggestOptimalType(double[] values)
        {
            if (values == null || values.Length == 0)
                return typeof(byte); // Par défaut le plus petit type

            double min = double.MaxValue;
            double max = double.MinValue;
            bool hasDecimals = false;

            // Analyser les valeurs
            foreach (double value in values)
            {
                min = Math.Min(min, value);
                max = Math.Max(max, value);

                if (Math.Floor(value) != value)
                    hasDecimals = true;
            }

            // Si les valeurs ont des décimales, on doit utiliser float ou double
            if (hasDecimals)
            {
                if (min >= float.MinValue && max <= float.MaxValue)
                    return typeof(float);
                else
                    return typeof(double);
            }

            // Pour les entiers, trouver le plus petit type qui peut contenir toutes les valeurs
            if (min >= 0)
            {
                if (max <= byte.MaxValue)
                    return typeof(byte);
                else if (max <= ushort.MaxValue)
                    return typeof(ushort);
                else if (max <= uint.MaxValue)
                    return typeof(uint);
                else if (max <= ulong.MaxValue)
                    return typeof(ulong);
            }
            else
            {
                if (min >= sbyte.MinValue && max <= sbyte.MaxValue)
                    return typeof(sbyte);
                else if (min >= short.MinValue && max <= short.MaxValue)
                    return typeof(short);
                else if (min >= int.MinValue && max <= int.MaxValue)
                    return typeof(int);
                else if (min >= long.MinValue && max <= long.MaxValue)
                    return typeof(long);
            }

            // Si aucun type entier ne convient, utiliser double
            return typeof(double);
        }

        /// <summary>
        /// Fournit un tableau de référence de tous les types numériques pour l'analyse
        /// </summary>
        public static Type[] AllNumericTypes = new Type[]
        {
            typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double),
            typeof(decimal)
        };

        /// <summary>
        /// Renvoie le nombre d'octets utilisés pour représenter le type spécifié
        /// </summary>
        public static int GetByteSize(Type type)
        {
            if (type == typeof(byte) || type == typeof(sbyte))
                return 1;
            else if (type == typeof(short) || type == typeof(ushort))
                return 2;
            else if (type == typeof(int) || type == typeof(uint) || type == typeof(float))
                return 4;
            else if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
                return 8;
            else if (type == typeof(decimal))
                return 16;
            else
                throw new ArgumentException($"Le type {type.Name} n'est pas un type numérique pris en charge.");
        }

        /// <summary>
        /// Affiche un tableau récapitulatif des plages de tous les types numériques
        /// </summary>
        public static string GetRangeSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Plages des types numériques:");
            sb.AppendLine("============================");

            foreach (var type in AllNumericTypes)
            {
                var (min, max) = GetRange(type);
                sb.AppendLine($"{type.Name} ({GetByteSize(type)} octets): Min = {min}, Max = {max}");
            }

            return sb.ToString();
        }

        public static Type GetHigherType(Type type)
        {
            // Séquence des types entiers non signés par taille croissante
            if (type == typeof(byte))
                return typeof(ushort);
            else if (type == typeof(ushort))
                return typeof(uint);
            else if (type == typeof(uint))
                return typeof(ulong);
            else if (type == typeof(ulong))
                return typeof(float); // Passer à float car pas de type entier non signé plus grand

            // Séquence des types entiers signés par taille croissante
            else if (type == typeof(sbyte))
                return typeof(short);
            else if (type == typeof(short))
                return typeof(int);
            else if (type == typeof(int))
                return typeof(long);
            else if (type == typeof(long))
                return typeof(float); // Passer à float car pas de type entier signé plus grand

            // Types à virgule flottante
            else if (type == typeof(float))
                return typeof(double);
            else if (type == typeof(double))
                return typeof(decimal);

            // Si aucun type supérieur n'est trouvé ou si le type est déjà le plus grand
            return type;
        }
    }
}
