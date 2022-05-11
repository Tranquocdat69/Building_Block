using Confluent.Kafka;

namespace FPTS.FIT.BDRD.BuildingBlocks.Logger.Kafka.Util;

internal class KafkaProducer : IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private const int PollTimeoutSecond = 1;

    internal KafkaProducer(IProducer<Null, string> producer)
    {
        _producer = producer;
    }

    public void Dispose()
    {
        _producer.Dispose();
    }

    internal void Produce(string topic, Message<Null, string> message)
    {
        try
        {
            _producer.Produce(topic, message);
        }
        catch (ProduceException<Null, string> produceException)
        {
            if (produceException.Error.Code == ErrorCode.Local_QueueFull)
            {
                _producer.Poll(TimeSpan.FromSeconds(PollTimeoutSecond));
            }
            else
            {
                throw;
            }
        }
    }
}