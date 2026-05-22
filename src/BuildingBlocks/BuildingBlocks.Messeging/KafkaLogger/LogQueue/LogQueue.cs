using BuildingBlocks.Messaging.KafkaLogger.Configs;
using MassTransit.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger.LogQueue
{
    public class LogQueue : ILogQueue
    {
        private readonly Channel<LogModel> _channel;


        public LogQueue(IOptions<KafkaSettings> options)
        {
            _channel = Channel.CreateBounded<LogModel>(new BoundedChannelOptions(options.Value.QueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = false,
                    SingleWriter = false

            });
        }

        public ChannelReader<LogModel> Reader => _channel.Reader;


        public bool TryEnqueue(LogModel log)
        {
            return _channel.Writer.TryWrite(log);
        }
    }
}
