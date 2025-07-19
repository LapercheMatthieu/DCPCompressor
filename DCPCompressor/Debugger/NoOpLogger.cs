using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Debugger
{
// <summary>
    /// Logger qui ne fait rien, utilisé en mode production
    /// </summary>
    public class NoOpLogger : IOperationLogger
    {
        public (T result, string log) Execute<T>(string operationName, Func<T> operation)
        {
            return (operation(), string.Empty);
        }

        public string Execute(string operationName, Action operation)
        {
            operation();
            return string.Empty;
        }
    }
}
