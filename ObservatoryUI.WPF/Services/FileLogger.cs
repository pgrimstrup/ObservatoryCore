using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ObservatoryUI.WPF.Services
{
    internal class FileLogger : ILogger
    {
        readonly string _name;

        public FileLogger(string name)
        {
            _name = name;
        }

        public IDisposable? BeginScope<TState>(TState state) 
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            
        }
    }
}
