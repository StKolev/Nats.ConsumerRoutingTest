namespace Nats.Consumer.Console;

public class OrderCreated
{
    public OrderCreated(int id, Guid batchId)
    {
        Id = id;
        BatchId = batchId;
    }

    public int Id { get; }

    public Guid BatchId { get; }

    public DateTimeOffset OrderDate { get; set; }


    public decimal Total { get; set; }
}