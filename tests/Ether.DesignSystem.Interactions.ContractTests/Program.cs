using Ether.DesignSystem.Interactions;

var context = new InteractionContext("trace-123", "OrderEdit", "tenant-456", "actor-789");
var interaction = InteractionEvent.Create(
    "order.submit.requested",
    context,
    "order-submit",
    new { OrderId = "order-42" },
    "idem-42");

var mockBackend = new RecordingSink();
await mockBackend.EnqueueAsync(interaction);

if (mockBackend.Interactions.Count != 1 ||
    mockBackend.Interactions[0].EventId == Guid.Empty ||
    mockBackend.Interactions[0].IdempotencyKey != "idem-42" ||
    mockBackend.Interactions[0].SchemaVersion != 1 ||
    mockBackend.Interactions[0].Context.CorrelationId != "trace-123" ||
    mockBackend.Interactions[0].Type != "order.submit.requested" ||
    mockBackend.Interactions[0].ComponentId != "order-submit" ||
    mockBackend.Interactions[0].Data.GetProperty("OrderId").GetString() != "order-42")
{
    throw new InvalidOperationException("The interaction envelope was not consumable by the mock backend.");
}

Console.WriteLine("Interaction contract mock-consumer smoke test passed.");

sealed class RecordingSink : IInteractionSink
{
    public List<InteractionEvent> Interactions { get; } = [];

    public ValueTask EnqueueAsync(InteractionEvent interaction, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interactions.Add(interaction);
        return ValueTask.CompletedTask;
    }
}
