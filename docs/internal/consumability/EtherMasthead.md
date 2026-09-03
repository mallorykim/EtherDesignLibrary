# Consumability review — Masthead

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherMasthead` (`: Control`) —
> [EtherMasthead.xaml.cs](../../../src/Ether.DesignSystem.Controls/Controls/Navigation/EtherMasthead.xaml.cs),
> [EtherMasthead.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Navigation/EtherMasthead.xaml),
> [MastheadAction.cs](../../../src/Ether.DesignSystem.Controls/Controls/Navigation/MastheadAction.cs).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-03 (post-reskin re-cite; base verdict
> per 2026-09-01 R3).
>
> **Post-reskin re-cite (2026-09-03, commit `fc88e83` — font-glyph caption buttons, 46×40 flush
> cells, red close-hover):** the reskin did **not** change the reviewed contract — the template part
> names, resource keys, the public `ActionInvoked` event, and the `EtherButton` caption structure are
> all intact, so every verdict below stands. Only source line numbers drifted. Current anchors,
> re-verified against HEAD: base `public sealed class EtherMasthead : Control` (`EtherMasthead.xaml.cs:46`);
> `public event EventHandler<MastheadActionInvokedEventArgs>? ActionInvoked` (`EtherMasthead.xaml.cs:89`),
> raised at `EtherMasthead.xaml.cs:337` (Minimize) / `:355` (MaximizeRestore) / `:379` (Close); the
> three caption buttons remain `controls:EtherButton` — `MinimizeButton` (`EtherMasthead.xaml:347`),
> `MaximizeRestoreButton` (`:352`), `CloseButton` (`:357`); the decorative `SettingsButton` (`:321`)
> and `SearchIconSlot` (`:308`) slots stay non-button `Grid`s. The older `EtherMasthead.xaml.cs:120`
> / `:531-590` / `:543,561,585` and `EtherMasthead.xaml:261-330` citations in the body predate
> `fc88e83` and have shifted to these anchors; the harness template-part contract
> (`Verify-ConsumerFixtures.ps1:987` `SearchIconSlot`/`SettingsButton`/`MinimizeButton`/
> `MaximizeRestoreButton`/`CloseButton`) is unaffected.

## Verdict at a glance
- **Base-control parity (B):** the confirmed base is bare `Control`
  (`EtherMasthead.xaml.cs:57`), so there is still no inherited masthead action/command surface. The
  component composes three `EtherButton` caption parts (`EtherMasthead.xaml:261-330`); their
  handlers now also raise a public event before touching the host window
  (`EtherMasthead.xaml.cs:531-590`).
- **Design coverage (B):** all required `ShowSettings`/`ShowSearch`/`ShowMenuIcon`/`ShowChevron` and
  `EnableWindowCommands` knobs are public DPs with CLR wrappers
  (`EtherMasthead.xaml.cs:123-179,198-213`) and drive the optional-icon states
  (`EtherMasthead.xaml.cs:310-316`, unchanged). Decision D2 is resolved: the caption action gap is
  closed by a new public `ActionInvoked` event carrying a `MastheadAction` enum
  (`MastheadAction.cs:1-27`; `EtherMasthead.xaml.cs:120`), raised before the host-window command in
  each caption handler (`…xaml.cs:543,561,585`); the menu/search/settings/chevron icon slots remain
  🚫 decorative by design, now documented in the class remarks
  (`EtherMasthead.xaml.cs:25-27`).
- **Consumability (A):** R1–R3 are fixture-covered for all five pre-existing DPs
  (`RuntimeVerification.PropertyConsumption.cs:270-281,321-327`), C2 now passes — Masthead is in
  the standard-interaction list, and `ObserveMasthead` is exercised end to end: it clicks the real
  `MaximizeRestoreButton` through UIA Invoke and asserts the adapter emits an `Action` field in its
  envelope (`RuntimeVerification.PropertyConsumption.cs:388-405`). C4 is proven for that same
  caption button through the same test (the click only succeeds because
  `MaximizeRestoreButton` exposes `PatternInterface.Invoke`/`IInvokeProvider` — `…PropertyConsumption.cs:432-441`).
  `RuntimeVerification.Masthead.cs` now also individually asserts `MinimizeButton` and
  `CloseButton` each expose `PatternInterface.Invoke`/`IInvokeProvider` — via
  `VerifyMastheadCaptionButtonInvokePatterns`, called from `VerifyMastheadAsync` — WITHOUT actually
  invoking either (invoking them would minimize/close the fixture's real host window mid-run).
  **Confirmed by a green run:** `Verify-ConsumerFixtures.ps1` reached
  `{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success", ...}`
  (`artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`), whose
  `masthead` record carries `MinimizeButtonInvokeExposed: true` and
  `CloseButtonInvokeExposed: true`.

**Overall: consumer-ready.** Both D2 action-surface items are closed; all four of Review A's follow-ups now have fixture assertions — the fourth (C4) is fully closed: `MaximizeRestoreButton`'s Invoke pattern is proven by actually clicking it (adapter test), and `MinimizeButton`/`CloseButton`'s Invoke pattern is proven by pattern-exposure assertions that deliberately do not click them. CLOSED, confirmed by the green run above (`masthead.MinimizeButtonInvokeExposed`, `CloseButtonInvokeExposed`).

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| `ShowSettings` | design | show/hide settings affordance | DP `EtherMasthead.xaml.cs:123-134`; state target `EtherMasthead.xaml:140-150` | ✔ DP + callback | ✅ |
| `ShowSearch` | design | show/hide search affordance | DP `EtherMasthead.xaml.cs:138-149`; state target `EtherMasthead.xaml:152-162` | ✔ DP + callback | ✅ |
| `ShowMenuIcon` | design | show/hide menu affordance | DP `EtherMasthead.xaml.cs:153-164`; state target `EtherMasthead.xaml:164-174` | ✔ DP + callback | ✅ |
| `ShowChevron` | design | show/hide chevron affordance | DP `EtherMasthead.xaml.cs:168-179`; state target `EtherMasthead.xaml:176-186` | ✔ DP + callback | ✅ |
| `EnableWindowCommands` | design | allow/suppress host minimize/maximize/restore/close | DP `EtherMasthead.xaml.cs:198-213`; checked by all caption handlers `…xaml.cs:537-590` | ✔ DP | ✅ |
| `ActionInvoked` event (D2) | design | listen/route minimize/maximize/restore/close → backend | ✔ new public `event EventHandler<MastheadActionInvokedEventArgs> ActionInvoked` `EtherMasthead.xaml.cs:120`; raised from each caption handler before the host command `…xaml.cs:543,561,585`; `MastheadAction` enum + public args `MastheadAction.cs:1-27` | event + `ObserveMasthead` `ControlInteractionAdapter.cs:115-126` | ✅ **closed this wave (D2)** |
| settings/search/menu/chevron action | design | listen/command the visible affordances → backend | ✗; template uses non-button `Grid`s `EtherMasthead.xaml:209-259` | n/a | 🚫 **documented decorative-by-design (D2)** — `EtherMasthead.xaml.cs:25-27`: "place an interactive control beside the masthead for those actions" |
| host window resolution | design | target the containing window without app-specific references | internal `XamlRoot` → `AppWindow` lookup `EtherMasthead.xaml.cs:264-303` | n/a | ✅ |
| maximize/restore state | design | show the correct caption glyph as host state changes | internal AppWindow change subscription `EtherMasthead.xaml.cs:264-289`; VSM update `…xaml.cs:318-326` | internal observable state | ✅ |
| `Background` / sizing / alignment | base | style/layout the composite control | inherited `Control` surface; template binds `Background` `EtherMasthead.xaml:130-138` | ✔ DP | ✅ |
| `IsEnabled` | base | disable the composite's interactions | inherited; caption parts are child controls `EtherMasthead.xaml:261-330` | ✔ DP, behavior unverified | ✅ (unverified by fixture) |

**Notes / gaps (B):**
- **Every named toggle in the brief is a bindable DP.** The five public identifiers register on
  `EtherMasthead`, their wrappers use `GetValue`/`SetValue`, and the four visibility properties
  share `OnOptionalIconPropertyChanged` (`EtherMasthead.xaml.cs:123-179,198-213,236-242`). The
  declared-property fixture supplies a sample for each (`PropertyConsumption.cs:321-327`).
- **D2 is fully closed: the caption action gap now has a public event.** `ActionInvoked` fires with
  a `MastheadAction` (`Minimize`/`MaximizeRestore`/`Close`) before the presenter/close call
  (`EtherMasthead.xaml.cs:543,561,585`), gated the same way the host command already was — behind
  `EnableWindowCommands` (`…xaml.cs:538-541,549-557,580-583`). `ObserveMasthead` subscribes to it
  and emits `{ Action }` (`ControlInteractionAdapter.cs:115-126`).
- **The menu/search/settings/chevron slots remain intentionally decorative (D2) — this is now
  written down, not merely observed.** The class remarks state it explicitly:
  "menu, search, settings, and chevron icon slots are decorative affordances; place an interactive
  control beside the masthead for those actions" (`EtherMasthead.xaml.cs:25-27`). No code change
  was made to those slots, matching the locked decision.

---

## Review A — Consumability report

```
Component: EtherMasthead
Base control: Control   | Package: Ether.DesignSystem.Controls
Declared DPs: 5  (ShowSettings, ShowSearch, ShowMenuIcon, ShowChevron,
                  EnableWindowCommands)  | inherited public surface: Control
Public event: ActionInvoked (EventHandler<MastheadActionInvokedEventArgs>)

Rubric (declared DPs):
  R1 DependencyProperty ....... PASS  (five public identifiers + wrappers,
                                       EtherMasthead.xaml.cs:123-213)
  R2 Round-trips .............. PASS  (standard GetValue/SetValue wrappers, cs:131-213;
                                       fixture asserts both paths, PropertyConsumption.cs:62-97)
  R3 Observable ............... PASS  (fixture registers callbacks, PropertyConsumption.cs:74-91;
                                       all five samples exist at cs:321-327)
  R4 TwoWay ................... N/A   (visibility/command-enable properties are host configuration,
                                       not user-edited state)
  R5 Documented ............... PASS  (all five identifier/wrapper pairs now state their registered
                                       default, e.g. ShowSettings=true, EnableWindowCommands=true,
                                       EtherMasthead.xaml.cs:123-213)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control)
  C2 Listenable + adapter ..... PASS  (public ActionInvoked event + ObserveMasthead,
                                       ControlInteractionAdapter.cs:115-126; exercised end to end —
                                       clicks the real MaximizeRestoreButton and asserts the
                                       adapter's envelope carries an Action field,
                                       PropertyConsumption.cs:388-405; added to
                                       VerifyStandardInteractionAdapters)
  C3 Command .................. N/A   (Masthead is Control, not a button-family base; action
                                       observability is accounted for under C2)
  C4 Automation peer/pattern .. PASS  (the C2 adapter test proves MaximizeRestoreButton
                                       exposes PatternInterface.Invoke via its EtherButton peer by
                                       actually clicking it, PropertyConsumption.cs:432-441;
                                       RuntimeVerification.Masthead.cs also finds MinimizeButton
                                       and CloseButton and asserts each resolves
                                       PatternInterface.Invoke to IInvokeProvider — via
                                       VerifyMastheadCaptionButtonInvokePatterns, called from
                                       VerifyMastheadAsync — deliberately without invoking either.
                                       Confirmed by the green run: masthead.MinimizeButtonInvokeExposed:true,
                                       CloseButtonInvokeExposed:true in
                                       runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json)

Harness: covered? YES. In PublicPropertyInventoryTypes (PublicPropertyInventory.cs:46),
         DeclaredPropertyOwnerTypes (PropertyConsumption.cs:270-281), and
         RuntimeVerification.Masthead.cs:15-18; now also in VerifyStandardInteractionAdapters
         (PropertyConsumption.cs:342-405 calls the masthead block at 388-405). Runtime marker
         ETHER_CONSUMER_SMOKE outcome:"success" confirms the masthead adapter path executed
         without error (part of the same successful run whose commands/automationPatterns/
         twoWayBindings fields are otherwise cited across this wave's reports)
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
         VerifyMastheadCaptionButtonInvokePatterns was added in a follow-up session and is called
         from VerifyMastheadAsync (itself already part of VerifyAsync's call path); a later
         Verify-ConsumerFixtures.ps1 run confirmed it — its marker
         (artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json,
         outcome:"success") carries masthead.MinimizeButtonInvokeExposed:true and
         CloseButtonInvokeExposed:true.
x64 build: green — confirmed by that same subsequent Verify-ConsumerFixtures.ps1 run, following
           the edit-only session that added the assertion above and the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md.
```

---

## Follow-ups (concrete, actionable)

1. ~~**[B decision] Resolve optional-icon semantics.**~~ DONE (D2) — recorded as decorative in the
   class remarks (`EtherMasthead.xaml.cs:25-27`); no code change, per the locked decision.
2. ~~**[C2, backend] Expose and adapt Masthead actions.**~~ DONE — `ActionInvoked` +
   `MastheadActionInvokedEventArgs` + `MastheadAction` enum (`MastheadAction.cs:1-27`;
   `EtherMasthead.xaml.cs:120`) are raised from each caption handler before the host command
   (`…xaml.cs:543,561,585`); `ObserveMasthead` was added to `ControlInteractionAdapter.cs:115-126`
   and to `VerifyStandardInteractionAdapters` (`PropertyConsumption.cs:388-405`), which asserts the
   emitted action payload.
3. ~~**[C4, harness] Prove the composite automation contract for all three caption buttons.**~~
   DONE. The adapter test in item 2 proves `MaximizeRestoreButton` exposes UIA
   Invoke as a side effect of exercising it (`PropertyConsumption.cs:392-400,432-441`).
   `RuntimeVerification.Masthead.cs` also adds `VerifyMastheadCaptionButtonInvokePatterns`,
   called from `VerifyMastheadAsync`, which finds `MinimizeButton` and `CloseButton` and asserts
   each resolves `PatternInterface.Invoke` to `IInvokeProvider` — deliberately without calling
   `Invoke()` on either (that would minimize/close the fixture's real host window mid-run, unlike
   `MaximizeRestoreButton`'s restore-in-place adapter click). Confirmed by a green
   `Verify-ConsumerFixtures.ps1` run (`masthead.MinimizeButtonInvokeExposed: true`,
   `CloseButtonInvokeExposed: true` in
   `artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`).
4. ~~**[R5] Complete the five DP docs.**~~ DONE — `EtherMasthead.xaml.cs:123-213` now states each
   registered default on both identifier and wrapper, including `EnableWindowCommands=true`.

Remaining: none. Item 3's assertions are wired into `VerifyMastheadAsync` (already part of
`VerifyAsync`'s call path) and have been exercised by an actual `Verify-ConsumerFixtures.ps1` run,
which reached `outcome:"success"` — closed and verified.
