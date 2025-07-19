using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Demo
{
    public static class BuildCompressedFile
    {

        public static bool WriteBinaryFile(string filePath, byte[] data)
        {
            try
            {
                // Assurer que le répertoire existe
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Écrire les données dans le fichier
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fileStream.Write(data, 0, data.Length);
                    fileStream.Flush();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'écriture du fichier binaire : {ex.Message}");
                return false;
            }
        }
    }
}
