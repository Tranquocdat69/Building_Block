using System.Text.RegularExpressions;
using Confluent.Kafka;
using FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Config;
using FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Util;
using Microsoft.Extensions.Logging;

namespace FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka;

public class Logger : ILogger
{
    private readonly string _name;
    private readonly Configuration _configuration;

    public Logger(string name, Configuration configuration)
    {
        _name = name;
        _configuration = configuration;
    }
    public IDisposable BeginScope<TState>(TState state) => default!;

    public bool IsEnabled(LogLevel logLevel) => true;
    private bool IsMatchLogger(string logger, string loggerPattern)
    {
        bool endsWithAst = false;
        loggerPattern = loggerPattern.Replace(".", "\\.");
        if (loggerPattern.EndsWith("*"))
        {
            loggerPattern = loggerPattern.Substring(0, loggerPattern.Length - 1);
            endsWithAst = true;
        }
        loggerPattern = loggerPattern.Replace("*", "\\w+");
        if (endsWithAst)
        {
            loggerPattern += ".*";
        }
        loggerPattern = "^" + loggerPattern + "$";
        return Regex.IsMatch(logger, loggerPattern);
    }

    private string GetLogContent(LogLevel logLevel, string message, Target target)
    {
        string content = target.Template;
        int levelNameIndex = (int)logLevel;
        if (!string.IsNullOrEmpty(content))
        {
            content = content.Replace("{level}", target.LevelNames[levelNameIndex]);
            content = content.Replace("{message}", message);
            content = content.Replace("{date}", DateTime.Now.ToString(target.DateTimeFormat));
            content = content.Replace("{logger}", _name);
            return content;
        }
        return message;
    }

    private IList<Target> GetTargets(LogLevel logLevel)
    {
        IList<Target> targets = new List<Target>();
        short logLv = (short)logLevel;
        foreach (Rule rule in _configuration.Rules)
        {
            short minLogLv = (short)InternalUtil.GetLogLevelByName(rule.MinLevel);
            if (minLogLv == 6)
            {
                minLogLv = -1;
            }
            short maxLogLv = (short)InternalUtil.GetLogLevelByName(rule.MaxLevel);
            if (logLv >= minLogLv && logLv <= maxLogLv && IsMatchLogger(_name, rule.Pattern))
            {
                try
                {
                    targets.Add(_configuration.Targets[rule.WriteTo]);
                }
                catch { }
            }
        }
        return targets;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        IList<Target> targets = GetTargets(logLevel);
        int targetsCount = targets.Count();
        if (targetsCount == 1)
        {
            Target target = targets[0];
            ProduceLogToKafka(logLevel, formatter(state, exception), target);
        }
        else if (targetsCount > 1)
        {
            Parallel.ForEach<Target>(targets, target =>
            {
                ProduceLogToKafka(logLevel, formatter(state, exception), target);
            });
        }
    }

    private void ProduceLogToKafka(LogLevel logLevel, string message, Target target)
    {
        string logContent = GetLogContent(logLevel, message, target);
        target.LogProducer.Produce(target.Topic, new Message<Null, string> { Value = logContent });
    }
}