using BuildingBlocks.Messaging.KafkaLogger.Configs;
using BuildingBlocks.Messaging.KafkaLogger.LogQueue;
using MassTransit.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger.Logger
{
    public sealed class KafkaLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly ConcurrentDictionary<string, KafkaLogger> _loggers = new();

        internal KafkaSettings Options { get; }
        internal ILogQueue LogQueue { get; }
        private readonly IConfiguration _config;

        public KafkaLoggerProvider(IOptions<KafkaSettings> options, ILogQueue logQueue, IConfiguration config)
        {
            Options = options.Value;
            LogQueue = logQueue;
            _config = config;
            if (string.IsNullOrEmpty(Options.ServiceName))
            {
                Options.ServiceName = _config["Service:Name"] ?? "UnknownService";
            }
        }

        internal IExternalScopeProvider ScopeProvider { get; private set; } = new LoggerExternalScopeProvider();


        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new KafkaLogger(name, this));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            ScopeProvider = scopeProvider ?? new LoggerExternalScopeProvider();
        }
    }
}
