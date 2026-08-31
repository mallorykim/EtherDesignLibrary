using System.Text.Json;

namespace Ether.DesignSystem.Interactions;

/// <summary>Context attached to every interaction emitted by an application screen.</summary>
public sealed record InteractionContext(
    string CorrelationId,
    string? Screen = null,
    string? TenantId = null,
    string? ActorId = null);

/// <summary>
/// A versioned, backend-consumable user interaction envelope.
/// </summary>
/// <remarks>
/// Direct construction is not supported: the primary constructor is private, so this is
/// deliberately not a positional record you can call <c>new InteractionEvent(...)</c> on.
/// <see cref="Create"/> is the only supported way to obtain one - it is what fills in a fresh
/// <see cref="EventId"/>, stamps <see cref="SchemaVersion"/> at the version this library
/// actually emits, derives a fallback <see cref="IdempotencyKey"/> when the caller does not
/// supply one, and rejects a blank <see cref="Type"/>, <see cref="ComponentId"/>, or
/// <see cref="InteractionContext.CorrelationId"/> up front. Bypassing it (for example by
/// reflecting into the private constructor) would trade all of that for envelopes an outbox
/// cannot trust. <c>with</c>-expressions on an already-valid instance remain supported, the
/// same as for any other record.
/// </remarks>
public sealed record InteractionEvent
{
    private InteractionEvent(
        Guid eventId,
        string idempotencyKey,
        string type,
        int schemaVersion,
        DateTimeOffset occurredAtUtc,
        InteractionContext context,
        string componentId,
        JsonElement data)
    {
        EventId = eventId;
        IdempotencyKey = idempotencyKey;
        Type = type;
        SchemaVersion = schemaVersion;
        OccurredAtUtc = occurredAtUtc;
        Context = context;
        ComponentId = componentId;
        Data = data;
    }

    /// <summary>Gets the unique identifier of this event instance.</summary>
    public Guid EventId { get; init; }

    /// <summary>Gets the key a durable outbox uses to deduplicate delivery attempts.</summary>
    public string IdempotencyKey { get; init; }

    /// <summary>Gets the business event type.</summary>
    public string Type { get; init; }

    /// <summary>Gets the envelope schema version.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Gets the UTC instant the interaction occurred.</summary>
    public DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>Gets the screen/tenant/actor context attached to this event.</summary>
    public InteractionContext Context { get; init; }

    /// <summary>Gets the identifier of the control that produced this event.</summary>
    public string ComponentId { get; init; }

    /// <summary>Gets the immutable JSON payload.</summary>
    public JsonElement Data { get; init; }

    /// <summary>Creates a version-one interaction envelope with an immutable JSON payload.</summary>
    public static InteractionEvent Create(
        string type,
        InteractionContext context,
        string componentId,
        object? data,
        string? idempotencyKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.CorrelationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(componentId);

        var eventId = Guid.NewGuid();
        var resolvedIdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? eventId.ToString("N")
            : idempotencyKey;

        return new InteractionEvent(
            eventId,
            resolvedIdempotencyKey,
            type,
            1,
            DateTimeOffset.UtcNow,
            context,
            componentId,
            JsonSerializer.SerializeToElement(data));
    }
}

/// <summary>Receives interaction envelopes after a durable outbox has accepted them.</summary>
public interface IInteractionSink
{
    /// <summary>Persists an interaction for eventual transport to the backend.</summary>
    ValueTask EnqueueAsync(InteractionEvent interaction, CancellationToken cancellationToken = default);
}

/// <summary>Provides an interaction envelope raised by a control adapter.</summary>
public sealed class InteractionProducedEventArgs(InteractionEvent interaction) : EventArgs
{
    /// <summary>Gets the immutable envelope to enqueue in an application outbox.</summary>
    public InteractionEvent Interaction { get; } = interaction ?? throw new ArgumentNullException(nameof(interaction));
}
