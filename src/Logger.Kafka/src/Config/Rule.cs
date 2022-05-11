namespace FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Config;

public class Rule
{
    public string Pattern { get; set; }
    public string MinLevel { get; set; }
    public string MaxLevel { get; set; }
    public string WriteTo { get; set; }
}