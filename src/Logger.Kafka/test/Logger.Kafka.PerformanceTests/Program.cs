using FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.PerformanceTests;

class Program
{
    static void Main()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices((_, services) =>
        {
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddKafka(options =>
                {
                    options.AppName = "PerformanceTests";
                    options.BootstrapServers = "10.26.7.58:9092";
                    options.Rules = new Rule[] 
                    {
                        new Rule 
                        {
                            WriteTo = "Target1"
                        }
                    };
                    options.Targets = new Dictionary<string, Target> {
                        {
                            "Target1",
                            new Target 
                            {
                                Template = "{date:format=dd-MM-yyyy HH:mm:ss.fff}|{appName}|{hostName}|{iPv4s[0]}|{iPv6s}|{level:uppercase=true:truncate=short}|{logger}|{message}"
                            }
                        }
                    };
                });
            });
        });
        var app = builder.Build();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var logContent = Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
        for (var i = 0; i < 1000; i++)
        {
            switch (i)
            {
                case 35:
                case 102:
                case 189:
                case 354:
                case 489:
                case 768:
                case 998:
                    ReportLog(logger, logContent);
                    break;
                default:
                    logger.LogInformation(logContent);
                    break;
            }
        }
        app.Run();
    }

    static void ReportLog(ILogger logger, string logContent)
    {
        var startTime = DateTime.Now.Ticks;
        logger.LogInformation(logContent);
        var duration = DateTime.Now.Ticks - startTime;
        Console.WriteLine(duration + " ticks");
    }

}