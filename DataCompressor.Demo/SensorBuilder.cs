using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mechatro.WPF.AcquisitionDatas.TestingSet.DataBuilding
{
    public class SensorBuilder
    {
        private Random _random;

        public SensorBuilder(int randomSeed = 42)
        {
            _random = new Random(randomSeed);
        }


        #region Configuration de base pour tous les capteurs
        public class SensorConfig
        {
            public int BitDepth { get; set; } = 12;
            public double SamplingFrequency { get; set; } = 1000; // Hz
            public double NoiseLevel { get; set; } = 0.01; // % du signal
            public double MinValue { get; set; } = 0;
            public double MaxValue { get; set; } = 100;
            public double DriftRate { get; set; } = 0; // unités/heure
        }

        // Configuration spécifique thermocouple
        public class ThermocoupleConfig : SensorConfig
        {
            public double AmbientTemp { get; set; } = 20.0; // °C
            public double TempVariation { get; set; } = 5.0; // °C
            public double ThermalTimeConstant { get; set; } = 10.0; // secondes

            public ThermocoupleConfig()
            {
                BitDepth = 16;
                MinValue = -200;
                MaxValue = 1300;
                NoiseLevel = 0.1; // °C
                DriftRate = 0.5; // °C/heure
            }
        }

        // Configuration accéléromètre
        public class AccelerometerConfig : SensorConfig
        {
            public double[] VibratingFrequencies { get; set; } = { 50, 100, 150 }; // Hz
            public double[] Amplitudes { get; set; } = { 0.1, 0.05, 0.02 }; // g
            public double Range { get; set; } = 2.0; // ±g

            public AccelerometerConfig()
            {
                BitDepth = 12;
                MinValue = -2;
                MaxValue = 2;
                SamplingFrequency = 5000; // Hz typique pour vibrations
                NoiseLevel = 0.001; // g RMS
            }
        }

        // Configuration capteur de pression
        public class PressureConfig : SensorConfig
        {
            public double NominalPressure { get; set; } = 1.0; // bar
            public double PressureFluctuation { get; set; } = 0.1; // bar
            public double ProcessFrequency { get; set; } = 0.1; // Hz (process lent)

            public PressureConfig()
            {
                BitDepth = 16;
                MinValue = 0;
                MaxValue = 10; // bar
                NoiseLevel = 0.0001; // bar
                DriftRate = 0.01; // bar/heure
            }
        }
        // Configuration capteur de couple
        public class TorqueSensorConfig : SensorConfig
        {
            public double NominalTorque { get; set; } = 10.0; // Nm
            public double LoadVariation { get; set; } = 3.0; // Nm
            public double MotorSpeed { get; set; } = 1500; // RPM
            public int MotorPoles { get; set; } = 4; // Nombre de pôles
            public double RippleAmplitude { get; set; } = 0.05; // % du couple nominal

            public TorqueSensorConfig()
            {
                BitDepth = 12;
                MinValue = -50;
                MaxValue = 50; // Nm
                SamplingFrequency = 5000; // Hz
                NoiseLevel = 0.02; // Nm RMS
            }
        }
        // Configuration encodeur angulaire
        public class EncoderConfig : SensorConfig
        {
            public double InitialPosition { get; set; } = 0; // degrés
            public double NominalSpeed { get; set; } = 100; // RPM
            public double SpeedVariation { get; set; } = 10; // RPM
            public bool Bidirectional { get; set; } = false;
            public double Backlash { get; set; } = 0.05; // degrés

            public EncoderConfig()
            {
                BitDepth = 16; // Résolution fixe pour encodeur 16 bits
                MinValue = 0;
                MaxValue = 360; // degrés
                SamplingFrequency = 10000; // Hz
                NoiseLevel = 0.001; // degrés (très faible)
            }
        }

        // Configuration capteur de courant
        public class CurrentSensorConfig : SensorConfig
        {
            public double NominalCurrent { get; set; } = 10.0; // A
            public double LoadCurrent { get; set; } = 8.0; // A
            public double InrushMultiplier { get; set; } = 6.0; // x nominal au démarrage
            public double PwmFrequency { get; set; } = 20000; // Hz
            public double PowerFactor { get; set; } = 0.85;
            public double LineFrequency { get; set; } = 50; // Hz (50 ou 60)

            public CurrentSensorConfig()
            {
                BitDepth = 12;
                MinValue = -50;
                MaxValue = 50; // A
                SamplingFrequency = 50000; // Hz (pour capturer PWM)
                NoiseLevel = 0.05; // A RMS
            }
        }






        #endregion
        #region Génération thermocouple avec config
        public double[] GenerateThermocoupleData(int samples, ThermocoupleConfig config = null)
        {
            config ??= new ThermocoupleConfig();
            double[] data = new double[samples];

            double resolution = (config.MaxValue - config.MinValue) / Math.Pow(2, config.BitDepth);
            double timeStep = 1.0 / config.SamplingFrequency;
            double driftPerSample = config.DriftRate * timeStep / 3600.0;

            double currentTemp = config.AmbientTemp;
            double targetTemp = config.AmbientTemp;
            double drift = 0;

            for (int i = 0; i < samples; i++)
            {
                // Changement occasionnel de température cible
                if (_random.NextDouble() < 0.001)
                {
                    targetTemp = config.AmbientTemp + config.TempVariation * (2 * _random.NextDouble() - 1);
                }

                // Réponse du premier ordre (constante de temps thermique)
                double tau = config.ThermalTimeConstant;
                currentTemp += (targetTemp - currentTemp) * timeStep / tau;

                // Dérive
                drift += driftPerSample;

                // Bruit thermique
                double noise = config.NoiseLevel * GaussianNoise();

                // Valeur finale avec quantification
                double value = currentTemp + drift + noise;
                data[i] = Quantize(value, resolution, config.MinValue, config.MaxValue);
            }

            return data;
        }

        // Génération accéléromètre
        public double[] GenerateAccelerometerData(int samples, AccelerometerConfig config = null)
        {
            config ??= new AccelerometerConfig();
            double[] data = new double[samples];

            double resolution = (config.MaxValue - config.MinValue) / Math.Pow(2, config.BitDepth);
            double timeStep = 1.0 / config.SamplingFrequency;

            for (int i = 0; i < samples; i++)
            {
                double signal = 0;

                // Somme des composantes fréquentielles
                for (int j = 0; j < config.VibratingFrequencies.Length && j < config.Amplitudes.Length; j++)
                {
                    signal += config.Amplitudes[j] * Math.Sin(2 * Math.PI * config.VibratingFrequencies[j] * i * timeStep);
                }

                // Bruit de mesure (bruit blanc + bruit 1/f)
                double whiteNoise = config.NoiseLevel * GaussianNoise();
                double pinkNoise = config.NoiseLevel * 0.5 * GeneratePinkNoise(i);

                // Occasionnellement, un choc
                if (_random.NextDouble() < 0.0001)
                {
                    signal += (_random.NextDouble() - 0.5) * config.Range * 0.8;
                }

                double value = signal + whiteNoise + pinkNoise;
                data[i] = Quantize(value, resolution, config.MinValue, config.MaxValue);
            }

            return data;
        }

        // Génération capteur de pression
        public double[] GeneratePressureData(int samples, PressureConfig config = null)
        {
            config ??= new PressureConfig();
            double[] data = new double[samples];

            double resolution = (config.MaxValue - config.MinValue) / Math.Pow(2, config.BitDepth);
            double timeStep = 1.0 / config.SamplingFrequency;
            double driftPerSample = config.DriftRate * timeStep / 3600.0;

            double pressure = config.NominalPressure;
            double drift = 0;
            double processPhase = 0;

            for (int i = 0; i < samples; i++)
            {
                // Variation lente du process
                processPhase += 2 * Math.PI * config.ProcessFrequency * timeStep;
                double processVariation = config.PressureFluctuation * Math.Sin(processPhase);

                // Dérive du capteur
                drift += driftPerSample;

                // Bruit de mesure (principalement bruit 1/f pour capteurs de pression)
                double noise = config.NoiseLevel * (0.3 * GaussianNoise() + 0.7 * GeneratePinkNoise(i));

                // Perturbations occasionnelles (ouverture vanne, etc.)
                if (_random.NextDouble() < 0.0005)
                {
                    pressure += (_random.NextDouble() - 0.5) * config.PressureFluctuation * 2;
                }

                // Retour lent vers la pression nominale
                pressure += (config.NominalPressure - pressure) * 0.001;

                double value = pressure + processVariation + drift + noise;
                data[i] = Quantize(value, resolution, config.MinValue, config.MaxValue);
            }

            return data;
        }

        // Générateur de capteur générique personnalisable
        public double[] GenerateCustomSensorData(int samples, SensorConfig config,
            Func<int, double, double> signalGenerator)
        {
            double[] data = new double[samples];
            double resolution = (config.MaxValue - config.MinValue) / Math.Pow(2, config.BitDepth);
            double timeStep = 1.0 / config.SamplingFrequency;

            for (int i = 0; i < samples; i++)
            {
                double signal = signalGenerator(i, timeStep);
                double noise = config.NoiseLevel * GaussianNoise();
                double value = signal + noise;
                data[i] = Quantize(value, resolution, config.MinValue, config.MaxValue);
            }

            return data;
        }

        // Méthodes utilitaires
        // Remplacez la méthode Quantize par celle-ci
        private double Quantize(double value, double resolution, double min, double max)
        {
            value = Math.Max(min, Math.Min(max, value)); // Clamp

            // Quantification selon la résolution
            long steps = (long)Math.Round(value / resolution);
            value = steps * resolution;

            // IMPORTANT : Arrondir selon la précision réelle du capteur
            // Déterminer le nombre de décimales significatives
            int decimals = GetSignificantDecimals(resolution);

            return Math.Round(value, decimals);
        }

        private int GetSignificantDecimals(double resolution)
        {
            // Trouver combien de décimales sont significatives
            if (resolution >= 1) return 0;
            if (resolution >= 0.1) return 1;
            if (resolution >= 0.01) return 2;
            if (resolution >= 0.001) return 3;
            if (resolution >= 0.0001) return 4;
            return 5; // Maximum raisonnable
        }


        private double GaussianNoise()
        {
            // Box-Muller transform
            double u1 = 1.0 - _random.NextDouble();
            double u2 = 1.0 - _random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        private double _pinkNoiseState = 0;
        private double GeneratePinkNoise(int index)
        {
            // Approximation simple du bruit 1/f
            _pinkNoiseState = 0.99 * _pinkNoiseState + GaussianNoise();
            return _pinkNoiseState;
        }

        // Méthode pour obtenir des infos sur le signal généré
        public string GetSignalInfo(double[] data, SensorConfig config)
        {
            var stats = CalculateStatistics(data);
            var snr = 20 * Math.Log10(stats.StdDev / config.NoiseLevel);

            return $"Signal Info:\n" +
                   $"  Samples: {data.Length:N0}\n" +
                   $"  Bit depth: {config.BitDepth} bits\n" +
                   $"  Sampling: {config.SamplingFrequency} Hz\n" +
                   $"  Range: [{config.MinValue}, {config.MaxValue}]\n" +
                   $"  Mean: {stats.Mean:F3}\n" +
                   $"  Std Dev: {stats.StdDev:F3}\n" +
                   $"  Min/Max: [{stats.Min:F3}, {stats.Max:F3}]\n" +
                   $"  SNR: {snr:F1} dB";
        }

        private (double Mean, double StdDev, double Min, double Max) CalculateStatistics(double[] data)
        {
            double mean = data.Average();
            double stdDev = Math.Sqrt(data.Select(x => Math.Pow(x - mean, 2)).Average());
            return (mean, stdDev, data.Min(), data.Max());
        }

        // Génération capteur de couple
        public double[] GenerateTorqueSensorData(int samples, TorqueSensorConfig config = null)
        {
            config ??= new TorqueSensorConfig();
            double[] data = new double[samples];

            double resolution = (config.MaxValue - config.MinValue) / Math.Pow(2, config.BitDepth);
            double timeStep = 1.0 / config.SamplingFrequency;

            double loadPhase = 0;
            double motorPhase = 0;
            double currentTorque = config.NominalTorque;

            for (int i = 0; i < samples; i++)
            {
                // Vitesse de rotation en rad/s
                double motorSpeedRad = config.MotorSpeed * 2 * Math.PI / 60;
                motorPhase += motorSpeedRad * timeStep;

                // Variation de charge (lente)
                loadPhase += 2 * Math.PI * 0.1 * timeStep; // 0.1 Hz
                double loadTorque = config.LoadVariation * Math.Sin(loadPhase);

                // Ripple du couple dû aux pôles moteur
                double rippleFreq = config.MotorSpeed * config.MotorPoles / 60;
                double ripple = config.NominalTorque * config.RippleAmplitude *
                               Math.Sin(2 * Math.PI * rippleFreq * i * timeStep);

                // Harmoniques dues aux imperfections mécaniques
                double harmonics = 0.02 * config.NominalTorque *
                                  (Math.Sin(4 * motorPhase) + 0.5 * Math.Sin(8 * motorPhase));

                // Variations rapides dues aux vibrations
                double vibration = 0.01 * config.NominalTorque * Math.Sin(2 * Math.PI * 300 * i * timeStep);

                // Bruit de mesure
                double noise = config.NoiseLevel * GaussianNoise();

                // Événements occasionnels (chocs, accrochages)
                if (_random.NextDouble() < 0.0001)
                {
                    currentTorque += (_random.NextDouble() - 0.5) * config.LoadVariation * 2;
                }

                // Retour progressif vers le couple nominal + charge
                currentTorque += (config.NominalTorque + loadTorque - currentTorque) * 0.01;

                double value = currentTorque + ripple + harmonics + vibration + noise;
                data[i] = Quantize(value, resolution, config.MinValue, config.MaxValue);
            }

            return data;
        }

        // Génération encodeur angulaire
        public double[] GenerateEncoderData(int samples, EncoderConfig config = null)
        {
            config ??= new EncoderConfig();
            double[] data = new double[samples];

            // Résolution fixe pour 16 bits
            double resolution = 360.0 / 65536.0; // ~0.0055 degrés
            double timeStep = 1.0 / config.SamplingFrequency;

            double position = config.InitialPosition;
            double speed = config.NominalSpeed;
            double speedPhase = 0;
            int direction = 1;
            double backlashState = 0;

            for (int i = 0; i < samples; i++)
            {
                // Variation de vitesse
                speedPhase += 2 * Math.PI * 0.5 * timeStep; // 0.5 Hz
                double speedVar = config.SpeedVariation * Math.Sin(speedPhase);
                double currentSpeed = speed + speedVar;

                // Changement de direction occasionnel
                if (config.Bidirectional && _random.NextDouble() < 0.0001)
                {
                    direction *= -1;
                    backlashState = config.Backlash; // Jeu mécanique
                }

                // Décrément du backlash
                if (backlashState > 0)
                {
                    backlashState -= Math.Abs(currentSpeed) * 360 * timeStep / 60;
                    if (backlashState < 0) backlashState = 0;
                }

                // Mise à jour de la position (seulement si pas de backlash)
                if (backlashState == 0)
                {
                    position += direction * currentSpeed * 360 * timeStep / 60;
                }

                // Gestion du wrap-around 0-360
                while (position < 0) position += 360;
                while (position >= 360) position -= 360;

                // Erreur de quantification minime
                double quantError = resolution * 0.1 * GaussianNoise();

                // Bruit électrique très faible
                double noise = config.NoiseLevel * GaussianNoise();

                double value = position + quantError + noise;

                // Quantification 16 bits
                value = Math.Round(value,4);

                // Assurer le range 0-360d
                if (value < 0) value += 360;
                if (value >= 360) value -= 360;

                data[i] = value;
            }

            return data;
        }

        // Génération capteur de courant
        public double[] GenerateCurrentSensorData(int samples, CurrentSensorConfig config = null)
        {
            config ??= new CurrentSensorConfig();
            double[] data = new double[samples];

            double resolution = (config.MaxValue - config.MinValue) / Math.Pow(2, config.BitDepth);
            double timeStep = 1.0 / config.SamplingFrequency;

            bool motorRunning = false;
            double startupTime = 0;
            double fundamentalPhase = 0;
            double loadPhase = 0;

            for (int i = 0; i < samples; i++)
            {
                double current = 0;

                // Démarrage du moteur
                if (!motorRunning && _random.NextDouble() < 0.0001)
                {
                    motorRunning = true;
                    startupTime = 0;
                }

                // Arrêt du moteur
                if (motorRunning && _random.NextDouble() < 0.00005)
                {
                    motorRunning = false;
                }

                if (motorRunning)
                {
                    startupTime += timeStep;

                    // Courant d'appel au démarrage (décroissance exponentielle)
                    double inrushDecay = Math.Exp(-startupTime / 0.1); // Constante de temps 100ms
                    double inrushCurrent = config.NominalCurrent * (config.InrushMultiplier - 1) * inrushDecay;

                    // Fondamentale du courant AC
                    fundamentalPhase += 2 * Math.PI * config.LineFrequency * timeStep;
                    double fundamental = (config.LoadCurrent + inrushCurrent) * Math.Sin(fundamentalPhase);

                    // Harmoniques dues au découpage PWM et non-linéarités
                    double harmonics = 0;
                    harmonics += 0.05 * config.LoadCurrent * Math.Sin(3 * fundamentalPhase); // 3ème harmonique
                    harmonics += 0.03 * config.LoadCurrent * Math.Sin(5 * fundamentalPhase); // 5ème harmonique
                    harmonics += 0.02 * config.LoadCurrent * Math.Sin(7 * fundamentalPhase); // 7ème harmonique

                    // Ripple haute fréquence du PWM
                    double pwmRipple = 0.02 * config.LoadCurrent *
                                      Math.Sin(2 * Math.PI * config.PwmFrequency * i * timeStep);

                    // Variation de charge
                    loadPhase += 2 * Math.PI * 0.2 * timeStep; // 0.2 Hz
                    double loadVariation = 0.2 * config.LoadCurrent * Math.Sin(loadPhase);

                    current = fundamental + harmonics + pwmRipple + loadVariation;

                    // Déphasage dû au facteur de puissance
                    current *= config.PowerFactor;
                }

                // Bruit de mesure (incluant interférences EMI)
                double noise = config.NoiseLevel * GaussianNoise();
                double emiNoise = 0.01 * Math.Sin(2 * Math.PI * 1000 * i * timeStep); // EMI à 1kHz

                double value = current + noise + emiNoise;
                data[i] = Quantize(value, resolution, config.MinValue, config.MaxValue);
            }

            return data;
        }
        #endregion
    }
}