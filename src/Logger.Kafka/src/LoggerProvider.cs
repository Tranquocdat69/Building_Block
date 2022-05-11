using System.Collections.Concurrent;
using FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Config;
using FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka;
public sealed class LoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, Logger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Configuration _configuration;

    public LoggerProvider(IOptions<Configuration> options)
    {
        _configuration = InternalUtil.StandardizeConfiguration(options.Value);
    }
    public ILogger CreateLogger(string categoryName)
    => _loggers.GetOrAdd(categoryName, name => new Logger(name, _configuration));

    public void Dispose()
    {
        _loggers.Clear();
    }
}
