using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections;
using System.Globalization;

namespace Ether.DesignSystem.Interactions;

/// <summary>
/// Adapts native WinUI and Ether control interactions into business event envelopes.
/// This class is UI-only: callers enqueue <see cref="InteractionProduced"/> into a durable
/// application outbox; no transport or backend credentials are held here.
/// </summary>
public sealed class ControlInteractionAdapter
{
    /// <summary>Raised synchronously when an observed control produces a business interaction.</summary>
    public event EventHandler<InteractionProducedEventArgs>? InteractionProduced;

    /// <summary>Observes a button command.</summary>
    public IDisposable ObserveButton(Button control, string eventType, InteractionContext context, string componentId, Func<string>? idempotencyKeyFactory = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ValidateSubscriptionArguments(eventType, context, componentId);
        RoutedEventHandler handler = (_, _) => Emit(eventType, context, componentId, new { }, idempotencyKeyFactory);
        control.Click += handler;
        return new Subscription(() => control.Click -= handler);
    }

    /// <summary>Observes a text input value after each WinUI Text dependency-property change.</summary>
    public IDisposable ObserveInput(TextBox control, string eventType, InteractionContext context, string componentId, Func<string>? idempotencyKeyFactory = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ValidateSubscriptionArguments(eventType, context, componentId);
        var token = control.RegisterPropertyChangedCallback(
            TextBox.TextProperty,
            (_, _) => Emit(eventType, context, componentId, new { control.Text }, idempotencyKeyFactory));
        return new Subscription(() => control.UnregisterPropertyChangedCallback(TextBox.TextProperty, token));
    }

    /// <summary>Observes the selected value of a dropdown.</summary>
    public IDisposable ObserveDropdown(ComboBox control, string eventType, InteractionContext context, string componentId, Func<string>? idempotencyKeyFactory = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ValidateSubscriptionArguments(eventType, context, componentId);
        SelectionChangedEventHandler handler = (_, _) => Emit(
            eventType,
            context,
            componentId,
            new { SelectedValue = ToConsumableValue(control.SelectedValue), control.SelectedIndex },
            idempotencyKeyFactory);
        control.SelectionChanged += handler;
        return new Subscription(() => control.SelectionChanged -= handler);
    }

    /// <summary>Observes the selected index and value of any WinUI selector.</summary>
    public IDisposable ObserveSelection(Selector control, string eventType, InteractionContext context, string componentId, Func<string>? idempotencyKeyFactory = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ValidateSubscriptionArguments(eventType, context, componentId);
        SelectionChangedEventHandler handler = (_, _) => Emit(
            eventType,
            context,
            componentId,
            new { control.SelectedIndex, SelectedValue = ToConsumableValue(control.SelectedValue) },
            idempotencyKeyFactory);
        control.SelectionChanged += handler;
        return new Subscription(() => control.SelectionChanged -= handler);
    }

    /// <summary>Observes a checkbox, radio button, or toggle-switch state.</summary>
    public IDisposable ObserveToggle(ToggleButton control, string eventType, InteractionContext context, string componentId, Func<string>? idempotencyKeyFactory = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ValidateSubscriptionArguments(eventType, context, componentId);
        RoutedEventHandler handler = (_, _) => Emit(eventType, context, componentId, new { control.IsChecked }, idempotencyKeyFactory);
        control.Checked += handler;
        control.Unchecked += handler;
        control.Indeterminate += handler;
        return new Subscription(() =>
        {
            control.Checked -= handler;
            control.Unchecked -= handler;
            control.Indeterminate -= handler;
        });
    }

    /// <summary>Observes the on/off state of a WinUI toggle switch.</summary>
    public IDisposable ObserveSwitch(ToggleSwitch control, string eventType, InteractionContext context, string componentId, Func<string>? idempotencyKeyFactory = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ValidateSubscriptionArguments(eventType, context, componentId);
        RoutedEventHandler handler = (_, _) => Emit(eventType, context, componentId, new { control.IsOn }, idempotencyKeyFactory);
        control.Toggled += handler;
        return new Subscription(() => control.Toggled -= handler);
    }

    /// <summary>Observes the stable selection contract of an Ether segmented control.</summary>
    public IDisposable ObserveSegmentedControl(EtherSegmentedControl control, string eventType, InteractionContext context, string componentId, Func<string>? idempotencyKeyFactory = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ValidateSubscriptionArguments(eventType, context, componentId);
        EventHandler<SegmentedSelectionChangedEventArgs> handler = (_, args) =>
            Emit(
                eventType,
                context,
                componentId,
                new { OldValue = ToConsumableValue(args.OldValue), NewValue = ToConsumableValue(args.NewValue) },
                idempotencyKeyFactory);
        control.SelectionChanged += handler;
        return new Subscription(() => control.SelectionChanged -= handler);
    }

    /// <summary>Observes an enabled Ether masthead caption action.</summary>
    public IDisposable ObserveMasthead(EtherMasthead control, string eventType, InteractionContext context, string componentId, Func<string>? idempotencyKeyFactory = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ValidateSubscriptionArguments(eventType, context, componentId);
        EventHandler<MastheadActionInvokedEventArgs> handler = (_, args) => Emit(
            eventType,
            context,
            componentId,
            new { args.Action },
            idempotencyKeyFactory);
        control.ActionInvoked += handler;
        return new Subscription(() => control.ActionInvoked -= handler);
    }

    /// <summary>Observes the stable range contract of an Ether steering bar.</summary>
    public IDisposable ObserveSteeringBar(EtherSteeringBar control, string eventType, InteractionContext context, string componentId, Func<string>? idempotencyKeyFactory = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ValidateSubscriptionArguments(eventType, context, componentId);
        EventHandler<SteeringBarValueChangedEventArgs> handler = (_, args) =>
            Emit(eventType, context, componentId, new { args.OldValue, args.NewValue }, idempotencyKeyFactory);
        control.ValueChanged += handler;
        return new Subscription(() => control.ValueChanged -= handler);
    }

    /// <summary>Observes a range value such as an EtherSlider or EtherProgressBar.</summary>
    public IDisposable ObserveRange(RangeBase control, string eventType, InteractionContext context, string componentId, Func<string>? idempotencyKeyFactory = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ValidateSubscriptionArguments(eventType, context, componentId);
        RangeBaseValueChangedEventHandler handler = (_, args) =>
            Emit(eventType, context, componentId, new { args.OldValue, args.NewValue }, idempotencyKeyFactory);
        control.ValueChanged += handler;
        return new Subscription(() => control.ValueChanged -= handler);
    }

    /// <summary>
    /// Observes one explicitly selected dependency property. The payload contains its stable
    /// property name and a JSON-safe value, allowing applications to opt a configuration or
    /// state property into their durable backend outbox without giving this library transport
    /// ownership. Do not use this as a blanket telemetry switch; select only properties that
    /// are meaningful to the receiving business contract.
    /// </summary>
    public IDisposable ObserveProperty(
        DependencyObject control,
        DependencyProperty property,
        string propertyName,
        string eventType,
        InteractionContext context,
        string componentId,
        Func<string>? idempotencyKeyFactory = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ValidateSubscriptionArguments(eventType, context, componentId);

        var token = control.RegisterPropertyChangedCallback(
            property,
            (_, _) => Emit(
                eventType,
                context,
                componentId,
                new { Property = propertyName, Value = ToConsumableValue(control.GetValue(property)) },
                idempotencyKeyFactory));

        return new Subscription(() => control.UnregisterPropertyChangedCallback(property, token));
    }

    /// <summary>
    /// Validates the envelope inputs at subscription time rather than leaving them to surface
    /// only when <see cref="InteractionEvent.Create"/> runs on the first real interaction. A
    /// misconfigured eventType, context, or componentId is then a wiring bug, not a runtime
    /// surprise that waits for a user to click something before it is caught.
    /// </summary>
    private static void ValidateSubscriptionArguments(string eventType, InteractionContext context, string componentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.CorrelationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(componentId);
    }

    private void Emit(string eventType, InteractionContext context, string componentId, object data, Func<string>? idempotencyKeyFactory)
    {
        var interaction = InteractionEvent.Create(eventType, context, componentId, data, idempotencyKeyFactory?.Invoke());
        InteractionProduced?.Invoke(this, new InteractionProducedEventArgs(interaction));
    }

    private static object? ToConsumableValue(object? value)
    {
        if (value is null || value is string || value is bool || value is char || value is Guid || value is DateTime || value is DateTimeOffset)
        {
            return value;
        }

        if (value is Enum)
        {
            return value.ToString();
        }

        if (value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
        {
            return value;
        }

        if (value is IEnumerable enumerable)
        {
            var items = new List<object?>();
            foreach (var item in enumerable)
            {
                items.Add(ToConsumableValue(item));
            }

            return items;
        }

        // UI objects (for example IconElement) have no stable transport schema. Their
        // display text is still inspectable by a receiving adapter without serializing a
        // native object graph or leaking UI implementation details across the boundary.
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
