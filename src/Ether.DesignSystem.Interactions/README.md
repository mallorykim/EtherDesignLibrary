# Ether Design System Interactions (Preview)

`Ether.DesignSystem.Interactions` is the application-layer boundary between WinUI controls and a backend interaction pipeline. It turns control changes into versioned, correlation-aware interaction envelopes; it intentionally does not perform HTTP, authentication, retries, or persistence.

Subscribe to `ControlInteractionAdapter.InteractionProduced` and write the envelope to the application's durable outbox. A backend adapter then publishes the envelope using its own API and authentication policy.

Use business event names such as `order.submit.requested` or `filters.category.changed`, and use a stable `componentId` plus a caller-provided correlation ID. For write operations, supply `idempotencyKeyFactory` to the observer so retries of one business operation preserve the same key. Do not use pointer or click names as backend event types.

For dropdowns, configure `SelectedValuePath` (or place a stable ID in the item model) before treating `SelectedValue` as a business identifier. If WinUI supplies a UI object instead, the adapter deliberately reduces it to a JSON-safe display value instead of serializing a native object graph.

For a component-specific dependency property that has an explicit business contract, call `ObserveProperty(control, property, propertyName, eventType, context, componentId, idempotencyKeyFactory)`. Its envelope has `data.Property` and JSON-safe `data.Value`. Select properties deliberately: visual-only settings are readable and observable, but should not become blanket backend telemetry. Native UI objects are reduced to display text rather than serializing their object graph.
