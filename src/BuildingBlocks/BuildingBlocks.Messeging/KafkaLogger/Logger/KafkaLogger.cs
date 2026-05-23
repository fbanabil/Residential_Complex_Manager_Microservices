using BuildingBlocks.Messaging.KafkaLogger.ProducerAndConsumer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger.Logger
{
    public sealed class KafkaLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly KafkaLoggerProvider _provider;

        public KafkaLogger(string categoryName, KafkaLoggerProvider provider)
        {
            _categoryName = categoryName;
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return _provider.ScopeProvider.Push(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel!= LogLevel.None && logLevel >= _provider.Options.MinLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            
            if(String.IsNullOrEmpty(message))
            {
                return;
            }

            var properties = new Dictionary<string, string?>();

            AddPropertiesFromState(properties, state);
            AddPropertiesFromScopes(properties);


            var activity = System.Diagnostics.Activity.Current;

            var logMessage = new LogModel
            {
                Timestamp = DateTime.UtcNow,
                ServiceName = _provider.Options.ServiceName,
                Environment = _provider.Options.Environment,
                LogLevel = logLevel.ToString(),
                Category = _categoryName,
                EventId = eventId.Id,
                EventName = eventId.Name,
                Message = message,

                ExceptionType = exception?.GetType().FullName,
                ExceptionMessage = exception?.Message,
                ExceptionStackTrace = exception?.StackTrace,

                TraceId = activity?.TraceId.ToString(),
                SpanId = activity?.SpanId.ToString(),

                RequestId = properties.ContainsKey("RequestId") ? properties["RequestId"] : null,
                CorrelationId = properties.ContainsKey("CorrelationId") ? properties["CorrelationId"] : null,

                Properties = properties
            };

            _provider.LogQueue.TryEnqueue(logMessage);
        }

        private void AddPropertiesFromState<TState>(Dictionary<string, string?> properties, TState state)
        {
            if (state is IEnumerable<KeyValuePair<string, object>> stateProperties)
            {
                foreach (var pair in stateProperties)
                {
                    if (pair.Key != "{OriginalFormat}")
                    {
                        properties[pair.Key] = pair.Value?.ToString();
                    }
                }
            }
        }

        private void AddPropertiesFromScopes(Dictionary<string, string?> properties)
        {
            _provider.ScopeProvider.ForEachScope((scope, props) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> scopeProperties)
                {
                    foreach (var pair in scopeProperties)
                    {
                        props[pair.Key] = pair.Value?.ToString();
                    }
                }
                else if(scope != null)
                {
                    props["Scope"] = scope.ToString();
                }
            }, properties);
        }

        private static string? GetValue(Dictionary<string, string?> properties, string key)
        {
            return properties.TryGetValue(key, out var value) ? value : null;
        }
    }
}