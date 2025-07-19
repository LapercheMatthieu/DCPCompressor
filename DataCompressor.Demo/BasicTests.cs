using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DCPCompressor.Demo
{
    public class BasicTests
    {
        public void Launch()
        {
            double nombre = 1.25366534;
            var compteur = GetDecimalPlaces(nombre);

            Debug.WriteLine(compteur); 
        }

        public static int GetDecimalPlaces(double value)
        {
            // Si c'est NaN ou infini, retourner 0
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0;

            // Si c'est un entier, retourner 0 rapidement
            if (value == Math.Truncate(value))
                return 0;

            // Cette méthode utilise une approche par chaîne mais optimisée
            string str = value.ToString("G17", CultureInfo.InvariantCulture);

            int dotIndex = str.IndexOf('.');
            if (dotIndex < 0)
                return 0;

            // Vérifier s'il y a une notation scientifique
            int eIndex = str.IndexOfAny(new[] { 'E', 'e' });

            if (eIndex > 0)
            {
                // Extraire la partie décimale et l'exposant
                int mantissaDecimals = eIndex - dotIndex - 1;
                int exponent = int.Parse(str.Substring(eIndex + 1));

                if (exponent < 0)
                {
                    // Exposant négatif augmente les décimales
                    return mantissaDecimals - exponent;
                }
                else if (exponent < mantissaDecimals)
                {
                    // Exposant positif peut réduire les décimales
                    return mantissaDecimals - exponent;
                }
                else
                {
                    return 0;
                }
            }

            // Éliminer les zéros finaux
            int length = str.Length;
            while (length > dotIndex + 1 && str[length - 1] == '0')
            {
                length--;
            }

            return length - dotIndex - 1;
        }
    }
}
