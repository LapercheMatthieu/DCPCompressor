using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Debugger
{
    /// <summary>
    /// Logger qui utilise LogHelper pour mesurer les temps d'exécution
    /// </summary>
    public class PerformanceLogger : IOperationLogger
    {
        public (T result, string log) Execute<T>(string operationName, Func<T> operation)
        {
            return LogHelper.LogOperationTime(operationName, operation);
        }

        public string Execute(string operationName, Action operation)
        {
            return LogHelper.LogOperationTime(operationName, operation);
        }
    }
}
