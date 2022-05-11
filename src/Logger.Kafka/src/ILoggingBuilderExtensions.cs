using FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka;

public static class loggingBuilderExtensions
{
    public static ILoggingBuilder AddKafka(this ILoggingBuilder loggingBuilder)
    {
        loggingBuilder.Services
        .TryAddEnumerable(ServiceDescriptor
        .Singleton<ILoggerProvider, LoggerProvider>());
        return loggingBuilder;
    }
    public static ILoggingBuilder AddKafka(this ILoggingBuilder loggingBuilder, Action<Configuration> configure)
    {
        loggingBuilder.AddKafka();
        loggingBuilder.Services.Configure<Configuration>(configure);
        return loggingBuilder;
    }
    public static ILoggingBuilder AddKafka(this ILoggingBuilder loggingBuilder, IConfigurationSection configurationSection)
    {
        loggingBuilder.AddKafka(options =>
        {
            Configuration configuration = configurationSection.Get<Configuration>();
            options.AppName = configuration.AppName;
            options.BootstrapServers = configuration.BootstrapServers;
            options.Rules = configuration.Rules;
            options.Targets = configuration.Targets;
        });
        return loggingBuilder;
    }
}