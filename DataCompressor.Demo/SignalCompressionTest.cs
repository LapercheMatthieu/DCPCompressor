using DCPCompressor.AbstractDataInformations;
using DCPCompressor.Compressors;
using DCPCompressor.Debugger;
using DCPCompressor.Managers;
using DCPCompressor.Tools;
using K4os.Compression.LZ4;
using Mechatro.WPF.AcquisitionDatas.TestingSet.DataBuilding;
using OptimizedProjectHandler.Datas.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Demo
{
    public class SignalCompressionTests
    {
        // Structure pour stocker les métriques
        private class CompressionMetrics
        {
            public double OriginalSizeMB { get; set; }
            public double CompressedSizeMB { get; set; }
            public double CompressionRatio { get; set; }
            public double CompressionTimeMs { get; set; }
            public double DecompressionTimeMs { get; set; }
            public long CompressionMemoryMB { get; set; }
            public long DecompressionMemoryMB { get; set; }
            public bool IsIdentical { get; set; }
            public string DataType { get; set; }
            public string CompressionType { get; set; }
        }

        // Exécuter les tests de compression/décompression
        public string RunCompressionTests()
        {
            StringBuilder logOutput = new StringBuilder();
            logOutput.AppendLine("=== Tests de Compression/Décompression ===\n");

            // Tailles de signaux à tester
            int[] signalSizes = { 10_000_000, 100_000_000};
            var Builder = new SensorBuilder(42);

            foreach (int size in signalSizes)
            {
                logOutput.AppendLine($"\n--- Test avec signal de taille {size:N0} ---");

                // Générer différents types de signaux
                var signals = new Dictionary<string, double[]>
                {
                    {"GenerateThermocoupleData", Builder.GenerateThermocoupleData(size)},
                    {"GenerateAccelerometerData", Builder.GenerateAccelerometerData(size)},
                    {"GeneratePressureData", Builder.GeneratePressureData(size)},
                    {"GenerateCurrentSensorData", Builder.GenerateCurrentSensorData(size)},
                    {"GenerateEncoderData", Builder.GenerateEncoderData(size)},
                    {"GenerateTorqueSensorData", Builder.GenerateTorqueSensorData(size)},
                };

                foreach (var signalEntry in signals)
                {
                    logOutput.AppendLine($"\n{signalEntry.Key}");

                    var metrics = TestSignalCompression(signalEntry.Value);

                    // Affichage des résultats
                    logOutput.AppendLine($"  Taille originale: {metrics.OriginalSizeMB:F2} MB");
                    logOutput.AppendLine($"  Taille compressée: {metrics.CompressedSizeMB:F2} MB");
                    logOutput.AppendLine($"  Type de données: {metrics.DataType}");
                    logOutput.AppendLine($"  Type de compression: {metrics.CompressionType}");
                    logOutput.AppendLine($"  Ratio de compression: {metrics.CompressionRatio:F1}:1");
                    logOutput.AppendLine($"  Taux de compression: {(1 - 1 / metrics.CompressionRatio) * 100:F1}%");
                    logOutput.AppendLine($"  Temps de compression: {metrics.CompressionTimeMs:F1} ms");
                    logOutput.AppendLine($"  Temps de décompression: {metrics.DecompressionTimeMs:F1} ms");
                    logOutput.AppendLine($"  Débit compression: {metrics.OriginalSizeMB / (metrics.CompressionTimeMs / 1000):F1} MB/s");
                    logOutput.AppendLine($"  Débit décompression: {metrics.OriginalSizeMB / (metrics.DecompressionTimeMs / 1000):F1} MB/s");
                    logOutput.AppendLine($"  Mémoire utilisée (compression): {metrics.CompressionMemoryMB} MB");
                    logOutput.AppendLine($"  Mémoire utilisée (décompression): {metrics.DecompressionMemoryMB} MB");
                    logOutput.AppendLine($"  Reconstruction identique: {metrics.IsIdentical}");
                    logOutput.AppendLine(new string('-', 60));
                }
            }

            logOutput.AppendLine("\nTests terminés.");
            return logOutput.ToString();
        }

        private CompressionMetrics TestSignalCompression(double[] signal)
        {
            var metrics = new CompressionMetrics();

            // Taille originale
            metrics.OriginalSizeMB = signal.Length * sizeof(double) / (1024.0 * 1024.0);

            // Copie pour vérification
            double[] originalCopy = new double[signal.Length];
            Array.Copy(signal, originalCopy, signal.Length);

            // Chronomètre pour la compression
            var stopwatch = Stopwatch.StartNew();

            // Mesure mémoire avant compression
            long memBeforeCompression = GetMemoryUsage();

            // COMPRESSION
            var compressionResult = DataCompressionManager.CompressDatas(signal);

            stopwatch.Stop();
            metrics.CompressionTimeMs = stopwatch.Elapsed.TotalMilliseconds;

            // Mesure mémoire après compression
            long memAfterCompression = GetMemoryUsage();
            metrics.CompressionMemoryMB = (memAfterCompression - memBeforeCompression) / (1024 * 1024);

            // Calcul de la taille compressée
            long compressedBytes = compressionResult.MainDatas.Length;
            if (compressionResult.CountDatas != null)
            {
                compressedBytes += compressionResult.CountDatas.Length;
            }
            metrics.CompressedSizeMB = compressedBytes / (1024.0 * 1024.0);

            // Ratio de compression
            metrics.CompressionRatio = (signal.Length * sizeof(double)) / (double)compressedBytes;

            // Type de compression
            metrics.DataType = compressionResult.CompressedDataInfos.DataType.ToString();
            metrics.CompressionType = compressionResult.CompressedDataInfos.CompressionType.ToString();

            // Chronomètre pour la décompression
            stopwatch.Restart();

            // Mesure mémoire avant décompression
            long memBeforeDecompression = GetMemoryUsage();

            // DÉCOMPRESSION
            var rebuiltSignal = DataDecompressionManager.DecompressDatas(compressionResult);

            stopwatch.Stop();
            metrics.DecompressionTimeMs = stopwatch.Elapsed.TotalMilliseconds;

            // Mesure mémoire après décompression
            long memAfterDecompression = GetMemoryUsage();
            metrics.DecompressionMemoryMB = (memAfterDecompression - memBeforeDecompression) / (1024 * 1024);

            // Vérification de l'identité
            metrics.IsIdentical = CompareDatas(originalCopy, rebuiltSignal);

            return metrics;
        }

        private long GetMemoryUsage()
        {
            // Force un garbage collection pour avoir une mesure plus précise
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return GC.GetTotalMemory(false);
        }

        private bool CompareDatas(double[] InitialDatas, double[] FinalDatas)
        {
            if (InitialDatas.Length != FinalDatas.Length)
                return false;

            for (int i = 0; i < InitialDatas.Length; i++)
            {
                if (InitialDatas[i] != FinalDatas[i] &&
                    !(double.IsNaN(InitialDatas[i]) && double.IsNaN(FinalDatas[i])))
                {
                    // Debug : afficher la première différence
                    if (Math.Abs(InitialDatas[i] - FinalDatas[i]) > 1e-10)
                    {
                        Console.WriteLine($"Différence à l'index {i}: {InitialDatas[i]} vs {FinalDatas[i]}");
                    }
                    return false;
                }
            }
            return true;
        }

        // Nouvelle méthode principale qui retourne les résultats
        public static string TestMain()
        {
            var tests = new SignalCompressionTests();
            return tests.RunCompressionTests();
        }
    }
}