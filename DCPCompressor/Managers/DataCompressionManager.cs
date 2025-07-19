
using DCPCompressor.AbstractDataInformations;
using DCPCompressor.AbstractDatas;
using DCPCompressor.Compressors;
using DCPCompressor.Debugger;
using DCPCompressor.Models;
using DCPCompressor.Tools;
using OptimizedProjectHandler.Datas.Tools;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OptimizedProjectHandler.Datas.Managers
{
    /// <summary>
    /// Façade optimisée pour les services d'optimisation de types de données
    /// Utilise ReadOnlySpan et méthodes statiques pour maximiser les performances
    /// </summary>
    public static class DataCompressionManager
    {
        /// <summary>
        /// Point d'entrée pour la compression avec List
        /// Convertit la liste en tableau pour le traitement
        /// </summary>
        public static ICompressedData CompressDatas(List<double> datasToOptimize, int precision = 0)
        {
            // Convertir la liste en tableau une seule fois
            if (datasToOptimize == null || datasToOptimize.Count == 0)
                return HandleNullDatas(0);

            double[] dataArray = datasToOptimize.ToArray();
            return CompressDatas(dataArray, precision);
        }

        /// <summary>
        /// Version principale pour la compression avec tableau en entrée
        /// </summary>
        public static ICompressedData CompressDatas(Memory<double> dataSpan, int precision = 0)
        {
            //On va réduire la charge mémoire. aujourd'hui je m'embete a fabriquer un double[] sans aleur null le mettre en delta etc...
            //on va tout faire d'un coup plutot et tout dans l'array initial 

            if (dataSpan.Length == 0)
                return HandleNullDatas(0);

            CompressedData resultData = new CompressedData();

            // Préparation des données
            // Extraction des valeurs non nulles
            (var NaNArray, int NbNan) = NaNValues(dataSpan.Span);

            // Si le tableau est vide
            if (NbNan == dataSpan.Length)
            {
                return HandleNullDatas(NbNan);
            }

            // Vérification si la première valeur est nulle
            resultData.CompressedDataInfos.StartingValueBegin = NaNArray[0] ? false : true;

            // Enregistrement des statistiques de base
            resultData.DataRangeInfos.NbNulls = NbNan;
            resultData.DataRangeInfos.HasNull = NbNan > 0 ? true : false;
            resultData.DataRangeInfos.NbValues = dataSpan.Length - NbNan;
            resultData.DataRangeInfos.Length = dataSpan.Length;


            // Analyse des données originales avec ReadOnlySpan

            ArrayMetrics startingMetrics = ArrayAnalyzer.Analyze(dataSpan.Span);

            resultData.DataRangeInfos.Min = startingMetrics.MinValue;
            resultData.DataRangeInfos.Max = startingMetrics.MaxValue;

            // Conversion en deltas avec ReadOnlySpan
            //DeltaConverter.FindStartingValue(dataSpan.Span, resultData.CompressedDataInfos);
            DeltaConverter.CreateDeltasInPlace(dataSpan, resultData.CompressedDataInfos);

            // Recherche et application des facteurs d'échelle

            (sbyte scaleFactor, sbyte comaFactor) = ArrayScaler.FindFactors(dataSpan.Span);

            resultData.CompressedDataInfos.ScaleFactor = scaleFactor;
            resultData.CompressedDataInfos.ComaFactor = comaFactor;


            ArrayScaler.ApplyFactors(dataSpan.Span, scaleFactor, comaFactor);

            // Analyse et sélection du type optimal
            ArrayMetrics metrics = ArrayAnalyzer.Analyze(dataSpan.Span);
            Type dataType = TypeSelector.DetermineOptimalType(in metrics);
            resultData.CompressedDataInfos.DataType = dataType;

            // Gestion des valeurs nulles
            NullValueHandler.InjectNullValue(dataSpan.Span, resultData.CompressedDataInfos);


            // Sélection du compresseur optimal
            (CompressionEnums compressionType, CompressionTypeEnums compressionTypeEnum) =
                CompressionComparator.Compare(dataSpan.Span, resultData.CompressedDataInfos.DataType, dataSpan.Length / 10, 0.8);

            resultData.CompressedDataInfos.CompressionType = compressionType;
            resultData.CompressedDataInfos.CompressionTypeEnum = compressionTypeEnum;
            //Forcage a fixe pour l'insant
            resultData.CompressedDataInfos.CompressionTypeEnum = CompressionTypeEnums.Fixe;

            // Compression avec la méthode adaptée
            CompressWithOptimalMethod(dataSpan, resultData);

            return resultData;
        }

        /// <summary>
        /// Méthode d'aide pour compresser avec la méthode optimale identifiée
        /// </summary>
        private static void CompressWithOptimalMethod(Memory<double> finalDoubles, CompressedData resultData)
        {
            if (resultData.CompressedDataInfos.CompressionType == CompressionEnums.DeltaCount)
            {
                if (resultData.CompressedDataInfos.CompressionTypeEnum == CompressionTypeEnums.Fixe)
                {
                    FixeCompressor.FixedLengthCompressDeltaCount(finalDoubles.Span, resultData);
                }
                else
                {
                    VariableCompressor.VariableLengthCompressDeltaCount(finalDoubles.Span, resultData);
                }
            }
            else
            {
                if (resultData.CompressedDataInfos.CompressionTypeEnum == CompressionTypeEnums.Fixe)
                {
                    FixeCompressor.FixedLengthCompressDelta(finalDoubles, resultData);
                }
                else
                {
                    VariableCompressor.VariableLengthCompressDelta(finalDoubles.Span, resultData);
                }
            }
        }


        /// <summary>
        /// Compte le nombre de valeurs NaN dans un ReadOnlySpan<double>
        /// </summary>
        private static (BitArray List,int NbNull) NaNValues(ReadOnlySpan<double> values)
        {
            int count = 0;
            BitArray NewBitArray = new BitArray(values.Length,false);

            for (int i = 0; i < values.Length; i++)
            {
                if (double.IsNaN(values[i]))
                {
                    NewBitArray[i] = true;
                    count++;
                }
            }
            return (NewBitArray,count);
        }

        /// <summary>
        /// Gère le cas spécial des tableaux entièrement composés de NaN
        /// </summary>
        private static ICompressedData HandleNullDatas(int valuesNb)
        {
            DataRangeInfo dataRangeInfo = new DataRangeInfo()
            {
                Max = 0,
                Min = 0,
                NbNulls = valuesNb,
                NbValues = 0,
                Length = valuesNb,
            };

            int nbMainValues = (valuesNb / 255);
            int restValues = (valuesNb % 255);
            int total = nbMainValues;

            if (restValues != 0)
            {
                total += 1;
            }

            var mainArray = new byte[total];
            var countArray = new byte[total];

            for (int i = 0; i < total; i++)
            {
                mainArray[i] = 255;

                if (i < total - 1)
                {
                    countArray[i] = 255;
                }
                else
                {
                    countArray[i] = (byte)restValues;
                }
            }

            CompressedDataInfo compressedDataInfo = new CompressedDataInfo()
            {
                ComaFactor = 0,
                ScaleFactor = 1,
                NullValue = byte.MaxValue,
                CompressionType = CompressionEnums.DeltaCount,
                NumberOfValues = total,
                DataType = typeof(byte),
                CompressionTypeEnum = CompressionTypeEnums.Fixe,
            };

            return new CompressedData()
            {
                CompressedDataInfos = compressedDataInfo,
                CountDatas = countArray,
                MainDatas = mainArray,
                DataRangeInfos = dataRangeInfo,
            };
        }
    }
}
