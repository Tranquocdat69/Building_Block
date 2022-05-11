using FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Util;

namespace FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Config;

public class Target
{
    public string Template { get; set; }
    public string BootstrapServers { get; set; }
    public string Topic { get; set; }
    internal string DateTimeFormat { get; set; }
    internal string[] LevelNames { get; set; }
    internal KafkaProducer LogProducer { get; set; }
}