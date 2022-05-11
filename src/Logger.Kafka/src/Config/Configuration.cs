namespace FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Config;

public class Configuration
{
    public IDictionary<string, Target> Targets { get; set; }
    public IList<Rule> Rules { get; set; }
    public string BootstrapServers { get; set; }
    public string AppName { get; set; }
}