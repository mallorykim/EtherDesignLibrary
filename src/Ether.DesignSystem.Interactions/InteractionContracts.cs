using System.Text.Json;

namespace Ether.DesignSystem.Interactions;

/// <summary>Context attached to every interaction emitted by an application screen.</summary>
public sealed record InteractionContext(
    string CorrelationId,
    string? Screen = null,
    string? TenantId = null,
    string? ActorId = null);

/// <summary>A versioned, backend-consumable user interaction envelope.</summary>
public sealed record InteractionEvent(
    Guid EventId,
    string IdempotencyKey,
    string Type,
    int SchemaVersion,
    DateTimeOffset OccurredAtUtc,
    InteractionContext Context,
    string ComponentId,
    JsonElement Data)
{
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
