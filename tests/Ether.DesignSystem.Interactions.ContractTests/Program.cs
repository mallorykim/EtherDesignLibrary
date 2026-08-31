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

// B4: direct construction must not be reachable. InteractionEvent.Create is the only
// supported path, so the type must not expose a public constructor.
if (typeof(InteractionEvent).GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Length != 0)
{
    throw new InvalidOperationException("InteractionEvent must not expose a public constructor; use InteractionEvent.Create instead.");
}

// Create() must still reject the inputs that would otherwise produce an unusable envelope.
void AssertCreateRejects(string description, Action create)
{
    try
    {
        create();
    }
    catch (ArgumentException)
    {
        return;
    }

    throw new InvalidOperationException($"InteractionEvent.Create did not reject {description}.");
}

AssertCreateRejects("a blank type", () => InteractionEvent.Create("", context, "order-submit", null));
AssertCreateRejects("a blank componentId", () => InteractionEvent.Create("order.submit.requested", context, "", null));
AssertCreateRejects("a context with a blank CorrelationId", () => InteractionEvent.Create("order.submit.requested", new InteractionContext(""), "order-submit", null));

// `with`-expressions on an already-valid instance must keep working now that the primary
// constructor is private.
var mutated = interaction with { ComponentId = "order-submit-2" };
if (mutated.ComponentId != "order-submit-2" || mutated.EventId != interaction.EventId)
{
    throw new InvalidOperationException("InteractionEvent no longer supports `with`-expressions after tightening its constructor.");
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
