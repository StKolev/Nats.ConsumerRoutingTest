namespace Nats.Consumer.Console;

public class OrderBatchCreated
{
    public OrderBatchCreated(Guid batchId, int ordersCount)
    {
        BatchId = batchId;
        OrdersCount = ordersCount;
    }

    public Guid BatchId { get; }

    public int OrdersCount { get; }
}