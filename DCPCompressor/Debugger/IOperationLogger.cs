using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCPCompressor.Debugger
{
    /// <summary>
    /// Interface pour les stratégies de logging
    /// </summary>
    public interface IOperationLogger
    {
        (T result, string log) Execute<T>(string operationName, Func<T> operation);
        string Execute(string operationName, Action operation);
    }
}
