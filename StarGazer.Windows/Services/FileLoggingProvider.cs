using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;

namespace StarGazer.UI.Services
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : ILoggerProvider
    {
        Action<FileLoggerOptions> _config;
        
        public FileLoggerProvider(Action<FileLoggerOptions> config)
        {
            _config = config;
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string name)
        {
            return new FileLogger(options => {
                options.Name = name;
                _config?.Invoke(options);
            });
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }

    public static class FileLoggingProviderExtensions
    {
        public static ILoggingBuilder AddFileLogging(this ILoggingBuilder builder, Action<FileLoggerOptions> config)
        {
            builder.AddProvider(new FileLoggerProvider(config));

            return builder;
        }

        public static ILoggingBuilder AddDebugLogging(this ILoggingBuilder builder)
        {
            builder.AddProvider(new DebugLoggerProvider());

            return builder;
        }
    }
}
