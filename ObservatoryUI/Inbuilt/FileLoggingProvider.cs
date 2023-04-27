using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ObservatoryUI.Inbuilt
{
    [ProviderAlias("Debug")]
    public class FileLoggerProvider : ILoggerProvider
    {
        /// <inheritdoc />
        public ILogger CreateLogger(string name)
        {
            return new FileLogger(name);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
