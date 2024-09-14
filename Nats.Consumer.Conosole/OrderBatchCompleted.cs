namespace Nats.Consumer.Console;

public class OrderBatchCompleted
{
    public OrderBatchCompleted(Guid batchId)
    {
        BatchId = batchId;
    }

    public Guid BatchId { get; }
}