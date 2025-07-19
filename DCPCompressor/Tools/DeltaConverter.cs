using DCPCompressor.AbstractDataInformations;
using Microsoft.Windows.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DCPCompressor.Tools
{
    /// <summary>
    /// Gestionnaire pour la création et la reconstruction des deltas
    /// Version optimisée avec Span pour réduire les allocations mémoire
    /// </summary>
    public static class DeltaConverter
    {
        private const int PARALLEL_THRESHOLD = 100_000;

        // Cache pour éviter les appels répétés à GetDecimalDigits
        private static readonly ThreadLocal<Dictionary<double, int>> _decimalDigitsCache =
            new ThreadLocal<Dictionary<double, int>>(() => new Dictionary<double, int>(1000));


        public static void FindStartingValue(ReadOnlySpan<double> datas, ICompressedDataInfos datainfos)
        {
            int i = 0;
            for (i = 0; i < datas.Length; i++)
            {
                if (!double.IsNaN(datas[i]))
                {
                    break;
                }
            }
            datainfos.StartingValue = datas[i];
            datainfos.StartingValueIndex = i;
        }
        /// <summary>
        /// Crée des deltas directement dans le tableau fourni
        /// </summary>
        /// <param name="datas">Les données qui seront converties en deltas</param>
        /// <returns>La valeur initiale (première valeur)</returns>
        public static void CreateDeltasInPlace(Memory<double> datas, ICompressedDataInfos datainfos)
        {
            // Vérification des entrées
            if (datas.IsEmpty || datas.Length <= 1)
            {
                return;
            }
            if (datas.Length < PARALLEL_THRESHOLD)
            {
                // Algorithme optimisé pour les tableaux moyens
                CreateDeltasMediumInPlace(datas.Span, datainfos);
            }
            else
            {
                // Algorithme parallélisé pour les grands tableaux
                CreateDeltasLargeInPlace(datas, datainfos);
            }
        }


        /// <summary>
        /// Algorithme optimisé pour les tableaux de taille moyenne avec modification in-place
        /// Pré-calcule toutes les précisions en une passe
        /// </summary>
        private static void CreateDeltasMediumInPlace(Span<double> datas, ICompressedDataInfos datainfos)
        {
            // Pré-calculer les précisions
            int precision = GetMaxDecimalPlaces(datas);
            datainfos.Precision = precision;
            double PreviousValue = double.NaN;
            int j = 0;
            while (double.IsNaN(PreviousValue))
            {
                if (double.IsNaN(datas[j]))
                {
                    j++;
                }
                else
                {
                    PreviousValue = datas[j];
                    datas[j] = 0;
                }
            }
            double CurrentValue = double.NaN;
            datainfos.StartingValue = PreviousValue;
            datainfos.StartingValueIndex = j;

            for (int i = j+1; i < datas.Length; i++)
            {
                if (double.IsNaN(datas[i]))
                {
                    continue;
                }

                CurrentValue = datas[i];

                datas[i] = Math.Round(CurrentValue - PreviousValue, precision);
                PreviousValue = CurrentValue;
            }
        }

      
        /// <summary>
        /// Algorithme parallélisé pour les grands tableaux avec modification minimale
        /// </summary>
        private static void CreateDeltasLargeInPlace(Memory<double> datas, ICompressedDataInfos datainfos)
        {
            int precision = GetMaxDecimalPlaces(datas.Span);
            datainfos.Precision = precision;
            ConcurrentDictionary<int, double> StartValues = new ConcurrentDictionary<int, double>();
            ConcurrentDictionary<int, double> EndValues = new ConcurrentDictionary<int, double>();

            //on va d'abord faire un delta sur chaque gros groupe 
            Parallel.ForEach(
                 Partitioner.Create(0, datas.Length, CompressorSettings.Default.PARALLEL_THRESHOLD),
                    range =>
                    {
                        Span<double> partitionSpan = datas.Slice(range.Item1, range.Item2 - range.Item1).Span;

                        // Recherche la dernière valeur non-NaN du segment
                        double lastValue = double.NaN;
                        int lastIndex = partitionSpan.Length - 1;

                        //on sauvegarde la derniere donnée

                        while (lastIndex >= 0 && double.IsNaN(partitionSpan[lastIndex]))
                        {
                            lastIndex--;
                        }

                        if (lastIndex >= 0)
                        {
                            lastValue = partitionSpan[lastIndex];
                            bool result = EndValues.TryAdd(range.Item1 + lastIndex, lastValue);
                        }

                        // Recherche la première valeur non-NaN du segment
                        double firstValue = double.NaN;
                        int firstIndex = 0;

                        while (firstIndex < partitionSpan.Length && double.IsNaN(partitionSpan[firstIndex]))
                        {
                            firstIndex++;
                        }

                        if (firstIndex < partitionSpan.Length)
                        {
                            firstValue = partitionSpan[firstIndex];
                            StartValues.TryAdd(range.Item1 + firstIndex, firstValue);
                        }

                        double CurrentValue = double.NaN;
                        double PreviousValue = firstValue;
                        //Maintenant on boucle les deltas
                        for (int i = firstIndex+1; i <= lastIndex; i++)
                        {
                            if (double.IsNaN(partitionSpan[i]))
                            {
                                continue;
                            }

                            CurrentValue = partitionSpan[i];

                            partitionSpan[i] = Math.Round(CurrentValue - PreviousValue, precision);
                            PreviousValue = CurrentValue;
                        }
                    });

            //ensuite on corrige les informations initiales
            //La toute premiere devient 0
            //La premiere des autres est corrigée avec la derniere d'avant
            var orderedSegmentStarts = StartValues.OrderBy(kv => kv.Key).ToList();
            var orderedSegmentEnds = EndValues.OrderBy(kv => kv.Key).ToList();
            var dataSpan = datas.Span;

            datainfos.StartingValue = orderedSegmentStarts.First().Value;
            datainfos.StartingValueIndex = orderedSegmentStarts.First().Key;
            dataSpan[orderedSegmentStarts.First().Key] = 0;

            if (orderedSegmentStarts.Count > 1)
            {
                for(int i = 1; i < orderedSegmentStarts.Count; i++)
                {
                    int ConsideredIndex = orderedSegmentStarts[i].Key;

                    double NewValue = Math.Round(orderedSegmentStarts[i].Value - orderedSegmentEnds[i - 1].Value, precision);
                    dataSpan[ConsideredIndex] = NewValue;
                }
            }
        

        }

        /// <summary>
        /// Détermine le nombre maximum de décimales parmi tous les doubles dans un Span
        /// </summary>
        /// <param name="values">Collection de valeurs à analyser</param>
        /// <returns>Le nombre maximum de décimales trouvé</returns>
        public static int GetMaxDecimalPlaces(Span<double> values)
                {
                    if (values.IsEmpty)
                        return 0;

                    int maxDecimalPlaces = 0;

                    // Buffer réutilisable pour la conversion en chaîne (évite les allocations répétées)
                    Span<char> buffer = stackalloc char[32]; // Suffisant pour représenter un double

                    foreach (double value in values)
                    {
                        // Ignorer les valeurs spéciales
                        if (double.IsNaN(value) || double.IsInfinity(value))
                            continue;

                        // Vérification rapide pour les entiers
                        if (value == Math.Truncate(value))
                            continue;

                        // Convertir en chaîne sans allocation d'objet string
                        bool success = value.TryFormat(buffer, out int charsWritten, "G", CultureInfo.InvariantCulture);
                        if (!success)
                            continue;

                        // Rechercher le point décimal
                        int dotIndex = -1;
                        for (int i = 0; i < charsWritten; i++)
                        {
                            if (buffer[i] == '.')
                            {
                                dotIndex = i;
                                break;
                            }
                        }

                        if (dotIndex < 0)
                            continue;

                        // Rechercher l'exposant (notation scientifique)
                        int eIndex = -1;
                        for (int i = dotIndex + 1; i < charsWritten; i++)
                        {
                            if (buffer[i] == 'E' || buffer[i] == 'e')
                            {
                                eIndex = i;
                                break;
                            }
                        }

                        int decimalPlaces = 0;

                        if (eIndex > 0)
                        {
                            // Traiter la notation scientifique
                            int mantissaDecimals = eIndex - dotIndex - 1;

                            // Extraire l'exposant
                            bool isNegativeExponent = buffer[eIndex + 1] == '-';
                            int exponentStartIndex = eIndex + (isNegativeExponent ? 2 : 1);

                            int exponent = 0;
                            for (int i = exponentStartIndex; i < charsWritten; i++)
                            {
                                exponent = exponent * 10 + (buffer[i] - '0');
                            }

                            if (isNegativeExponent)
                                exponent = -exponent;

                            if (exponent < 0)
                                decimalPlaces = mantissaDecimals - exponent;
                            else if (exponent < mantissaDecimals)
                                decimalPlaces = mantissaDecimals - exponent;
                        }
                        else
                        {
                            // Format décimal standard
                            decimalPlaces = charsWritten - dotIndex - 1;

                            // Supprimer les zéros finaux
                            while (decimalPlaces > 0 && buffer[dotIndex + decimalPlaces] == '0')
                            {
                                decimalPlaces--;
                            }
                        }

                // Mettre à jour le maximum sauf si il est absurde
                    if (decimalPlaces > maxDecimalPlaces && decimalPlaces < 15)
                            maxDecimalPlaces = decimalPlaces;
                    }

                    return maxDecimalPlaces;
                }

        /// <summary>
        /// Reconstruit les valeurs directement dans le tableau fourni
        /// </summary>
        public static void RebuildValuesInPlace(Span<double> result, ReadOnlySpan<double> deltaValues, ICompressedDataInfos compressedDataInfos)
        {
            if (deltaValues.IsEmpty)
            {
                if (result.Length > 0)
                    result[0] = compressedDataInfos.StartingValue;
                return;
            }

            double initialValue = compressedDataInfos.StartingValue;
            int offset = 0;
            double previousValue = 0;
            int comaFactor = compressedDataInfos.ComaFactor;

            // Gérer le cas où la valeur initiale est au début
            if (compressedDataInfos.StartingValueBegin)
            {
                if (result.Length > 0)
                    result[0] = initialValue;
                previousValue = initialValue;
                offset = 1;
                initialValue = double.NaN; // Marquer comme utilisée
            }

            // Reconstruire les valeurs
            for (int i = 0; i < deltaValues.Length && (i + offset) < result.Length; i++)
            {
                if (!double.IsNaN(initialValue) && !double.IsNaN(deltaValues[i]))
                {
                    // Insérer la valeur initiale si nécessaire
                    result[i + offset] = initialValue;
                    previousValue = initialValue;
                    initialValue = double.NaN; // Marquer comme utilisée
                    offset = 1;
                    i--; // Retraiter l'index actuel
                }
                else
                {
                    if (!double.IsNaN(deltaValues[i]))
                    {
                        // Calculer la valeur suivante à partir du delta
                        result[i + offset] = Math.Round(previousValue + deltaValues[i], comaFactor);
                        previousValue = result[i + offset];
                    }
                    else
                    {
                        // Préserver les NaN
                        result[i + offset] = double.NaN;
                    }
                }
            }
        }

        /// <summary>
        /// Reconstruit les valeurs à partir de deltas avec compteurs de répétition
        /// </summary>
        public static void RebuildValuesInPlace(Span<double> result, ReadOnlySpan<double> deltaValues, ReadOnlySpan<byte> countValues, ICompressedDataInfos compressedDataInfos)
        {
            // Vérification des entrées
            if (deltaValues.IsEmpty || countValues.IsEmpty)
            {
                if (result.Length > 0)
                    result[0] = compressedDataInfos.StartingValue;
                return;
            }

            // Assurer que les tableaux ont la même longueur
            int dataLength = Math.Min(deltaValues.Length, countValues.Length);
            int resultIndex = 0;

            // La valeur courante commence avec la valeur initiale
            double currentValue = compressedDataInfos.StartingValue;

            // Ajouter la valeur initiale au début si nécessaire
            if (compressedDataInfos.StartingValueBegin && resultIndex < result.Length)
            {
                result[resultIndex++] = currentValue;
            }

            // Précision pour l'arrondi
            int precision = Math.Max(0, (int)compressedDataInfos.ComaFactor);

            // Flag pour suivre si on a inséré la valeur initiale
            bool initialValueInserted = compressedDataInfos.StartingValueBegin;

            // Reconstruire les valeurs
            for (int i = 0; i < dataLength && resultIndex < result.Length; i++)
            {
                // Si on n'a pas encore inséré la valeur initiale et qu'on a une valeur delta
                if (!initialValueInserted && !double.IsNaN(deltaValues[i]))
                {
                    result[resultIndex++] = currentValue;
                    initialValueInserted = true;
                    // Ne pas avancer l'index i car on veut traiter ce delta
                    i--;
                    continue;
                }

                byte count = countValues[i];

                if (!double.IsNaN(deltaValues[i]))
                {
                    double deltaValue = deltaValues[i];

                    // Optimisation: traitement en bloc des répétitions
                    for (int j = 0; j < count && resultIndex < result.Length; j++)
                    {
                        // Recalculer uniquement si la valeur actuelle n'est pas null
                        if (!double.IsNaN(currentValue))
                        {
                            currentValue = Math.Round(currentValue + deltaValue, precision);
                        }
                        else
                        {
                            // Si pour une raison quelconque currentValue est null, utiliser la valeur de départ
                            currentValue = compressedDataInfos.StartingValue;
                        }

                        result[resultIndex++] = currentValue;
                    }
                }
                else
                {
                    // Optimisation: remplissage de NaN en bloc
                    int endIndex = Math.Min(resultIndex + count, result.Length);
                    for (int j = resultIndex; j < endIndex; j++)
                    {
                        result[j] = double.NaN;
                    }
                    resultIndex = endIndex;
                }
            }
        }

        /// <summary>
        /// Reconstruit les valeurs à partir de deltas
        /// </summary>
        public static double[] RebuildValues(ReadOnlySpan<double> deltaValues, ICompressedDataInfos compressedDataInfos)
        {
            if (deltaValues.IsEmpty)
                return new double[] { compressedDataInfos.StartingValue };

            // Préallouer le tableau de résultats
            double[] result = new double[deltaValues.Length + 1];

            double initialValue = compressedDataInfos.StartingValue;
            int offset = 0;
            double previousValue = 0;
            int comaFactor = compressedDataInfos.ComaFactor;

            // Gérer le cas où la valeur initiale est au début
            if (compressedDataInfos.StartingValueBegin)
            {
                result[0] = initialValue;
                previousValue = initialValue;
                offset = 1;
                initialValue = double.NaN; // Marquer comme utilisée
            }

            // Reconstruire les valeurs
            for (int i = 0; i < deltaValues.Length; i++)
            {
                if (!double.IsNaN(initialValue) && !double.IsNaN(deltaValues[i]))
                {
                    // Insérer la valeur initiale si nécessaire
                    result[i + offset] = initialValue;
                    previousValue = initialValue;
                    initialValue = double.NaN; // Marquer comme utilisée
                    offset = 1;
                    i--; // Retraiter l'index actuel
                }
                else
                {
                    if (!double.IsNaN(deltaValues[i]))
                    {
                        // Calculer la valeur suivante à partir du delta
                        result[i + offset] = Math.Round(previousValue + deltaValues[i], comaFactor);
                        previousValue = result[i + offset];
                    }
                    else
                    {
                        // Préserver les NaN
                        result[i + offset] = double.NaN;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Surcharge pour compatibilité avec les tableaux
        /// </summary>
        public static double[] RebuildValues(double[] deltaValues, ICompressedDataInfos compressedDataInfos)
        {
            return deltaValues == null || deltaValues.Length == 0
                ? new double[] { compressedDataInfos.StartingValue }
                : RebuildValues(deltaValues.AsSpan(), compressedDataInfos);
        }

        /// <summary>
        /// Reconstruit les valeurs à partir de deltas avec compteurs de répétition
        /// </summary>
        public static double[] RebuildValues(ReadOnlySpan<double> deltaValues, byte[] countValues, ICompressedDataInfos compressedDataInfos)
        {
            // Vérification des entrées
            if (deltaValues.IsEmpty || countValues == null || countValues.Length == 0)
            {
                return new double[] { compressedDataInfos.StartingValue };
            }

            // Assurer que les tableaux ont la même longueur
            int dataLength = Math.Min(deltaValues.Length, countValues.Length);

            // Calculer la taille totale du tableau résultat
            int totalSize = compressedDataInfos.StartingValueBegin ? 1 : 0;
            for (int i = 0; i < dataLength; i++)
            {
                totalSize += countValues[i];
            }

            // Ajouter une place pour la valeur initiale si elle n'est pas au début
            if (!compressedDataInfos.StartingValueBegin)
                totalSize++;

            // Créer le tableau de résultats
            double[] result = new double[totalSize];
            int resultIndex = 0;

            // La valeur courante commence avec la valeur initiale
            double currentValue = compressedDataInfos.StartingValue;

            // Ajouter la valeur initiale au début si nécessaire
            if (compressedDataInfos.StartingValueBegin)
            {
                result[resultIndex++] = currentValue;
            }

            // Précision pour l'arrondi
            int precision = Math.Max(0, (int)compressedDataInfos.ComaFactor);

            // Flag pour suivre si on a inséré la valeur initiale
            bool initialValueInserted = compressedDataInfos.StartingValueBegin;

            // Reconstruire les valeurs
            for (int i = 0; i < dataLength; i++)
            {
                // Si on n'a pas encore inséré la valeur initiale et qu'on a une valeur delta
                if (!initialValueInserted && !double.IsNaN(deltaValues[i]))
                {
                    result[resultIndex++] = currentValue;
                    initialValueInserted = true;
                    // Ne pas avancer l'index i car on veut traiter ce delta
                    i--;
                    continue;
                }

                byte count = countValues[i];

                if (!double.IsNaN(deltaValues[i]))
                {
                    double deltaValue = deltaValues[i];

                    // Optimisation: traitement en bloc des répétitions
                    for (int j = 0; j < count; j++)
                    {
                        // Recalculer uniquement si la valeur actuelle n'est pas null
                        if (!double.IsNaN(currentValue))
                        {
                            currentValue = Math.Round(currentValue + deltaValue, precision);
                        }
                        else
                        {
                            // Si pour une raison quelconque currentValue est null, utiliser la valeur de départ
                            currentValue = compressedDataInfos.StartingValue;
                        }

                        if (resultIndex < result.Length)
                            result[resultIndex++] = currentValue;
                    }
                }
                else
                {
                    // Optimisation: remplissage de NaN en bloc
                    int endIndex = Math.Min(resultIndex + count, result.Length);
                    for (int j = resultIndex; j < endIndex; j++)
                    {
                        result[j] = double.NaN;
                    }
                    resultIndex = endIndex;
                }
            }

            // Vérifier que nous avons rempli correctement le tableau
            if (resultIndex < totalSize)
            {
                // Il reste de la place et nous n'avons pas encore inséré la valeur initiale
                if (!initialValueInserted && resultIndex < result.Length)
                {
                    result[resultIndex++] = compressedDataInfos.StartingValue;
                }

                // Si nous n'avons toujours pas utilisé tout le tableau, le redimensionner
                if (resultIndex < totalSize)
                {
                    Array.Resize(ref result, resultIndex);
                }
            }

            return result;
        }

        /// <summary>
        /// Surcharge pour compatibilité avec les tableaux
        /// </summary>
        public static double[] RebuildValues(double[] deltaValues, byte[] countValues, ICompressedDataInfos compressedDataInfos)
        {
            return deltaValues == null || deltaValues.Length == 0 || countValues == null || countValues.Length == 0
                ? new double[] { compressedDataInfos.StartingValue }
                : RebuildValues(deltaValues.AsSpan(), countValues, compressedDataInfos);
        }
    }
}
