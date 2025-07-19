using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using DataCompressor.AbstractDataInformations;
using DataCompressor.Benchmarks;
using DataCompressor.Compressors;
using DataCompressor.Managers;
using DataCompressor.Models;
using DataCompressor.Tools;
using OptimizedProjectHandler.Datas.Managers;
using OptimizedProjectHandler.Datas.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataCompressor.Demo
{
    [MemoryDiagnoser]
    [HardwareCounters]
    [RankColumn]
    [Config(typeof(BenchmarkConfiguration))]
    public class DetailedCompressionBenchmarks
    {
        // Configuration spécifique pour nos benchmarks
        private class BenchmarkConfiguration : ManualConfig
        {
            public BenchmarkConfiguration()
            {
                // Exécuter en mode Release uniquement
                AddJob(Job.MediumRun.WithWarmupCount(2).WithIterationCount(2));

                // Ajouter des diagnostics supplémentaires
                AddDiagnoser(MemoryDiagnoser.Default);
                AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig()));
            }
        }

        // Générateurs de signaux
        private readonly SignalGenerator _signalGenerator = new SignalGenerator(42); // Seed fixe

        // Variables pour les données de test
        private double[] _sineWave;
        private double[] _compositeSignal;

        // Variables intermédiaires pour les benchmarks d'étapes
        private double[] _nonNullValues;
        private double[] _deltas;
        private double _startingValue;
        private sbyte _scaleFactor;
        private sbyte _comaFactor;
        private ArrayMetrics _metrics;

        [Params(10_000, 10_000_000)] // Différentes tailles à tester
        public int SignalSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // Génération des signaux de test une seule fois
            _sineWave = _signalGenerator.GenerateSineWave(SignalSize);
            _compositeSignal = _signalGenerator.GenerateCompositeSignal(SignalSize);

            // Préparation des valeurs intermédiaires pour les benchmarks spécifiques
            PrepareIntermediateValues();
        }

        private void PrepareIntermediateValues()
        {
            /*// Extraction des valeurs non-NaN pour les tests suivants
            _nonNullValues = ArrayAnalyzer.ExtractNonNullValues(_sineWave.AsSpan(), 8);

            // Préparation des deltas
            var deltaResult = DeltaConverter.DeltasCreation(_nonNullValues.AsSpan());
            _deltas = deltaResult.Item1;
            _startingValue = deltaResult.Item2;

            // Calcul des facteurs d'échelle
            (_scaleFactor, _comaFactor) = ArrayScaler.FindFactors(_deltas.AsSpan());

            // Application des facteurs (pour les tests suivants)
            double[] scaledDeltas = _deltas.ToArray(); // Copie pour ne pas modifier l'original
            ArrayScaler.ApplyFactors(scaledDeltas.AsSpan(), _scaleFactor, _comaFactor);

            // Analyse des métriques
            _metrics = ArrayAnalyzer.Analyze(scaledDeltas.AsSpan());*/
        }

        #region Benchmarks des étapes individuelles

      /*  [Benchmark(Description = "1. Extraction des valeurs non-NaN")]
        public double[] BenchmarkExtractNonNullValues()
        {
            return ArrayAnalyzer.ExtractNonNullValues(_sineWave.AsSpan(), 8);
        }

        [Benchmark(Description = "2. Création des deltas")]
        public (double[], double) BenchmarkDeltasCreation()
        {
            return DeltaConverter.DeltasCreation(_nonNullValues.AsSpan());
        }

        [Benchmark(Description = "3. Recherche des facteurs d'échelle")]
        public (sbyte, sbyte) BenchmarkFindFactors()
        {
            return ArrayScaler.FindFactors(_deltas.AsSpan());
        }

        [Benchmark(Description = "4. Application des facteurs d'échelle")]
        public void BenchmarkApplyFactors()
        {
            // Copie pour ne pas modifier l'original dans le benchmark
            double[] scaledDeltas = _deltas.ToArray();
            ArrayScaler.ApplyFactors(scaledDeltas.AsSpan(), _scaleFactor, _comaFactor);
        }

        [Benchmark(Description = "5. Analyse des métriques")]
        public ArrayMetrics BenchmarkAnalyzeMetrics()
        {
            // Copie pour ne pas modifier l'original dans le benchmark
            double[] scaledDeltas = _deltas.ToArray();
            ArrayScaler.ApplyFactors(scaledDeltas.AsSpan(), _scaleFactor, _comaFactor);

            return ArrayAnalyzer.Analyze(scaledDeltas.AsSpan());
        }

        [Benchmark(Description = "6. Détermination du type optimal")]
        public Type BenchmarkDetermineOptimalType()
        {
            return TypeSelector.DetermineOptimalType(in _metrics);
        }

        [Benchmark(Description = "7. Gestion des valeurs nulles")]
        public double[] BenchmarkInjectNullValue()
        {
            // Copie pour ne pas modifier l'original dans le benchmark
            double[] scaledDeltas = _deltas.ToArray();
            ArrayScaler.ApplyFactors(scaledDeltas.AsSpan(), _scaleFactor, _comaFactor);

            var compressedInfo = new DataCompressor.Models.CompressedDataInfo
            {
                ScaleFactor = _scaleFactor,
                ComaFactor = _comaFactor,
                StartingValue = _startingValue,
                StartingValueBegin = true
            };

            return NullValueHandler.InjectNullValue(scaledDeltas.AsSpan(), _sineWave.AsSpan(), compressedInfo);
        }

        [Benchmark(Description = "8. Comparaison des méthodes de compression")]
        public (CompressionEnums, CompressionTypeEnums) BenchmarkCompressionComparison()
        {
            // Préparation des données comme dans le flux réel
            double[] scaledDeltas = _deltas.ToArray();
            ArrayScaler.ApplyFactors(scaledDeltas.AsSpan(), _scaleFactor, _comaFactor);

            var compressedInfo = new DataCompressor.Models.CompressedDataInfo
            {
                ScaleFactor = _scaleFactor,
                ComaFactor = _comaFactor,
                DataType = typeof(int) // Type arbitraire pour le test
            };

            var injectedValues = NullValueHandler.InjectNullValue(scaledDeltas.AsSpan(), _sineWave.AsSpan(), compressedInfo);

            return CompressionComparator.Compare(injectedValues, compressedInfo.DataType, injectedValues.Length / 10, 0.8);
        }

        [Benchmark(Description = "9. Création DeltaCount")]
        public (double[] deltasValues, byte[] Counts) BenchmarkDeltaCountCreation()
        {
            // Préparation des données comme dans le flux réel
            double[] scaledDeltas = _deltas.ToArray();
            ArrayScaler.ApplyFactors(scaledDeltas.AsSpan(), _scaleFactor, _comaFactor);

            var compressedInfo = new DataCompressor.Models.CompressedDataInfo
            {
                ScaleFactor = _scaleFactor,
                ComaFactor = _comaFactor
            };

            var injectedValues = NullValueHandler.InjectNullValue(scaledDeltas.AsSpan(), _sineWave.AsSpan(), compressedInfo);

            return DeltaConverter.DeltaCountCreation(injectedValues);
        }

        [Benchmark(Description = "10. Compression par Delta")]
        public byte[] BenchmarkDeltaCompression()
        {
            // Préparation des données comme dans le flux réel
            double[] scaledDeltas = _deltas.ToArray();
            ArrayScaler.ApplyFactors(scaledDeltas.AsSpan(), _scaleFactor, _comaFactor);

            var compressedInfo = new DataCompressor.Models.CompressedDataInfo
            {
                ScaleFactor = _scaleFactor,
                ComaFactor = _comaFactor,
                DataType = typeof(int) // Type arbitraire pour le test
            };

            var injectedValues = NullValueHandler.InjectNullValue(scaledDeltas.AsSpan(), _sineWave.AsSpan(), compressedInfo);

            var compressor = new DataCompressor.Compressors.DeltaCompressor();
            var compressedData = new DataCompressor.Models.CompressedData
            {
                CompressedDataInfos = compressedInfo
            };

            return compressor.Compress(injectedValues, compressedData);
        }

        [Benchmark(Description = "11. Compression par DeltaCount")]
        public (byte[] Main, byte[] Count) BenchmarkDeltaCountCompression()
        {
            // Préparation des données comme dans le flux réel
            double[] scaledDeltas = _deltas.ToArray();
            ArrayScaler.ApplyFactors(scaledDeltas.AsSpan(), _scaleFactor, _comaFactor);

            var compressedInfo = new DataCompressor.Models.CompressedDataInfo
            {
                ScaleFactor = _scaleFactor,
                ComaFactor = _comaFactor,
                DataType = typeof(int) // Type arbitraire pour le test
            };

            var injectedValues = NullValueHandler.InjectNullValue(scaledDeltas.AsSpan(), _sineWave.AsSpan(), compressedInfo);

            var deltaCounts = DeltaConverter.DeltaCountCreation(injectedValues);

            var compressor = new DataCompressor.Compressors.DeltaCountCompressor();
            var compressedData = new DataCompressor.Models.CompressedData
            {
                CompressedDataInfos = compressedInfo
            };

            return compressor.Compress(deltaCounts.deltasValues, deltaCounts.Counts, compressedData);
        }

        #endregion
      */
        #region Benchmarks complets pour comparer

        [Benchmark(Description = "Flux complet - Signal sinusoïdal")]
        public void FullProcessSineWave()
        {
            // Copier d'abord les données comme dans la démo
            double[] signalCopy = new double[_sineWave.Length];
            Array.Copy(_sineWave, signalCopy, _sineWave.Length);

            // Puis compresser
            var compressed = DataCompressionManager.CompressDatas(signalCopy);
        }
        /*
        [Benchmark(Description = "Flux complet - Signal composite")]
        public void FullProcessCompositeSignal()
        {
            var compressed = DataCompressionManager.CompressDatas(_compositeSignal);
        }*/

        #endregion
        #endregion
        // Point d'entrée
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<DetailedCompressionBenchmarks>();
            Console.WriteLine(summary);
        }
    }

}