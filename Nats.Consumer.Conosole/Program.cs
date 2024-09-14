using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.Serializers.Json;
using Nats.Consumer.Console;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var serilogLogger = new LoggerConfiguration().WriteTo
    .Console(theme: AnsiConsoleTheme.Code)
    .CreateLogger();
var loggerFactory = LoggerFactory.Create(
    builder =>
    {
        builder.ClearProviders();
        builder.AddSerilog(serilogLogger);
    });
var logger = loggerFactory.CreateLogger("NATS-by-Example");

var url = Environment.GetEnvironmentVariable("NATS_URL") ?? "127.0.0.1:4222";
var cancellationTokenSource = new CancellationTokenSource();

var opts = new NatsOpts
{
    Url = url,
    Name = "NATS-by-Example",
    AuthOpts = NatsAuthOpts.Default with { Username = "cp-admin", Password = "password" },
    LoggerFactory = loggerFactory,
    SerializerRegistry = new NatsJsonSerializerRegistry()
};
await using var natsConnection = new NatsConnection(opts);

var js = new NatsJSContext(natsConnection);
await js.CreateStreamAsync(new StreamConfig("orders", ["orders.>"]));

var consumer = await js.CreateOrUpdateConsumerAsync(
    "orders",
    new ConsumerConfig("order-consumer")
    {
        AckPolicy = ConsumerConfigAckPolicy.Explicit,
        AckWait = TimeSpan.FromSeconds(90),
        MaxAckPending = 1,
        MaxDeliver = 3,
        // FilterSubject = "orders.batch.>"
        FilterSubjects =
        [
            "orders.batch.*.created",
            "orders.batch.order.*.created",
            "orders.batch.*.completed"
        ]
    });
var batchId = Guid.NewGuid();
var batchSize = 5;
await js.PublishAsync($"orders.batch.{batchId}.created", new OrderBatchCreated(batchId, batchSize));

foreach (var num in Enumerable.Range(1, batchSize))
{
    await js.PublishAsync(
        $"orders.batch.order.{num}.created",
        new OrderCreated(num, batchId) { Total = Random.Shared.Next(50, 500), OrderDate = DateTimeOffset.UtcNow });
}

await js.PublishAsync($"orders.batch.{batchId}.completed", new OrderBatchCompleted(batchId));

Task.Run(
    async () =>
    {
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var msg = await consumer.NextAsync<OrderBatchCreated>();
            if (msg is { Data: not null })
            {
                logger.LogInformation(
                    "Received message from {Subject} subject for {Consumer} with payload {@Payload}",
                    msg.Value.Subject,
                    nameof(OrderBatchCreated),
                    msg.Value.Data);
                await msg.Value.AckAsync();
            }
        }
    });

Task.Run(
    async () =>
    {
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var msg = await consumer.NextAsync<OrderCreated>();
            if (msg is { Data: not null })
            {
                logger.LogInformation(
                    "Received message from {Subject} subject for {Consumer} with payload {@Payload}",
                    msg.Value.Subject,
                    nameof(OrderCreated),
                    msg.Value.Data);
                await msg.Value.AckAsync();
            }
        }
    });

Task.Run(
    async () =>
    {
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var msg = await consumer.NextAsync<OrderBatchCompleted>();
            if (msg is { Data: not null })
            {
                logger.LogInformation(
                    "Received message from {Subject} subject for {Consumer} with payload {@Payload}",
                    msg.Value.Subject,
                    nameof(OrderBatchCompleted),
                    msg.Value.Data);
                await msg.Value.AckAsync();
            }
        }
    });

Console.ReadKey();
await cancellationTokenSource.CancelAsync();
cancellationTokenSource.Dispose();