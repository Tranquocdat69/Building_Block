using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Confluent.Kafka;
using FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Config;
using Microsoft.Extensions.Logging;

namespace FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Util;
internal static class InternalUtil
{
    internal static LogLevel GetLogLevelByName(string logLevelName)
    {
        LogLevel logLevel = LogLevel.None;
        switch (logLevelName)
        {
            case Constants.DebugLogLevelName:
                logLevel = LogLevel.Debug;
                break;
            case Constants.TraceLogLevelName:
                logLevel = LogLevel.Trace;
                break;
            case Constants.InformationLogLevelLongName:
            case Constants.InformationLogLevelShortName:
                logLevel = LogLevel.Information;
                break;
            case Constants.WarningLogLevelLongName:
            case Constants.WarningLogLevelShortName:
                logLevel = LogLevel.Warning;
                break;
            case Constants.ErrorLogLevelLongName:
            case Constants.ErrorLogLevelShortName:
                logLevel = LogLevel.Error;
                break;
            case Constants.CriticalLogLevelLongName:
            case Constants.CriticalLogLevelShortName:
                logLevel = LogLevel.Critical;
                break;
        }
        return logLevel;
    }

    internal static string[] GetAllLogLevelLongName()
    {
        return new string[]{Constants.TraceLogLevelName, Constants.DebugLogLevelName, Constants.InformationLogLevelLongName,
        Constants.WarningLogLevelLongName, Constants.ErrorLogLevelLongName, Constants.ErrorLogLevelLongName,
        Constants.CriticalLogLevelLongName, Constants.NoneLogLevelName };
    }

    internal static string[] GetAllLogLevelShortName()
    {
        return new string[]{Constants.TraceLogLevelName, Constants.DebugLogLevelName, Constants.InformationLogLevelShortName,
        Constants.WarningLogLevelShortName, Constants.ErrorLogLevelShortName, Constants.ErrorLogLevelShortName,
        Constants.CriticalLogLevelShortName, Constants.NoneLogLevelName };
    }

    internal static string GetLogLevelShortName(LogLevel logLevel)
    => logLevel switch
    {
        LogLevel.Trace => Constants.TraceLogLevelName,
        LogLevel.Debug => Constants.DebugLogLevelName,
        LogLevel.Information => Constants.InformationLogLevelShortName,
        LogLevel.Warning => Constants.WarningLogLevelShortName,
        LogLevel.Error => Constants.ErrorLogLevelShortName,
        LogLevel.Critical => Constants.CriticalLogLevelShortName,
        _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
    };

    internal static string GetLogLevelLongName(LogLevel logLevel)
    => logLevel switch
    {
        LogLevel.Trace => Constants.TraceLogLevelName,
        LogLevel.Debug => Constants.DebugLogLevelName,
        LogLevel.Information => Constants.InformationLogLevelLongName,
        LogLevel.Warning => Constants.WarningLogLevelLongName,
        LogLevel.Error => Constants.ErrorLogLevelLongName,
        LogLevel.Critical => Constants.CriticalLogLevelLongName,
        _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
    };

    internal static string ConvertListToString(IEnumerable<object> list)
    {
        string result = "";
        if (list != null)
        {
            for (int ix = 0; ix < list.Count(); ix++)
            {
                if (ix == 0)
                {
                    result += list.ElementAt(ix).ToString();
                }
                else
                {
                    result += ", " + list.ElementAt(ix).ToString();
                }
            }
        }
        return result;
    }

    internal static Configuration StandardizeConfiguration(Configuration configuration)
    {
        if (string.IsNullOrEmpty(configuration.BootstrapServers))
        {
            configuration.BootstrapServers = Constants.DefaultKafkaBootstrapServers;
        }
        if (string.IsNullOrEmpty(configuration.AppName))
        {
            configuration.AppName = string.Empty;
        }
        if (configuration.Targets == null)
        {
            configuration.Targets = new Dictionary<string, Target> { { Constants.DefaultTargetName, new Target() } };
        }
        if (configuration.Rules == null)
        {
            configuration.Rules = new Rule[] { new Rule() };
        }
        foreach (KeyValuePair<string, Target> keyValue in configuration.Targets)
        {
            StandardizeTarget(keyValue.Value, configuration.BootstrapServers,
            configuration.AppName);
        }
        foreach (Rule rule in configuration.Rules)
        {
            StandardizeRule(rule);
        }
        return configuration;
    }

    private static Rule StandardizeRule(Rule rule)
    {
        if (string.IsNullOrEmpty(rule.Pattern))
        {
            rule.Pattern = Constants.DefaultLoggerPattern;
        }
        if (string.IsNullOrEmpty(rule.MaxLevel))
        {
            rule.MaxLevel = Constants.DefaultMaxLogLevelName;
        }
        if (string.IsNullOrEmpty(rule.MinLevel))
        {
            rule.MinLevel = Constants.DefaultMinLogLevelName;
        }
        if (string.IsNullOrEmpty(rule.WriteTo))
        {
            rule.WriteTo = Constants.DefaultTargetName;
        }
        return rule;
    }

    private static Target StandardizeTarget(Target target, string commonBootstrapServers, string appName)
    {
        if (string.IsNullOrEmpty(target.Template))
        {
            target.Template = Constants.DefaultLogTemplate;
        }
        target.LevelNames = GetLevelNamesForTarget(target.Template);
        target.DateTimeFormat = GetDateTimeFormatFromTemplate(target.Template);
        target.Template = StandardizeTemplate(target.Template, appName);
        if (string.IsNullOrEmpty(target.BootstrapServers))
        {
            target.BootstrapServers = commonBootstrapServers;
        }
        if (string.IsNullOrEmpty(target.Topic))
        {
            target.Topic = Constants.DefaultKafkaTopic;
        }
        SetLogProducerForTarget(target);
        return target;
    }

    private static KafkaProducer SetLogProducerForTarget(Target target)
    {
        ProducerConfig producerConfig = new ProducerConfig()
        {
            BootstrapServers = target.BootstrapServers,
            QueueBufferingMaxMessages = 2000000,
            RetryBackoffMs = 500,
            MessageSendMaxRetries = 3,
            LingerMs = 5
        };
        IProducer<Null, string> producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        KafkaProducer kafkaProducer = new KafkaProducer(producer);
        target.LogProducer = kafkaProducer;
        return kafkaProducer;
    }

    private static string StandardizeTemplate(string template, string appName)
    {
        template = MapAppNameToTemplate(template, appName);
        template = MapHostNameToTemplate(template);
        template = MapIPv4ToTemplate(template);
        template = MapIPv6ToTemplate(template);
        template = ReplaceDatePatternToDefault(template);
        template = ReplaceLevelPatternToDefault(template);
        return template;
    }

    private static string ReplaceDatePatternToDefault(string template)
    {
        string dateVariablePattern = @"{date(:format=[-\\/:\s\.a-zA-Z]+)?}";
        template = Regex.Replace(template, dateVariablePattern, "{date}");
        return template;
    }

    private static string ReplaceLevelPatternToDefault(string template)
    {
        string levelVariablePattern = @"{level(:uppercase=(true|false))?(:truncate=(short|long))?}";
        template = Regex.Replace(template, levelVariablePattern, "{level}");
        return template;
    }

    private static string MapHostNameToTemplate(string template)
    {
        template = template.Replace("{hostName}", GetHostName());
        return template;
    }

    private static string MapIPv6ToTemplate(string template)
    {
        IEnumerable<string> iPv6s = GetHostIPs(AddressFamily.InterNetworkV6);
        IList<string> variablePatterns = GetIpVariablePatternsFromTemplate(template, AddressFamily.InterNetworkV6);
        template = MapIPToTemplate(template, variablePatterns, iPv6s);
        return template;
    }

    private static string MapIPv4ToTemplate(string template)
    {
        IEnumerable<string> iPv4s = GetHostIPs(AddressFamily.InterNetwork);
        IList<string> variablePatterns = GetIpVariablePatternsFromTemplate(template, AddressFamily.InterNetwork);
        template = MapIPToTemplate(template, variablePatterns, iPv4s);
        return template;
    }

    private static string MapIPToTemplate(string template, IList<string> variablePatterns, IEnumerable<string> iPs)
    {
        string indexRegex = @"\[\d+\]";
        foreach (string variablePattern in variablePatterns)
        {
            string indexMatch = Regex.Match(variablePattern, indexRegex).Value;
            if (!string.IsNullOrEmpty(indexMatch))
            {
                int index = int.Parse(indexMatch.Substring(1, indexMatch.Length - 2));
                string ip = "";
                try
                {
                    ip = iPs.ElementAt(index);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    template = template.Replace(variablePattern, ip);
                }
            }
            else
            {
                template = template.Replace(variablePattern, InternalUtil.ConvertListToString(iPs));
            }
        }
        return template;
    }

    private static IList<string> GetIpVariablePatternsFromTemplate(string template, AddressFamily addressFamily)
    {
        const string iPv4VariableRegex = @"{iPv4s(\[\d+\])?}";
        const string iPv6VariableRegex = @"{iPv6s(\[\d+\])?}";
        IList<string> variablePatterns;
        if (addressFamily == AddressFamily.InterNetwork)
        {
            variablePatterns = GetVariablePatternsFromTemplate(template, iPv4VariableRegex);
        }
        else
        {
            variablePatterns = GetVariablePatternsFromTemplate(template, iPv6VariableRegex);
        }
        return variablePatterns;
    }

    private static IList<string> GetVariablePatternsFromTemplate(string template, string regexPattern)
    {
        MatchCollection matches = Regex.Matches(template, regexPattern);
        List<string> variablePatterns = new List<string>();
        foreach (Match match in matches)
        {
            variablePatterns.Add(match.Value);
        }
        return variablePatterns;
    }

    private static string MapAppNameToTemplate(string template, string appName)
    {
        if (!string.IsNullOrEmpty(appName))
        {
            template = template.Replace("{appName}", appName);
        }
        return template;
    }

    private static IEnumerable<string> GetHostIPs(AddressFamily addressFamily)
    {
        IPHostEntry iPHostEntry = Dns.GetHostEntry(GetHostName());
        IList<string> ipAddresses = new List<string>();
        foreach (var ip in iPHostEntry.AddressList)
        {
            if (ip.AddressFamily == addressFamily)
            {
                yield return ip.ToString();
            }
        }
    }
    private static string GetHostName()
    {
        try
        {
            return Dns.GetHostName();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string[] GetLevelNamesForTarget(string template)
    {
        string levelVariableRegex = @"{level(:uppercase=(true|false))?(:truncate=(short|long))?}";
        string[] logLevelNames = InternalUtil.GetAllLogLevelLongName();
        Match match = Regex.Match(template, levelVariableRegex);
        if (match != null && !string.IsNullOrEmpty(match.Value))
        {
            bool? isUppercase = GetLevelUppercaseParam(match.Value);
            string truncate = GetLevelTruncateParam(match.Value);
            if (!string.IsNullOrEmpty(truncate) && truncate.Equals("short"))
            {
                logLevelNames = InternalUtil.GetAllLogLevelShortName();
            }
            if (isUppercase != null)
            {
                if (isUppercase.Value)
                {
                    for (int ix = 0; ix < logLevelNames.Length; ix++)
                    {
                        logLevelNames[ix] = logLevelNames[ix].ToUpper();
                    }
                }
                else
                {
                    for (int ix = 0; ix < logLevelNames.Length; ix++)
                    {
                        logLevelNames[ix] = logLevelNames[ix].ToLower();
                    }
                }
            }
        }
        return logLevelNames;
    }

    private static bool? GetLevelUppercaseParam(string pattern)
    {
        Match match = Regex.Match(pattern, @"(true|false)");
        if (match != null && !string.IsNullOrEmpty(match.Value))
        {
            return bool.Parse(match.Value);
        }
        return null;
    }

    private static string GetLevelTruncateParam(string pattern)
    {
        Match match = Regex.Match(pattern, @"(short|long)");
        if (match != null && !string.IsNullOrEmpty(match.Value))
        {
            return match.Value;
        }
        return null;
    }

    private static string GetDateTimeFormatFromTemplate(string template)
    {
        string dateVariablePattern = @"{date(:format=[-\\/:\s\.a-zA-Z]+)?}";
        Match match = Regex.Match(template, dateVariablePattern);
        if (match != null && !string.IsNullOrEmpty(match.Value))
        {
            string value = match.Value;
            if (value.Length > 14)
            {
                return value.Substring(13, value.Length - 14);
            }
            return Constants.DefaultDateTimeFormat;
        }
        return string.Empty;
    }
}