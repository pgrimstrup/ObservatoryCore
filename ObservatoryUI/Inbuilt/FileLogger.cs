using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ObservatoryUI.Inbuilt
{
    internal class FileLogger : ILogger
    {
        public FileLogger(string name)
        {

        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return new FileLoggerScope<TState>();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            
        }
    }

    internal class FileLoggerScope<T> : IDisposable
        where T : notnull
    {
        public void Dispose()
        {

        }
    }
}
