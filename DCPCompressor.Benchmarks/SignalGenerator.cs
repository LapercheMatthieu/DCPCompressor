using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCompressor.Benchmarks
{
    /// <summary>
    /// Générateur de signaux optimisé
    /// </summary>
    public class SignalGenerator
    {
        private readonly Random _random;

        public SignalGenerator(int seed = 42)
        {
            _random = new Random(seed);
        }

        // Générateur de signaux sinusoïdaux avec valeurs NaN aléatoires
        public double[] GenerateSineWave(int size, double frequency = 1.0, double amplitude = 1.0, int numberOfDigits = 8,
                                  double phase = 0.0, double nanProbability = 0.1)
        {
            double[] signal = new double[size];
            for (int i = 0; i < size; i++)
            {
                // Introduire des valeurs NaN aléatoirement
                if (_random.NextDouble() < nanProbability)
                {
                    signal[i] = double.NaN;
                }
                else
                {
                    double x = (double)i / size;
                    signal[i] = Math.Round(amplitude * Math.Sin(2 * Math.PI * frequency * x + phase), numberOfDigits);
                }
            }
            return signal;
        }

        // Générateur de signaux sinusoïdaux sans valeurs NaN
        public double[] GenerateSineWaveNotNull(int size, double frequency = 1.0, double amplitude = 1.0, int numberOfDigits = 8,
                                        double phase = 0.0)
        {
            double[] signal = new double[size];
            for (int i = 0; i < size; i++)
            {
                double x = (double)i / size;
                signal[i] = Math.Round(amplitude * Math.Sin(2 * Math.PI * frequency * x + phase), numberOfDigits);
            }
            return signal;
        }

        // Générateur de signaux en dents de scie avec valeurs NaN aléatoires
        public double[] GenerateSawtoothWave(int size, double amplitude = 1.0, int numberOfDigits = 4, double nanProbability = 0.1)
        {
            double[] signal = new double[size];
            for (int i = 0; i < size; i++)
            {
                // Introduire des valeurs NaN aléatoirement
                if (_random.NextDouble() < nanProbability)
                {
                    signal[i] = double.NaN;
                }
                else
                {
                    double x = (double)i / size;
                    signal[i] = Math.Round(amplitude * (2 * (x - Math.Floor(x + 0.5))), numberOfDigits);
                }
            }
            return signal;
        }

        // Générateur de signaux carrés avec valeurs NaN aléatoires
        public double[] GenerateSquareWave(int size, double frequency = 1.0, double amplitude = 1.0, int numberOfDigits = 4,
                                     double nanProbability = 0.1)
        {
            double[] signal = new double[size];
            for (int i = 0; i < size; i++)
            {
                // Introduire des valeurs NaN aléatoirement
                if (_random.NextDouble() < nanProbability)
                {
                    signal[i] = double.NaN;
                }
                else
                {
                    double x = (double)i / size;
                    signal[i] = Math.Round(amplitude * Math.Sign(Math.Sin(2 * Math.PI * frequency * x)), numberOfDigits);
                }
            }
            return signal;
        }

        // Générateur de bruit blanc avec valeurs NaN aléatoires
        public double[] GenerateWhiteNoise(int size, double amplitude = 1.0, int numberOfDigits = 4, double nanProbability = 0.1)
        {
            double[] signal = new double[size];
            for (int i = 0; i < size; i++)
            {
                // Introduire des valeurs NaN aléatoirement
                if (_random.NextDouble() < nanProbability)
                {
                    signal[i] = double.NaN;
                }
                else
                {
                    signal[i] = Math.Round(amplitude * (2 * _random.NextDouble() - 1), numberOfDigits);
                }
            }
            return signal;
        }

        // Générateur de signal simple de test
        public double[] GenerateHandMade()
        {
            double[] signal = new double[4];
            signal[0] = 0.5;
            signal[1] = double.NaN;
            signal[2] = 0.6;
            signal[3] = 0.9;
            return signal;
        }

        // Générateur de signal composé avec valeurs NaN aléatoires
        public double[] GenerateCompositeSignal(int size, int numberOfDigits = 4, double nanProbability = 0.1)
        {
            double[] sine = GenerateSineWave(size, 2.0, 0.5, 4, 0.0, 0);
            double[] sawtooth = GenerateSawtoothWave(size, 0.3, 4, 0);
            double[] noise = GenerateWhiteNoise(size, 0.1, 4, 0);

            double[] composite = new double[size];
            for (int i = 0; i < size; i++)
            {
                // Additionner les signaux composants (s'ils ne sont pas NaN)
                double sum = 0;
                if (!double.IsNaN(sine[i])) sum += sine[i];
                if (!double.IsNaN(sawtooth[i])) sum += sawtooth[i];
                if (!double.IsNaN(noise[i])) sum += noise[i];

                // Introduire des valeurs NaN aléatoirement
                if (_random.NextDouble() < nanProbability)
                {
                    composite[i] = double.NaN;
                }
                else
                {
                    composite[i] = Math.Round(sum, numberOfDigits);
                }
            }

            return composite;
        }

        // Générateur de signal entièrement composé de NaN
        public double[] GenerateNullSignal(int size)
        {
            double[] nulls = new double[size];
            for (int i = 0; i < size; i++)
            {
                nulls[i] = double.NaN;
            }

            return nulls;
        }
    }
}
