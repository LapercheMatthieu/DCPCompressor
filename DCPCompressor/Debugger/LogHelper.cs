using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Debugger
{
    public static class LogHelper
    {
        public static (T result, string log) LogOperationTime<T>(string operationName, Func<T> operation)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            T result = operation();

            stopwatch.Stop();
            string log = $"{operationName} a pris {stopwatch.ElapsedMilliseconds} ms";

            // On garde le log Debug pour la compatibilité
            Debug.WriteLine(log);

            return (result, log);
        }

        public static string LogOperationTime(string operationName, Action operation)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            operation();

            stopwatch.Stop();
            string log = $"{operationName} a pris {stopwatch.ElapsedMilliseconds} ms";

            // On garde le log Debug pour la compatibilité
            Debug.WriteLine(log);

            return log;
        }
    }


}
