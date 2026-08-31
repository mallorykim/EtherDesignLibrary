using System.Reflection;
using Ether.DesignSystem.Interactions;
using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
{
    // This is deliberately restricted to properties declared by Ether controls. It does not
    // pretend that every inherited FrameworkElement layout knob is business data. New declared
    // properties fail this test unless they have an explicit runtime sample below.
    internal sealed record PropertyConsumptionVerification(
        int ComponentOwnedPropertyCount,
        int PropertyChangedCallbackCount,
        int BackendPropertyEventCount,
        int StandardInteractionEventCount,
        string[] VerifiedProperties,
        bool SubscriptionValidatedEagerly,
        bool DirectConstructionBlocked);

    private static PropertyConsumptionVerification VerifyPropertyConsumption(
        EtherButton button,
        EtherCheckbox checkbox,
        EtherRadioButton radioButton,
        EtherInput input,
        EtherDropdown dropdown,
        EtherSegmentedControl segmentedControl,
        EtherIntelligenceButton intelligenceButton,
        EtherProgressBar progressBar,
        EtherSteeringBar steeringBar,
        EtherSlider slider,
        EtherMasthead masthead,
        ToggleSwitch toggleSwitch,
        ScrollBar scrollBar)
    {
        var adapter = new ControlInteractionAdapter();
        var produced = new List<InteractionEvent>();
        adapter.InteractionProduced += (_, args) => produced.Add(args.Interaction);
        var context = new InteractionContext("property-audit", "ConsumerFixture");
        var verified = new List<string>();
        var callbackCount = 0;
        var propertyEventCount = 0;

        foreach (var type in DeclaredPropertyOwnerTypes)
        {
            if (Activator.CreateInstance(type) is not DependencyObject instance)
            {
                throw new InvalidOperationException($"Could not create {type.Name} for its public-property contract.");
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(field => field.FieldType == typeof(DependencyProperty) && field.Name.EndsWith("Property", StringComparison.Ordinal))
                .OrderBy(field => field.Name, StringComparer.Ordinal)
                .ToArray();

            foreach (var field in fields)
            {
                var property = (DependencyProperty?)field.GetValue(null)
                    ?? throw new InvalidOperationException($"{type.Name}.{field.Name} did not expose a dependency property identifier.");
                var propertyName = field.Name[..^"Property".Length];
                var wrapper = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    ?? throw new InvalidOperationException($"{type.Name}.{propertyName} is missing its public CLR dependency-property wrapper.");
                if (!wrapper.CanRead || !wrapper.CanWrite)
                {
                    throw new InvalidOperationException($"{type.Name}.{propertyName} must be publicly readable and writable.");
                }

                var sample = CreatePropertySample(type, propertyName);
                var callbackObserved = false;
                var token = instance.RegisterPropertyChangedCallback(property, (_, _) => callbackObserved = true);
                using var subscription = adapter.ObserveProperty(
                    instance,
                    property,
                    propertyName,
                    $"{type.Name}.property.changed",
                    context,
                    $"{type.Name}.{propertyName}",
                    () => $"property-audit-{type.Name}-{propertyName}");

                instance.SetValue(property, sample);
                instance.UnregisterPropertyChangedCallback(property, token);

                if (!callbackObserved)
                {
                    throw new InvalidOperationException($"{type.Name}.{propertyName} did not raise a dependency-property change callback.");
                }

                if (!ValuesMatch(sample, instance.GetValue(property)) || !ValuesMatch(sample, wrapper.GetValue(instance)))
                {
                    throw new InvalidOperationException($"{type.Name}.{propertyName} did not round-trip through both GetValue and its CLR wrapper.");
                }

                var interaction = produced.LastOrDefault(item => item.ComponentId == $"{type.Name}.{propertyName}");
                if (interaction is null ||
                    interaction.IdempotencyKey != $"property-audit-{type.Name}-{propertyName}" ||
                    interaction.Data.GetProperty("Property").GetString() != propertyName ||
                    !interaction.Data.TryGetProperty("Value", out _))
                {
                    throw new InvalidOperationException($"{type.Name}.{propertyName} was not emitted as a backend-consumable property envelope.");
                }

                callbackCount++;
                propertyEventCount++;
                verified.Add($"{type.Name}.{propertyName}");
            }
        }

        var standardInteractionEventCount = VerifyStandardInteractionAdapters(
            adapter,
            produced,
            context,
            button,
            checkbox,
            radioButton,
            input,
            dropdown,
            segmentedControl,
            intelligenceButton,
            progressBar,
            steeringBar,
            slider,
            toggleSwitch,
            scrollBar);

        var subscriptionValidatedEagerly = VerifySubscriptionValidatesEagerly(adapter, button, context);
        var directConstructionBlocked = VerifyInteractionEventConstructionBlocked();

        return new PropertyConsumptionVerification(
            verified.Count,
            callbackCount,
            propertyEventCount,
            standardInteractionEventCount,
            verified.ToArray(),
            subscriptionValidatedEagerly,
            directConstructionBlocked);
    }

    /// <summary>
    /// Regression coverage for B3: eventType, context, and componentId must be validated when
    /// Observe* is called, not deferred to the first real interaction. Probes ObserveButton with
    /// each kind of invalid input and asserts the exception surfaces synchronously from the
    /// subscribe call itself - clicking is never reached - and that a rejected subscription
    /// leaves no dangling event-handler registration on the control.
    /// </summary>
    private static bool VerifySubscriptionValidatesEagerly(ControlInteractionAdapter adapter, EtherButton button, InteractionContext validContext)
    {
        var blankCorrelationContext = new InteractionContext(string.Empty);

        void AssertRejectedAtSubscribeTime(string description, Func<IDisposable> subscribe)
        {
            IDisposable? subscription = null;
            try
            {
                subscription = subscribe();
                throw new InvalidOperationException($"ControlInteractionAdapter.ObserveButton did not validate {description} at subscription time.");
            }
            catch (ArgumentException)
            {
                // Expected: rejected eagerly, at Observe* itself, before any interaction fires.
            }
            finally
            {
                subscription?.Dispose();
            }
        }

        AssertRejectedAtSubscribeTime(
            "a blank eventType",
            () => adapter.ObserveButton(button, string.Empty, validContext, "button.subscription-validation-probe"));
        AssertRejectedAtSubscribeTime(
            "a blank componentId",
            () => adapter.ObserveButton(button, "subscription.validation.probe", validContext, string.Empty));
        AssertRejectedAtSubscribeTime(
            "a context with a blank CorrelationId",
            () => adapter.ObserveButton(button, "subscription.validation.probe", blankCorrelationContext, "button.subscription-validation-probe"));

        var producedByRejectedSubscriptions = new List<InteractionEvent>();
        void Handler(object? _, InteractionProducedEventArgs args) => producedByRejectedSubscriptions.Add(args.Interaction);
        adapter.InteractionProduced += Handler;
        try
        {
            Invoke(button);
        }
        finally
        {
            adapter.InteractionProduced -= Handler;
        }

        if (producedByRejectedSubscriptions.Count != 0)
        {
            throw new InvalidOperationException("A rejected ControlInteractionAdapter.ObserveButton subscription still left a control event handler wired up.");
        }

        return true;
    }

    /// <summary>
    /// Regression coverage for B4: InteractionEvent must not expose a public constructor -
    /// InteractionEvent.Create is the only supported construction path. Also re-confirms Create
    /// itself still enforces its invariants, and that `with`-expressions on an already-valid
    /// instance keep working now that the primary constructor is private.
    /// </summary>
    private static bool VerifyInteractionEventConstructionBlocked()
    {
        var publicConstructors = typeof(InteractionEvent).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (publicConstructors.Length != 0)
        {
            throw new InvalidOperationException("InteractionEvent must not expose a public constructor; InteractionEvent.Create should be the only supported construction path.");
        }

        var validContext = new InteractionContext("construction-audit");

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

        AssertCreateRejects("a blank type", () => InteractionEvent.Create(string.Empty, validContext, "component", null));
        AssertCreateRejects("a blank componentId", () => InteractionEvent.Create("audit.event", validContext, string.Empty, null));
        AssertCreateRejects("a context with a blank CorrelationId", () => InteractionEvent.Create("audit.event", new InteractionContext(string.Empty), "component", null));

        var original = InteractionEvent.Create("audit.event", validContext, "component", new { Ok = true });
        var mutated = original with { ComponentId = "component-2" };
        if (mutated.ComponentId != "component-2" || mutated.EventId != original.EventId)
        {
            throw new InvalidOperationException("InteractionEvent no longer supports `with`-expressions after tightening its constructor.");
        }

        return true;
    }

    private static readonly Type[] DeclaredPropertyOwnerTypes =
    [
        typeof(EtherButton),
        typeof(EtherDropdown),
        typeof(EtherProgressBar),
        typeof(EtherSegmentPanel),
        typeof(EtherSegmentedControl),
        typeof(EtherSlider),
        typeof(EtherSteeringBar),
        typeof(EtherMasthead),
    ];

    private static object? CreatePropertySample(Type type, string propertyName) => (type.Name, propertyName) switch
    {
        (nameof(EtherButton), nameof(EtherButton.LeftIcon)) => new SymbolIcon(Symbol.Accept),
        (nameof(EtherButton), nameof(EtherButton.RightIcon)) => new SymbolIcon(Symbol.Cancel),
        (nameof(EtherButton), nameof(EtherButton.Variant)) => EtherButtonVariant.Tertiary,
        (nameof(EtherButton), nameof(EtherButton.Size)) => EtherButtonSize.Small,
        (nameof(EtherDropdown), nameof(EtherDropdown.MaxVisibleItems)) => 4,
        (nameof(EtherDropdown), nameof(EtherDropdown.MenuGap)) => 8d,
        (nameof(EtherProgressBar), nameof(EtherProgressBar.Title)) => "Audit progress",
        (nameof(EtherProgressBar), nameof(EtherProgressBar.ValueContent)) => "42%",
        (nameof(EtherProgressBar), nameof(EtherProgressBar.ShowTitle)) => true,
        (nameof(EtherProgressBar), nameof(EtherProgressBar.ShowValue)) => true,
        (nameof(EtherSegmentPanel), nameof(EtherSegmentPanel.Spacing)) => 6d,
        (nameof(EtherSegmentedControl), nameof(EtherSegmentedControl.SelectedValue)) => "audit-selected",
        (nameof(EtherSlider), nameof(EtherSlider.ShowTitle)) => true,
        (nameof(EtherSlider), nameof(EtherSlider.Title)) => "Audit slider",
        (nameof(EtherSlider), nameof(EtherSlider.ShowLabels)) => true,
        (nameof(EtherSlider), nameof(EtherSlider.Labels)) => new EtherSliderLabelCollection { "0", "50", "100" },
        (nameof(EtherSlider), nameof(EtherSlider.Stops)) => new DoubleCollection { 10d, 50d, 90d },
        (nameof(EtherSlider), nameof(EtherSlider.SnapToStops)) => true,
        (nameof(EtherSteeringBar), nameof(EtherSteeringBar.Minimum)) => 10d,
        (nameof(EtherSteeringBar), nameof(EtherSteeringBar.Maximum)) => 120d,
        (nameof(EtherSteeringBar), nameof(EtherSteeringBar.Value)) => 42d,
        (nameof(EtherSteeringBar), nameof(EtherSteeringBar.Stops)) => new DoubleCollection { 10d, 42d, 90d },
        (nameof(EtherSteeringBar), nameof(EtherSteeringBar.SnapToStops)) => true,
        (nameof(EtherSteeringBar), nameof(EtherSteeringBar.ShowStops)) => false,
        (nameof(EtherSteeringBar), nameof(EtherSteeringBar.SmallChange)) => 2d,
        (nameof(EtherSteeringBar), nameof(EtherSteeringBar.LargeChange)) => 20d,
        (nameof(EtherSteeringBar), nameof(EtherSteeringBar.Title)) => "Audit steering",
        (nameof(EtherSteeringBar), nameof(EtherSteeringBar.ValueContent)) => "42%",
        (nameof(EtherSteeringBar), nameof(EtherSteeringBar.ShowTitle)) => true,
        (nameof(EtherSteeringBar), nameof(EtherSteeringBar.ShowValue)) => true,
        (nameof(EtherMasthead), nameof(EtherMasthead.ShowSettings)) => false,
        (nameof(EtherMasthead), nameof(EtherMasthead.ShowSearch)) => true,
        (nameof(EtherMasthead), nameof(EtherMasthead.ShowMenuIcon)) => true,
        (nameof(EtherMasthead), nameof(EtherMasthead.ShowChevron)) => true,
        (nameof(EtherMasthead), nameof(EtherMasthead.EnableWindowCommands)) => false,
        _ => throw new InvalidOperationException($"No property-audit sample is registered for {type.Name}.{propertyName}."),
    };

    private static bool ValuesMatch(object? expected, object? actual)
    {
        if (ReferenceEquals(expected, actual) || Equals(expected, actual))
        {
            return true;
        }

        return expected is IEnumerable<object?> expectedItems && actual is IEnumerable<object?> actualItems &&
            expectedItems.SequenceEqual(actualItems);
    }

    private static int VerifyStandardInteractionAdapters(
        ControlInteractionAdapter adapter,
        List<InteractionEvent> produced,
        InteractionContext context,
        EtherButton button,
        EtherCheckbox checkbox,
        EtherRadioButton radioButton,
        EtherInput input,
        EtherDropdown dropdown,
        EtherSegmentedControl segmentedControl,
        EtherIntelligenceButton intelligenceButton,
        EtherProgressBar progressBar,
        EtherSteeringBar steeringBar,
        EtherSlider slider,
        ToggleSwitch toggleSwitch,
        ScrollBar scrollBar)
    {
        var verified = 0;
        VerifyInteraction(adapter, produced, context, "button.clicked", "button", () => Invoke(button), sink => sink.ObserveButton(button, "button.clicked", context, "button")); verified++;
        VerifyInteraction(adapter, produced, context, "checkbox.changed", "checkbox", () => checkbox.IsChecked = checkbox.IsChecked != true, sink => sink.ObserveToggle(checkbox, "checkbox.changed", context, "checkbox")); verified++;
        radioButton.IsChecked = false;
        VerifyInteraction(adapter, produced, context, "radio.changed", "radio", () => radioButton.IsChecked = true, sink => sink.ObserveToggle(radioButton, "radio.changed", context, "radio")); verified++;
        input.IsEnabled = true;
        input.Text = string.Empty;
        VerifyInteraction(adapter, produced, context, "input.changed", "input", () => input.Text = "backend audit", sink => sink.ObserveInput(input, "input.changed", context, "input")); verified++;
        dropdown.SelectedIndex = 0;
        VerifyInteraction(adapter, produced, context, "dropdown.changed", "dropdown", () => dropdown.SelectedIndex = Math.Min(1, dropdown.Items.Count - 1), sink => sink.ObserveDropdown(dropdown, "dropdown.changed", context, "dropdown")); verified++;
        segmentedControl.SelectedValue = "A";
        VerifyInteraction(adapter, produced, context, "segmented.changed", "segmented", () => segmentedControl.SelectedValue = "B", sink => sink.ObserveSegmentedControl(segmentedControl, "segmented.changed", context, "segmented")); verified++;
        intelligenceButton.IsEnabled = true;
        intelligenceButton.IsHitTestVisible = true;
        intelligenceButton.IsTabStop = true;
        VerifyInteraction(adapter, produced, context, "intelligence.clicked", "intelligence", () => Invoke(intelligenceButton), sink => sink.ObserveButton(intelligenceButton, "intelligence.clicked", context, "intelligence")); verified++;
        progressBar.Value = 0d;
        VerifyInteraction(adapter, produced, context, "progress.changed", "progress", () => progressBar.Value = 43d, sink => sink.ObserveRange(progressBar, "progress.changed", context, "progress")); verified++;
        steeringBar.Value = 0d;
        VerifyInteraction(adapter, produced, context, "steering.changed", "steering", () => steeringBar.Value = 43d, sink => sink.ObserveSteeringBar(steeringBar, "steering.changed", context, "steering")); verified++;
        slider.Value = 0d;
        VerifyInteraction(adapter, produced, context, "slider.changed", "slider", () => slider.Value = 43d, sink => sink.ObserveRange(slider, "slider.changed", context, "slider")); verified++;
        toggleSwitch.IsOn = false;
        VerifyInteraction(adapter, produced, context, "switch.changed", "switch", () => toggleSwitch.IsOn = true, sink => sink.ObserveSwitch(toggleSwitch, "switch.changed", context, "switch")); verified++;
        scrollBar.Value = 0d;
        VerifyInteraction(adapter, produced, context, "scrollbar.changed", "scrollbar", () => scrollBar.Value = 20d, sink => sink.ObserveRange(scrollBar, "scrollbar.changed", context, "scrollbar")); verified++;
        return verified;
    }

    private static void VerifyInteraction(
        ControlInteractionAdapter adapter,
        List<InteractionEvent> produced,
        InteractionContext context,
        string eventType,
        string componentId,
        Action trigger,
        Func<ControlInteractionAdapter, IDisposable> observe)
    {
        var countBefore = produced.Count;
        using var subscription = observe(adapter);
        trigger();
        var interaction = produced.Skip(countBefore).LastOrDefault(item => item.Type == eventType && item.ComponentId == componentId);
        if (interaction is null || interaction.Context != context || interaction.Data.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            throw new InvalidOperationException($"The standard interaction adapter did not produce a consumable '{eventType}' envelope.");
        }
    }

    private static void Invoke(Button button)
    {
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button)
            ?? throw new InvalidOperationException($"{button.GetType().Name} did not create an automation peer for click consumption.");
        if (peer.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
        {
            throw new InvalidOperationException($"{button.GetType().Name} did not expose the WinUI Invoke pattern.");
        }

        invoke.Invoke();
    }
}
