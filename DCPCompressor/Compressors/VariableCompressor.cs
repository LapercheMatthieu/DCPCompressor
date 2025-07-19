using DCPCompressor.Models;
using DCPCompressor.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Compressors
{
    /// <summary>
    /// Classe qui compresse des valeurs en utilisant un type adapté à chaque valeur individuelle
    /// </summary>
    public class VariableCompressor
    {
        /// <summary>
        /// Version optimisée pour de grands tableaux utilisant le partitionnement
        /// </summary>
        /// <param name="values">Valeurs à compresser</param>
        /// <param name="compData">Objet CompressedData à remplir</param>
        public static void VariableLengthCompressDelta(Span<double> values, CompressedData compData)
        {
            if (values == null || values.Length == 0)
                return;

            // Première étape: calculer la taille totale nécessaire
            int[] typeSizes = new int[values.Length];
            int partitionSize = 100000; // Taille de bloc pour le partitionnement

            // Pour les petits tableaux, traitement séquentiel
            if (values.Length < partitionSize)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    Type bestType = CompressorUtilities.GetTypeForValue(values[i]);
                    typeSizes[i] = CompressorUtilities.GetBytesForType(bestType) + 1; // +1 pour le code du type
                }
            }
            else
            {
                // Pour grands tableaux: traitement parallèle avec conversion
                double[] valuesArray = values.ToArray();

                // Calculer les tailles par partition
                Parallel.ForEach(
                    Partitioner.Create(0, valuesArray.Length, partitionSize),
                    range =>
                    {
                        for (int i = range.Item1; i < range.Item2; i++)
                        {
                            Type bestType = CompressorUtilities.GetTypeForValue(valuesArray[i]);
                            typeSizes[i] = CompressorUtilities.GetBytesForType(bestType) + 1; // +1 pour le code du type
                        }
                    });
            }

            // Calculer les offsets cumulatifs
            int[] offsets = new int[values.Length + 1];
            for (int i = 0; i < values.Length; i++)
            {
                offsets[i + 1] = offsets[i] + typeSizes[i];
            }

            // Créer le buffer à la taille exacte
            byte[] buffer = new byte[offsets[values.Length]];

            // Pour les petits tableaux, traitement séquentiel
            if (values.Length < partitionSize)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    double value = values[i];
                    int offset = offsets[i];

                    Type bestType = CompressorUtilities.GetTypeForValue(value);
                    buffer[offset] = CompressorUtilities.TypeToByte(bestType);
                    ByteConversionUtility.WriteValueToBuffer(value, bestType, buffer, offset + 1);
                }
            }
            else
            {
                // Pour grands tableaux: traitement parallèle avec conversion
                double[] valuesArray = values.ToArray();

                // Compresser les valeurs en parallèle
                Parallel.ForEach(
                    Partitioner.Create(0, valuesArray.Length, partitionSize),
                    range =>
                    {
                        for (int i = range.Item1; i < range.Item2; i++)
                        {
                            double value = valuesArray[i];
                            int offset = offsets[i];

                            Type bestType = CompressorUtilities.GetTypeForValue(value);
                            buffer[offset] = CompressorUtilities.TypeToByte(bestType);
                            ByteConversionUtility.WriteValueToBuffer(value, bestType, buffer, offset + 1);
                        }
                    });
            }

            // Assigner le résultat à CompressedData
            compData.MainDatas = buffer;
        }


        /// <summary>
        /// Version optimisée pour de grands tableaux utilisant le partitionnement 
        /// avec compression DeltaCount
        /// </summary>
        /// <param name="values">Valeurs à compresser</param>
        /// <param name="compData">Objet CompressedData à remplir</param>
        // Version alternative avec moins d'allocations et encore plus optimisée
        public static void VariableLengthCompressDeltaCount(Span<double> values, CompressedData compData)
        {
            if (values == null || values.Length == 0)
                return;

            // Étape 1: Faire un premier passage pour compter les groupes
            int groupCount = CountGroups(values);

            // Étape 2: Créer des arrays de la bonne taille
            var deltaValues = new double[groupCount];
            var deltaTypes = new Type[groupCount];
            var countValues = new byte[groupCount];

            // Étape 3: Remplir les arrays en un second passage
            FillArrays(values, deltaValues, deltaTypes, countValues);

            // Étape 4: Créer le buffer final avec la taille exacte
            int totalBufferSize = 0;
            for (int i = 0; i < groupCount; i++)
            {
                totalBufferSize += CompressorUtilities.GetBytesForType(deltaTypes[i]) + 1; // +1 pour le type
            }

            byte[] finalDeltaBuffer = new byte[totalBufferSize];

            // Étape 5: Remplir le buffer final
            int currentPos = 0;
            for (int i = 0; i < groupCount; i++)
            {
                finalDeltaBuffer[currentPos] = CompressorUtilities.TypeToByte(deltaTypes[i]);
                currentPos++;

                ByteConversionUtility.WriteValueToBuffer(deltaValues[i], deltaTypes[i], finalDeltaBuffer, currentPos);
                currentPos += CompressorUtilities.GetBytesForType(deltaTypes[i]);
            }

            // Assigner les résultats
            compData.MainDatas = finalDeltaBuffer;
            compData.CountDatas = countValues;
        }

        /// <summary>
        /// Compte le nombre de groupes distincts
        /// </summary>
        private static int CountGroups(Span<double> values)
        {
            int groupCount = 1; // Au moins un groupe
            double currentValue = values[0];
            byte count = 1;

            for (int i = 1; i < values.Length; i++)
            {
                if (Math.Abs(values[i] - currentValue) < 1e-10 && count < 255)
                {
                    count++;
                }
                else
                {
                    groupCount++;
                    currentValue = values[i];
                    count = 1;
                }
            }

            return groupCount;
        }

        /// <summary>
        /// Remplit les arrays avec les deltas, types et compteurs
        /// </summary>
        private static void FillArrays(Span<double> values, double[] deltaValues, Type[] deltaTypes, byte[] countValues)
        {
            double currentValue = values[0];
            byte count = 1;
            int groupIndex = 0;

            for (int i = 1; i < values.Length; i++)
            {
                if (Math.Abs(values[i] - currentValue) < 1e-10 && count < 255)
                {
                    count++;
                }
                else
                {
                    // Enregistrer le groupe terminé
                    deltaValues[groupIndex] = currentValue;
                    deltaTypes[groupIndex] = CompressorUtilities.GetTypeForValue(currentValue);
                    countValues[groupIndex] = count;
                    groupIndex++;

                    // Nouveau groupe
                    currentValue = values[i];
                    count = 1;
                }
            }

            // Enregistrer le dernier groupe
            deltaValues[groupIndex] = currentValue;
            deltaTypes[groupIndex] = CompressorUtilities.GetTypeForValue(currentValue);
            countValues[groupIndex] = count;
        }

    }
}
