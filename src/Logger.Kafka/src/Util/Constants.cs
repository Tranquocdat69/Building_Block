namespace FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Util;

internal static class Constants
{
    internal const string TraceLogLevelName = "Trace";
    internal const string DebugLogLevelName = "Debug";
    internal const string InformationLogLevelLongName = "Information";
    internal const string InformationLogLevelShortName = "Info";
    internal const string WarningLogLevelLongName = "Warning";
    internal const string WarningLogLevelShortName = "Warn";
    internal const string ErrorLogLevelLongName = "Error";
    internal const string ErrorLogLevelShortName = "Err";
    internal const string CriticalLogLevelLongName = "Critical";
    internal const string CriticalLogLevelShortName = "Crit";
    internal const string NoneLogLevelName = "None";
    internal const string DefaultLoggerPattern = "*";
    internal const string DefaultMinLogLevelName = TraceLogLevelName;
    internal const string DefaultMaxLogLevelName = CriticalLogLevelLongName;
    internal const string DefaultTargetName = "target1";
    internal const string DefaultLogTemplate = "{date} {level:uppercase=true:truncate=short} {logger} {message}";
    internal const string DefaultDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";
    internal const string DefaultKafkaBootstrapServers = "localhost:9092";
    internal const string DefaultKafkaTopic = "log-topic";
}