# WinUI 3 + XAML: The AI Agent's Field Guide

**A must-read reference for any LLM/AI agent writing WinUI 3 (Windows App SDK) code in C#/XAML.**

> Sources: Microsoft Learn (`learn.microsoft.com/windows/apps`), the official Windows App SDK "AI-assisted migration" docs, the `microsoft/microsoft-ui-xaml` GitHub repo, and Microsoft's own Copilot instruction/skill files for WinUI 3 (`github/awesome-copilot`, `microsoft/win-dev-skills`). Current as of mid-2026 (Windows App SDK 2.x, Visual Studio 2026, .NET 10).

---

## Pre-Generation Checklist (compact — for agent memory / system prompt)

**Lock these defaults in before writing any line of code.** This is the short version; Section 16 has the full post-generation checklist, and the rest of this document has the "why."

1. **Identity**: Target = WinUI 3 / Windows App SDK. NOT UWP. NOT WPF. All namespaces = `Microsoft.UI.Xaml.*` — never `Windows.UI.Xaml.*`.
2. **Bindings**: Default to `{x:Bind}`. Add `Mode=OneWay` for anything that changes at runtime, `Mode=TwoWay` for editable inputs (default mode is `OneTime` — don't leave it there by accident). `{x:Bind}` targets the code-behind class, not `DataContext`. Every `DataTemplate` using `{x:Bind}` needs `x:DataType`. Source properties need `INotifyPropertyChanged` (or `CommunityToolkit.Mvvm` `[ObservableProperty]` in a `partial` class).
3. **Threading**: UI-thread marshaling = `DispatcherQueue.TryEnqueue(...)` only. Never `CoreDispatcher`/`Dispatcher.RunAsync`. `TryEnqueue` returns `bool`, don't `await` it.
4. **Window**: No `Window.Current` — use a static `App.MainWindow`. Resize/move/titlebar = `AppWindow`, not `ApplicationView`/`CoreWindow`. `InitializeComponent()` is always the first line of a constructor.
5. **Dialogs/Pickers**: Every `ContentDialog` sets `XamlRoot` before `ShowAsync()`. Only one `ContentDialog` open per thread. No `MessageDialog`. Every File/Folder picker calls `InitializeWithWindow.Initialize(picker, hwnd)` first.
6. **Navigation**: `NavigationView` does not auto-navigate — wire `ItemInvoked` → `Frame.Navigate()` and `BackRequested` → `Frame.GoBack()` explicitly.
7. **Styling**: Brushes/colors = `{ThemeResource ...}`, never hardcoded hex. Text = built-in `*TextBlockStyle`, never manual `FontSize`/`FontWeight`. Spacing = multiples of 4px.
8. **Errors**: Wrap every `async void` event handler body in `try/catch`.
9. **Packaging**: Don't assume `ApplicationData.Current` works — it only works in packaged apps.
10. **File hygiene**: Write `.cs`/`.xaml` files with CRLF line endings (or ensure `.gitattributes`/`.editorconfig` normalize them) — LF-only files trigger Visual Studio's "inconsistent line endings" prompt and noisy diffs.
11. **When unsure**: Don't guess from a remembered UWP/WPF API. Flag the uncertainty instead of inventing a plausible-looking call.

---

## 0. How to use this document

This guide exists because **LLMs are trained on years of UWP, WPF, and Windows Forms sample code**, and will silently reproduce those patterns unless explicitly stopped. WinUI 3 looks almost identical to UWP XAML on the surface, but the runtime, namespaces, threading model, and several APIs are meaningfully different. Most "WinUI 3 bugs" produced by AI are not new mistakes — they are **old UWP/WPF habits pasted into the wrong framework**.

Read this top-to-bottom once. Then treat Section 16 ("The Golden Checklist") as a pre-submit checklist for every file you generate.

**Golden Rule:** If you are not 100% sure an API exists in `Microsoft.UI.Xaml.*` (WinUI 3 / Windows App SDK), do not guess. Prefer a documented, verified pattern from this guide over a remembered UWP/WPF pattern that "should probably still work."

---

## 1. What WinUI 3 actually is (and is not)

- **WinUI 3** (Microsoft now just calls it **"WinUI"**; the older "WinUI 3" name and "WinUI 2 = WinUI for UWP" naming still appear everywhere in docs and samples) is the native UI framework shipped as part of the **Windows App SDK**. It is the Microsoft-recommended framework for **new** native Windows desktop apps.
- It runs as a normal **Win32 desktop process**, not inside a UWP app container. This is the single most important architectural fact: WinUI 3 apps are Win32 apps with a modern XAML UI stack, not sandboxed UWP apps.
- It is **not** UWP. It is **not** WPF, even though the XAML syntax looks similar to both. Namespace roots differ, the threading object differs, the windowing model differs, and several controls/APIs simply don't exist or behave differently.
- It supports **C# (.NET)** and **C++/WinRT**. This guide assumes C#.
- Supported OS: Windows 10 version 1809 (build 17763) and later, Windows 11 recommended.
- UWP is now in **maintenance mode**. Do not start new projects in UWP. If asked to "modernize" a UWP app, migrate it to WinUI 3 (see Section 15).

### 1.1 The #1 rule: namespace roots

| Framework | Root XAML namespace | Root C# namespace |
|---|---|---|
| UWP | `Windows.UI.Xaml.*` | `Windows.UI.Xaml` |
| **WinUI 3 (Windows App SDK)** | **`Microsoft.UI.Xaml.*`** | **`Microsoft.UI.Xaml`** |
| WPF | `System.Windows.*` | `System.Windows` |

**NEVER emit `Windows.UI.Xaml.*` in a WinUI 3 project.** This is the single most common AI-generated bug in WinUI 3 code. If you see `using Windows.UI.Xaml;` or `xmlns:local="using:Windows.UI.Xaml.Controls"` anywhere in generated code for a WinUI 3 project, it is wrong — full stop.

Full namespace mapping:

| UWP namespace | WinUI 3 namespace |
|---|---|
| `Windows.UI.Xaml` | `Microsoft.UI.Xaml` |
| `Windows.UI.Xaml.Controls` | `Microsoft.UI.Xaml.Controls` |
| `Windows.UI.Xaml.Media` | `Microsoft.UI.Xaml.Media` |
| `Windows.UI.Xaml.Input` | `Microsoft.UI.Xaml.Input` |
| `Windows.UI.Xaml.Data` | `Microsoft.UI.Xaml.Data` |
| `Windows.UI.Xaml.Navigation` | `Microsoft.UI.Xaml.Navigation` |
| `Windows.UI.Xaml.Shapes` | `Microsoft.UI.Xaml.Shapes` |
| `Windows.UI.Xaml.Markup` | `Microsoft.UI.Xaml.Markup` |
| `Windows.UI.Composition` | `Microsoft.UI.Composition` |
| `Windows.UI.Input` | `Microsoft.UI.Input` |
| `Windows.UI.Colors` | `Microsoft.UI.Colors` |
| `Windows.UI.Text` | `Microsoft.UI.Text` |
| `Windows.UI.Core` (dispatcher use) | `Microsoft.UI.Dispatching` |

APIs that **do not change** namespace (they are general WinRT APIs, not XAML APIs): `Windows.Storage.*` (files, `ApplicationData`), `Windows.Devices.*`, `Windows.Media.*`, `Windows.UI.ViewManagement.UISettings`, `Windows.UI.Color` (the struct, not the XAML `Colors` class), `Windows.Foundation.*`. Most non-XAML WinRT APIs are unchanged.

---

## 2. Project setup

### 2.1 Project file essentials

A modern WinUI 3 project targets .NET and the Windows App SDK. Key MSBuild properties an AI agent must get right:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <RootNamespace>MyApp</RootNamespace>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <UseWinUI>true</UseWinUI>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableMsixTooling>true</EnableMsixTooling>
    <WindowsPackageType>MSIX</WindowsPackageType> <!-- or "None" for unpackaged -->
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.*" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.*" />
  </ItemGroup>
</Project>
```

- `<UseWinUI>true</UseWinUI>` is **mandatory**. Without it, the XAML compiler will not run correctly and you'll get bizarre build errors about missing generated partial classes.
- The TFM (`net10.0-windows10.0.19041.0` or similar) must include a `-windows10.0.XXXXX.0` suffix. Forgetting the Windows suffix (e.g. just `net10.0`) is a common AI mistake — it produces a console-style project with no WinRT/XAML projection.
- Old UWP projects use `<TargetPlatformVersion>` / `<TargetPlatformMinVersion>` in a `.wapproj`/appxmanifest style. **Do not port that pattern directly.** WinUI 3 SDK-style projects use the TFM approach shown above.
- `Microsoft.WindowsAppSDK` is the required NuGet package. Without it, none of `Microsoft.UI.Xaml.*` exists.

### 2.2 Two legitimate creation paths

1. **Visual Studio 2026** with the "WinUI application development" workload — templates like *Blank App, Packaged (WinUI 3 in Desktop)*. Handles MSIX packaging/signing automatically.
2. **Command line**: `dotnet new winui...` templates (via .NET 10 SDK) + `dotnet run`. The `Microsoft.Windows.SDK.BuildTools.WinApp` package handles debug identity automatically for local runs; packaging for distribution needs extra steps (see Section 14).

Both are valid — don't assume Visual Studio is the only way, but do assume **MSBuild** (not plain `csc`/`dotnet build` tricks) is required to compile XAML, because the XAML compiler is an MSBuild task.

### 2.3 Line endings — write CRLF, not LF

**Symptom:** Open an AI-touched `.cs` or `.xaml` file in Visual Studio and it pops a dialog reporting inconsistent line endings, offering to normalize the file — usually to CRLF (`\r\n`, "PC" line endings).

**Why it happens:** AI agents (and most Linux/cross-platform tooling) default to writing files with LF-only (`\n`) endings. Windows/.NET convention — and Visual Studio's own save behavior — is CRLF for `.cs`/`.xaml`/`.resw`. A file with LF, or a file with a mix of both (e.g., new lines an agent appended to an existing CRLF file), triggers the prompt. It's not a compile error — XAML and C# parse fine either way — but left unmanaged it causes noisy git diffs (whole files flagged as "changed") and repeated prompts every time a human opens a file the agent touched.

**Fix — don't rely on the dialog, normalize at the source:**

```gitattributes
# .gitattributes (repo root)
* text=auto
*.cs   text eol=crlf
*.xaml text eol=crlf
*.resw text eol=crlf
```

```ini
# .editorconfig (repo root)
root = true

[*.{cs,xaml}]
end_of_line = crlf
```

Set **both** files, not just one: `.gitattributes` controls what git stores/checks out; `.editorconfig` controls what editors write on save. Either alone leaves a gap. If your file-writing tool lets you specify line endings directly, just write CRLF for `.cs`/`.xaml` in the first place and skip the dialog entirely.

### 2.4 File structure of a typical page

Every XAML page/window/usercontrol is a **partial class split across two files**:

```
MainWindow.xaml       <- markup, declares x:Class="MyApp.MainWindow"
MainWindow.xaml.cs     <- code-behind, "public sealed partial class MainWindow : Window"
```

The XAML compiler generates a third, hidden file (`MainWindow.g.cs` in `obj/`) that implements `InitializeComponent()` and declares fields for every named (`x:Name`) element. **`InitializeComponent()` must be the first call in the constructor**, before touching any named XAML element, or those elements will be `null`:

```csharp
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent(); // MUST be first — wires up x:Name fields
        // Safe to touch named elements only after this line.
        Title = "My App";
    }
}
```

A very common AI mistake: referencing `x:Name`'d elements or setting properties on them in field initializers or before `InitializeComponent()` runs. This throws `NullReferenceException` at runtime, not a compile error.

---

## 3. XAML syntax fundamentals (and the traps)

### 3.1 Root element namespaces

A correct WinUI 3 XAML root looks like this — **memorize this block**:

```xml
<Window
    x:Class="MyApp.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:MyApp"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

    <Grid>
        <TextBlock Text="Hello, WinUI 3" />
    </Grid>
</Window>
```

Note what is **the same** as UWP/WPF (the `xmlns` URIs themselves — `presentation` and the `x` schema — are unchanged; only the *type resolution behind them* points at `Microsoft.UI.Xaml` assemblies instead of `Windows.UI.Xaml` ones because of the project's SDK references). You do **not** need to (and should not) put `Microsoft.UI.Xaml` literally in the default `xmlns` — the default presentation namespace URI is correct as-is. The mistake to avoid is in **custom `using:` clauses** and in **code-behind `using` statements**, where AI models frequently default to `Windows.UI.Xaml.Controls` out of habit.

Root element types you'll declare `x:Class` against:
- `<Window x:Class="...">` — a top-level window (most common root today).
- `<Page x:Class="...">` — a navigable unit hosted inside a `Frame` (used with `NavigationView`).
- `<UserControl x:Class="...">` — a reusable composite control.
- `<ContentDialog x:Class="...">` — a custom XAML-authored dialog (see Section 8).
- `<Application x:Class="...">` — `App.xaml`.

### 3.2 Markup extension syntax — `{ }`

| Syntax | Meaning |
|---|---|
| `{Binding Path}` | Classic reflection-based binding (see Section 6). |
| `{x:Bind Path}` | Compiled binding, resolved at build time (see Section 6). **Preferred.** |
| `{StaticResource Key}` | Resolve once, at load time. Does not update on theme change. |
| `{ThemeResource Key}` | Resolve at load time **and** re-resolve when the app theme (Light/Dark/HighContrast) changes. **Use for all brushes/colors.** |
| `{x:Null}` | Explicit null value. |
| `{x:Type local:MyClass}` | A `Type` reference (rare; mostly for `DataTemplateSelector` scenarios). |
| `{RelativeSource Self}` | Bind to a property on the same element (works with `{Binding}`; `{x:Bind}` has its own relative-source patterns). |

**Curly-brace escaping**: if a literal string value must start with `{`, escape it: `Text="{}{This is literal}"`.

### 3.3 Attached properties

Attached-property syntax (`Owner.Property="value"`) is unchanged from UWP/WPF:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>

    <TextBlock Grid.Row="0" Text="Header" />
    <ContentPresenter Grid.Row="1" />
</Grid>
```

Common mistake: forgetting that `*` and `Auto` sizing require `RowDefinitions`/`ColumnDefinitions` to actually be declared — `Grid.Row="1"` on an element does nothing useful if the `Grid` only has one implicit row.

### 3.4 Property-element syntax vs. attribute syntax

Simple values go in attributes. Complex objects (styles, templates, gradients, collections) go in **property-element** syntax:

```xml
<Button Content="Save">
    <Button.Background>
        <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
            <GradientStop Color="#FF3B82F6" Offset="0" />
            <GradientStop Color="#FF1D4ED8" Offset="1" />
        </LinearGradientBrush>
    </Button.Background>
</Button>
```

### 3.5 `x:Name` vs `x:Key`

- `x:Name` — gives a field name to an element **in the visual tree** (page/window/control), generated as a private field via `InitializeComponent()`. Use for elements you reference from code-behind.
- `x:Key` — gives a lookup key to an object **inside a `ResourceDictionary`** (a `Style`, `Brush`, `DataTemplate`, converter instance, etc.). Never used on visual-tree elements you want to reference directly by field.

A `Style` with `x:Key="MyStyle"` must be referenced via `{StaticResource MyStyle}`; a `Style` with **no key** but a `TargetType` becomes the **implicit default style** for that control type app-wide — this is intentional in some cases but a very common accidental bug (an AI writes an un-keyed `Style TargetType="Button"` intending it for one button, and it silently restyles every `Button` in the file/app).

### 3.6 Comments

`<!-- like this -->`. XAML has no single-line `//`-style comment. Do not emit `//` inside XAML markup — it will either be ignored as invalid text or cause a parse error depending on context.

### 3.7 Common XAML compile-time errors and their real cause

| Error message (paraphrased) | Real cause |
|---|---|
| "The name 'X' does not exist in the current context" (in `.xaml.cs`) | You referenced an `x:Name` that's misspelled, missing, or inside an `x:Load="False"` / template region the generator didn't emit a named field for (fields inside `DataTemplate`s are **not** promoted to code-behind fields). |
| "The property 'X' was not found" / "Unknown member" | Wrong namespace prefix, or the property doesn't exist on that WinUI 3 type (it may only exist in WPF or UWP). |
| "Type 'X' was not found" for a custom control | Missing/incorrect `xmlns:local="using:MyApp.Controls"` clause, or the control's namespace/class doesn't match. |
| `InitializeComponent` missing / not generated | The `.xaml` file's Build Action isn't `Page`, or `x:Class` in XAML doesn't match the code-behind's namespace + class name exactly (case-sensitive). |
| "Multiple root objects" or "Only one x:Class allowed" | Two files reference the same `x:Class`, or a copy/paste left a duplicate root element. |
| Binding fails silently at runtime (nothing shown, no crash) | This is `{Binding}`'s signature failure mode — reflection binding **swallows** path errors. Check the Visual Studio **Output** window for `BindingExpression path error` diagnostics; nothing appears in the XAML itself. `{x:Bind}` instead fails at **compile time**, which is one of the reasons it's preferred. |

---

## 4. Data binding: `{x:Bind}` vs `{Binding}`

This is the area where AI models most reliably introduce silent, hard-to-diagnose bugs, because the two markup extensions have **different default modes and different default sources**.

### 4.1 The critical differences table

| Aspect | `{x:Bind}` | `{Binding}` |
|---|---|---|
| Resolution | **Compile-time** — generates real C# code in a `.g.cs` partial class. | **Runtime** — resolved via reflection. |
| Default source | The **code-behind class** of the page/window/control (the `x:Class` root), i.e., `this`. | The element's `DataContext`. |
| Default `Mode` | **`OneTime`** | **`OneWay`** |
| Errors | Caught by the **compiler** — typos in the path are build errors. | Silent at runtime; only visible as debug-output warnings. |
| Performance | Fast — no reflection. **Required** for Native AOT (`{Binding}` does not work at all under Native AOT). | Slower — reflection on every evaluation. |
| Binding to methods/functions | Supported natively (see 4.4). | Requires an `IValueConverter`. |
| Binding to private/internal members | Supported (generated code lives in the same partial class). | Not supported — reflection only sees public members by default. |
| Works in `Style` setters | **No** (a long-standing limitation; you cannot `{x:Bind}` inside a `Setter`). | Historically also not supported directly in `Style` setters in WinUI 3 either — this needs a workaround (attached-property helper or code-behind), it is **not** simply "use `{Binding}` instead." |

### 4.2 The #1 `{x:Bind}` trap: default mode is `OneTime`

```xml
<!-- ❌ WRONG (probably): silently never updates when ViewModel.Status changes -->
<TextBlock Text="{x:Bind ViewModel.Status}" />

<!-- ✅ CORRECT: updates when Status changes and raises PropertyChanged -->
<TextBlock Text="{x:Bind ViewModel.Status, Mode=OneWay}" />
```

If the AI is porting XAML from a codebase (or from training data) that used `{Binding}` (default `OneWay`) and mechanically swaps it to `{x:Bind}` without adding `Mode=OneWay`, the UI will **build fine and look correct at first render**, then silently stop updating. This is the classic "works once, then freezes" bug.

**Rule of thumb for AI-generated XAML:**
- Any property that is **read-only display data from a ViewModel that changes at runtime** → `Mode=OneWay`, and the source property **must** raise `INotifyPropertyChanged.PropertyChanged` (or be a `DependencyProperty`).
- Any property that is **truly static after first load** (rare) → `Mode=OneTime` is fine (and is the implicit default, so it can be omitted).
- Any two-way editable control value (`TextBox.Text`, `ToggleSwitch.IsOn`, slider values, etc.) bound to a settings/edit ViewModel → `Mode=TwoWay`.

### 4.3 The #2 `{x:Bind}` trap: default source is code-behind, not `DataContext`

```xml
<!-- If MainWindow.xaml.cs exposes: public MainViewModel ViewModel { get; } -->
<TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}" />
```

This binds to `this.ViewModel.Title` where `this` is the `MainWindow` instance — **not** to `DataContext.Title`. You do not need to set `DataContext` at all for `{x:Bind}` to work; in fact, many WinUI 3 MVVM codebases never set `DataContext`. If code (often copy-pasted from WPF habits) does `this.DataContext = viewModel;` and then uses `{x:Bind}` expecting it to read from `DataContext`, that's a bug — `{x:Bind}` does not consult `DataContext` unless you explicitly set up a relative binding to it.

If you truly need `DataContext`-based resolution (e.g., because a reusable `DataTemplate` is applied against arbitrary item types), use `{Binding}` for that specific case, or set `x:DataType` explicitly (see 4.5) and pass the right object down.

### 4.4 Function bindings (a `{x:Bind}`-only superpower)

`{x:Bind}` allows calling a method as the leaf of a binding path — this replaces most `IValueConverter` uses:

```xml
<TextBlock Text="{x:Bind ViewModel.IsBusy, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />

<!-- Equivalent, more idiomatic x:Bind style: -->
<TextBlock Visibility="{x:Bind GetVisibility(ViewModel.IsBusy), Mode=OneWay}" />
```

```csharp
private Visibility GetVisibility(bool isBusy) =>
    isBusy ? Visibility.Visible : Visibility.Collapsed;
```

Rules for function bindings:
- Multiple arguments are comma-separated: `{x:Bind Combine(ViewModel.First, ViewModel.Second)}`.
- Any argument path used in the function call that can change and should trigger re-evaluation must be a real bindable path (property that raises change notifications), and the binding `Mode` must be `OneWay`/`TwoWay` for re-evaluation to happen at all — a function binding with the default `OneTime` mode only ever runs once, exactly like a plain property binding.
- For a **two-way** function binding (e.g., a `TextBox.Text` that both formats a number for display and parses it back), specify a second function via `BindBack`:

```xml
<TextBox Text="{x:Bind ViewModel.Amount, Mode=TwoWay, Converter={StaticResource} }" /> <!-- do not mix Converter with function-binding syntax -->

<!-- Correct two-way function binding: -->
<TextBox Text="{x:Bind FormatAmount(ViewModel.Amount), Mode=TwoWay, BindBack=ParseAmount}" />
```

```csharp
private string FormatAmount(double amount) => amount.ToString("F2");
private void ParseAmount(string text)
{
    if (double.TryParse(text, out var value))
        ViewModel.Amount = value;
}
```

There is a **built-in** `bool`→`Visibility` converter available since Windows 10 1607 for **property** bindings (not function bindings): you can bind `Visibility="{x:Bind ViewModel.IsBusy, Mode=OneWay}"` directly if `IsBusy` is `bool` — no converter class needed — *as long as* the app's minimum target SDK is 14393+ (true for all current WinUI 3 apps). Many AI-written apps unnecessarily hand-roll a `BoolToVisibilityConverter` class; it's not wrong, just redundant for the simple case.

### 4.5 `x:DataType` — required inside `DataTemplate`, optional at page level

```xml
<ListView ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="local:TodoItem">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <CheckBox IsChecked="{x:Bind IsDone, Mode=TwoWay}" />
                <TextBlock Text="{x:Bind Title}" />
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

- `x:DataType` on a `DataTemplate` is **mandatory** for `{x:Bind}` to work inside that template (without it, the compiler doesn't know what type each item is, so `{x:Bind}` inside the template will fail to compile, or silently fall back to weakly-typed behavior in older toolchains — always set it explicitly).
- `x:DataType` on the **Page/UserControl root** is optional; it enables compile-time validation of `{x:Bind}` expressions against a type other than the class itself, but the default (binding against the code-behind class, `this`) works without it.
- The type in `x:DataType` **must match the actual runtime type** of the items in the bound collection, or bindings inside the template silently target the wrong members.

### 4.6 `INotifyPropertyChanged` is not optional for `OneWay`/`TwoWay`

Whatever the source object is (ViewModel, model, etc.), for `Mode=OneWay`/`TwoWay` bindings to actually refresh the UI, the source property must raise change notifications: implement `INotifyPropertyChanged` and call `PropertyChanged` in the setter, or use a `DependencyProperty` (for custom controls). Prefer `CommunityToolkit.Mvvm`'s `[ObservableProperty]` source generator over hand-writing boilerplate (see Section 9).

For `ItemsSource` collections that change (add/remove) at runtime, bind to an `ObservableCollection<T>`, not `List<T>` — `List<T>` does not raise `CollectionChanged`, so `ListView`/`ItemsRepeater` will not notice new/removed items.

---

## 5. Threading: `DispatcherQueue`, not `CoreDispatcher`

`CoreDispatcher` (UWP) **does not exist** in WinUI 3. This is one of the highest-frequency AI mistakes when generating "update UI from a background thread" code.

```csharp
// ❌ WRONG — CoreDispatcher/Dispatcher.RunAsync is a UWP-only API, does not exist in WinUI 3
await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
{
    StatusText.Text = "Done";
});
```

```csharp
// ✅ CORRECT — DispatcherQueue.TryEnqueue
DispatcherQueue.TryEnqueue(() =>
{
    StatusText.Text = "Done";
});

// With explicit priority:
DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
{
    ProgressBar.Value = 100;
});
```

Key facts about `DispatcherQueue`:

- Every `Window`, `Page`, `UserControl`, and most `DependencyObject`-derived XAML types expose a `DispatcherQueue` property once they're associated with a window (i.e., after being loaded into the tree). From code that doesn't have direct access to a XAML element, capture the `DispatcherQueue` reference on the UI thread first (e.g., `this.DispatcherQueue` in the constructor) and hold onto it, then call `TryEnqueue` from a background thread later.
- **`TryEnqueue` returns `bool`, not a `Task`.** It is fire-and-forget by design — it does not await completion of the enqueued work. Do not `await DispatcherQueue.TryEnqueue(...)`; that line won't even compile, since the return type is `bool`. If you need to know the callback ran, use a `TaskCompletionSource` or a synchronization primitive inside the callback.
- Check `DispatcherQueue.HasThreadAccess` to decide whether you're already on the UI thread before dispatching, to avoid unnecessary marshaling.
- **`Window.Current` does not exist in WinUI 3.** In UWP, `Window.Current` was a global static accessor. WinUI 3 has no such static — you must track your own reference to the main window (see Section 7.1).
- **Threading model difference**: UWP used **ASTA** (Application Single-Threaded Apartment) which has built-in reentrancy blocking. WinUI 3 uses a **standard STA**, with **no** built-in reentrancy protection. This means async code that pumps messages (e.g., nested message loops, certain COM interop calls) can re-enter in ways that were impossible under UWP's ASTA. Be more careful with async void handlers and nested awaits that might re-enter UI code than you would be in old UWP samples.

Full UWP→WinUI 3 threading substitution table:

| UWP pattern | WinUI 3 equivalent |
|---|---|
| `CoreDispatcher` | `DispatcherQueue` |
| `Dispatcher.RunAsync(priority, callback)` | `DispatcherQueue.TryEnqueue(priority, callback)` |
| `Dispatcher.HasThreadAccess` | `DispatcherQueue.HasThreadAccess` |
| `CoreDispatcher.ProcessEvents()` | No equivalent — restructure the async flow instead of relying on manual message pumping. |
| `CoreWindow.GetForCurrentThread()` | Not available. Use `DispatcherQueue.GetForCurrentThread()` if you specifically need the queue for the calling thread (e.g., in a background worker that itself owns a dispatcher). |
| `CoreApplication.MainView.CoreWindow.Dispatcher` | `this.DispatcherQueue` (captured from a `Window` or `Page` while on the UI thread). |

---

## 6. Windowing: `AppWindow`, not `ApplicationView`/`CoreWindow`

WinUI 3 windowing is a **combination** of two objects:
1. The **XAML `Window`** class (`Microsoft.UI.Xaml.Window`) — your program-facing object; you create it with `new MainWindow()`, set its `Content`, and call `.Activate()`.
2. The **`AppWindow`** class (`Microsoft.UI.Windowing.AppWindow`) — the lower-level, Win32-HWND-backed object for resize/move/title bar/presenter operations that the XAML `Window` doesn't directly expose.

### 6.1 There is no `Window.Current` — track your own reference

```csharp
// App.xaml.cs
public partial class App : Application
{
    // ✅ Public static property, set once at launch — the WinUI 3 replacement for Window.Current
    public static Window? MainWindow { get; private set; }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
```

Access it anywhere as `App.MainWindow`. Do **not** emit `Window.Current` in WinUI 3 code — it's a UWP-only static and either won't compile or will always be `null`, depending on the API surface referenced.

Also note these `Window` properties are **carried over from UWP for source compatibility but are always `null`/unsupported in WinUI 3**: `Window.Current`, `Window.CoreWindow`, `Window.Dispatcher`. Don't rely on any of them — use `App.MainWindow`, `AppWindow`, and `DispatcherQueue` respectively.

### 6.2 Getting the `AppWindow` from a `Window`

```csharp
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;

var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

appWindow.Resize(new Windows.Graphics.SizeInt32(1000, 700));
appWindow.Move(new Windows.Graphics.PointInt32(100, 100));
appWindow.Title = "My App";
```

Windowing substitution table:

| UWP API | WinUI 3 API |
|---|---|
| `ApplicationView` | `AppWindow` |
| `ApplicationView.GetForCurrentView()` | `AppWindow.GetFromWindowId(windowId)` (obtained via `WindowNative` + `Win32Interop` as above — no direct "current view" concept in a multi-window Win32 app) |
| `CoreWindow` | `Microsoft.UI.Xaml.Window` |
| `ApplicationViewTitleBar` / `CoreApplicationViewTitleBar` | `AppWindowTitleBar` (via `appWindow.TitleBar`) |
| `CoreApplicationView.TitleBar.ExtendViewIntoTitleBar` | `appWindow.TitleBar.ExtendsContentIntoTitleBar` |
| `ApplicationView.TryResizeView()` | `AppWindow.Resize()` |
| `AppWindow.TryCreateAsync()` (old preview API) | `AppWindow.Create()` |
| `AppWindow.TryShowAsync()` | `AppWindow.Show()` |
| `AppWindow.TryConsolidateAsync()` | `AppWindow.Destroy()` |
| `SystemNavigationManager` / back button | Handle back navigation via `NavigationView`'s built-in back button, or `AppWindowTitleBar`; there's no `SystemNavigationManager` singleton in desktop WinUI 3. |
| `UIViewSettings.GetForCurrentView()` | No direct equivalent — read what you need from `AppWindow` properties instead. |
| `DisplayInformation.GetForCurrentView()` | Win32 `GetDpiForWindow()` p/invoke, or `XamlRoot.RasterizationScale` for XAML-space scale factor. |

### 6.3 Custom title bar

```csharp
appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent; // Microsoft.UI.Colors, not Windows.UI.Colors
```

```xml
<Grid x:Name="AppTitleBar" Height="32" Canvas.ZIndex="1">
    <TextBlock Text="My App" VerticalAlignment="Center" Margin="16,0,0,0" />
</Grid>
```

```csharp
this.InitializeComponent();
appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
SetTitleBar(AppTitleBar); // Window.SetTitleBar — designates the drag region
```

Do not use `CoreApplicationViewTitleBar` — it does not exist in WinUI 3.

### 6.4 Backdrops: Mica and Acrylic

```xml
<Window ...>
    <Window.SystemBackdrop>
        <MicaBackdrop />
        <!-- or: <DesktopAcrylicBackdrop /> for transient surfaces -->
    </Window.SystemBackdrop>
    ...
</Window>
```

- Use **Mica** for the main app window background (it's the modern Windows 11 default look).
- Use **Acrylic** (`DesktopAcrylicBackdrop`, or `AcrylicBrush` for smaller surfaces) for transient surfaces only — flyouts, menus, `NavigationView` panes — not full windows.
- For Mica/Acrylic to actually show through, layers above must be transparent or use theme-aware "layer fill" brushes (e.g., `LayerFillColorDefaultBrush`) rather than opaque hardcoded colors.

---

## 7. Dialogs and pickers

### 7.1 `ContentDialog` — the #1 most common AI mistake in WinUI 3

```csharp
// ❌ WRONG — throws InvalidOperationException at ShowAsync() in WinUI 3
var dialog = new ContentDialog
{
    Title = "Error",
    Content = "Something went wrong.",
    CloseButtonText = "OK"
};
await dialog.ShowAsync();
```

```csharp
// ✅ CORRECT — XamlRoot must be set before ShowAsync()
var dialog = new ContentDialog
{
    Title = "Error",
    Content = "Something went wrong.",
    CloseButtonText = "OK",
    XamlRoot = this.Content.XamlRoot // 'this' = the Window; or Page.XamlRoot directly if 'this' is a Page
};
await dialog.ShowAsync();
```

Why: UWP had a single implicit visual root, so `ContentDialog` could infer where to render. WinUI 3 supports **multiple windows**, so the framework needs to be told explicitly which visual tree (`XamlRoot`) to host the dialog in.

- If you're inside a **`Page`**, use `this.XamlRoot` directly (a `Page` has its own `XamlRoot` property).
- If you're inside a **`Window`** (or `App.xaml.cs`), there is no `Window.XamlRoot` property — go through the window's root content element: `this.Content.XamlRoot`.
- **Never** show a second `ContentDialog` while one is already open on the same thread — WinUI 3 allows only **one open `ContentDialog` per thread**, even across multiple `AppWindow`s. Calling `ShowAsync()` on a second dialog while the first is open throws. If your app can trigger dialogs from multiple code paths, serialize them (a simple `SemaphoreSlim(1,1)` guard around every `ShowAsync()` call, or a small dialog-queueing service, works well).
- A `ContentDialog` (or any `FrameworkElement`) can only be associated with **one `XamlRoot` at a time**. Re-showing the *same instance* against a different `XamlRoot` without first fully closing it throws `"This element is already associated with a XamlRoot..."`. Prefer creating a fresh `ContentDialog` instance per show, or explicitly reset `XamlRoot` only after the previous `ShowAsync()` has completed.
- `ContentDialog` can also be authored fully in XAML as its own file (`x:Class="MyApp.ConfirmDialog"`, root element `<ContentDialog ...>` instead of `<Page ...>`, code-behind `public sealed partial class ConfirmDialog : ContentDialog`). There's no Visual Studio item template for this — create it by hand from a blank `Page` template and change the root/base class.

### 7.2 `MessageDialog` does not exist — use `ContentDialog`

```csharp
// ❌ WRONG — UWP-only Windows.UI.Popups API, not usable this way in WinUI 3 desktop
var dialog = new Windows.UI.Popups.MessageDialog("Are you sure?", "Confirm");
await dialog.ShowAsync();
```

```csharp
// ✅ CORRECT
var dialog = new ContentDialog
{
    Title = "Confirm",
    Content = "Are you sure?",
    PrimaryButtonText = "Yes",
    CloseButtonText = "No",
    XamlRoot = this.Content.XamlRoot
};
var result = await dialog.ShowAsync();
if (result == ContentDialogResult.Primary)
{
    // user confirmed
}
```

For routine, non-blocking, in-app error/status messages, prefer an **`InfoBar`** (with `Severity="Error"`) embedded in the page over popping a modal `ContentDialog` for every little thing — this matches current Fluent Design guidance and avoids the "only one dialog at a time" trap entirely for high-frequency messages.

### 7.3 File/Folder pickers — must call `InitializeWithWindow`

```csharp
// ❌ WRONG — throws (or silently no-ops) because there's no implicit "current view" HWND in desktop WinUI 3
var picker = new Windows.Storage.Pickers.FileOpenPicker();
picker.FileTypeFilter.Add(".txt");
var file = await picker.PickSingleFileAsync();
```

```csharp
// ✅ CORRECT
var picker = new Windows.Storage.Pickers.FileOpenPicker();
picker.FileTypeFilter.Add(".txt");

var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

var file = await picker.PickSingleFileAsync();
```

This applies to **`FileOpenPicker`, `FileSavePicker`, and `FolderPicker`** identically — always call `InitializeWithWindow.Initialize(picker, hwnd)` before invoking the pick call. This is the general shape of a broader rule:

> **Every UWP API that used to rely on `GetForCurrentView()` needs an explicit window handle in WinUI 3 desktop apps**, obtained via `WinRT.Interop.WindowNative.GetWindowHandle(window)`.

This same interop pattern applies to `DataTransferManager` (Share) via `IDataTransferManagerInterop`, and `PrintManager` via `IPrintManagerInterop` — both need a window handle rather than the old `GetForCurrentView()` call.

`GetForCurrentView()` replacement table:

| UWP API | WinUI 3 replacement |
|---|---|
| `ApplicationView.GetForCurrentView()` | `AppWindow.GetFromWindowId(windowId)` |
| `UIViewSettings.GetForCurrentView()` | Use `AppWindow` properties |
| `DisplayInformation.GetForCurrentView()` | Win32 `GetDpiForWindow()`, or `XamlRoot.RasterizationScale` |
| `CoreApplication.GetCurrentView()` | Not available — track windows manually (e.g., a `List<Window>` or a dictionary keyed by `AppWindow.Id`) |
| `SystemNavigationManager.GetForCurrentView()` | Handle back navigation directly in `NavigationView` |

---

## 8. Navigation: `Frame` + `NavigationView`

`Frame.Navigate(typeof(MyPage))` is **unchanged** from UWP — the navigation/`Page`/`Frame` history model carried over almost verbatim. What changed is how you host it.

### 8.1 Typical setup

```csharp
// App.xaml.cs
protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
{
    MainWindow = new MainWindow();
    MainWindow.Activate();
}
```

```xml
<!-- MainWindow.xaml -->
<Window ...>
    <NavigationView x:Name="NavView"
                     ItemInvoked="NavView_ItemInvoked"
                     IsBackButtonVisible="Auto"
                     BackRequested="NavView_BackRequested">
        <NavigationView.MenuItems>
            <NavigationViewItem Content="Home" Icon="Home" Tag="Home" />
            <NavigationViewItem Content="Settings" Icon="Setting" Tag="Settings" />
        </NavigationView.MenuItems>

        <Frame x:Name="ContentFrame" />
    </NavigationView>
</Window>
```

```csharp
private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
{
    if (args.IsSettingsInvoked)
    {
        ContentFrame.Navigate(typeof(SettingsPage));
        return;
    }

    var tag = (args.InvokedItemContainer as NavigationViewItem)?.Tag as string;
    Type? pageType = tag switch
    {
        "Home" => typeof(HomePage),
        _ => null
    };
    if (pageType != null)
        ContentFrame.Navigate(pageType);
}

private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
{
    if (ContentFrame.CanGoBack)
        ContentFrame.GoBack();
}
```

Key facts:

- **`NavigationView` does not perform navigation automatically.** It only raises `ItemInvoked` (and `SelectionChanged`); you are responsible for calling `Frame.Navigate(...)` yourself in the handler. Assuming `NavigationView` will auto-navigate to a `Page` just because a `NavigationViewItem`'s tag matches a type name is a common AI misunderstanding — there is no such built-in binding.
- `ItemInvoked` fires when the user taps an item (even if it's already selected); `SelectionChanged` fires when the *selection* actually changes, and only fires as a *result* of `ItemInvoked` for user-driven taps (it fires independently for programmatic selection changes). For navigation, handle `ItemInvoked`.
- The **built-in back button** does not navigate automatically either — handle `BackRequested` and call `Frame.GoBack()` yourself.
- Recommended current pattern (used by the WinUI 3 Gallery app): put a `TitleBar` control above the `NavigationView` and let the `TitleBar` own the back button/pane-toggle button, then set `NavigationView.IsBackButtonVisible="Collapsed"` and `IsPaneToggleButtonVisible="False"` so you don't get duplicate back/toggle buttons in two places.
- `NavigationView.SelectedItem="{x:Bind ViewModel.SelectedItem}"` (binding selection from a ViewModel) has known long-standing quirks/bugs in some scenarios — prefer driving selection imperatively from the `ItemInvoked`/navigation-completed handlers rather than fighting a two-way `SelectedItem` binding for MVVM-driven nav state, unless you've verified the specific WinUI 3 version behaves correctly.

Navigation substitution table:

| UWP | WinUI 3 |
|---|---|
| `Frame.Navigate(typeof(MyPage))` | Unchanged |
| `SystemNavigationManager.BackRequested` | `NavigationView.BackRequested` (or `AppWindow`-level handling) |
| `Windows.UI.Core.Preview.SystemNavigationManagerPreview` (closing confirmation) | `AppWindow.Closing` event |

---

## 9. MVVM and dependency injection

### 9.1 Prefer `CommunityToolkit.Mvvm`

Hand-rolling `INotifyPropertyChanged` boilerplate is verbose and error-prone. The current recommended approach uses the `CommunityToolkit.Mvvm` NuGet package's source generators:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string statusText = "Ready";

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            await Task.Delay(500); // real work here
            StatusText = "Saved";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

- The ViewModel class **must be `partial`** — the source generator emits the public property (`StatusText`, generated from the private field `statusText`) and the `PropertyChanged` plumbing into a second partial file. Forgetting `partial` on the class is a very common AI mistake that produces confusing "property does not exist" errors in XAML, because the generator silently fails to add members to a non-partial class.
- `[RelayCommand]` generates an `ICommand`-typed property (`SaveCommand`) from the method (`SaveAsync`) — bind it as `Command="{x:Bind ViewModel.SaveCommand}"`.
- Do not bind directly to a plain `async void` method from XAML `Click` handlers when a full command (with `CanExecute`, busy-state, etc.) is more appropriate — but for simple cases, an `x:Bind`-wired code-behind `Click="Button_Click"` event handler is completely valid WinUI 3 style; MVVM purity is a project choice, not a hard framework requirement.

### 9.2 Dependency injection

`Microsoft.Extensions.DependencyInjection` is the standard choice for service/ViewModel registration in WinUI 3 apps, wired up in `App.xaml.cs`:

```csharp
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
```

Keep Views (XAML) focused on layout/bindings; keep logic in ViewModels/services; use `async`/`await` for I/O so the UI thread is never blocked.

---

## 10. Theming, colors, and Fluent Design resources

### 10.1 `{ThemeResource}` vs `{StaticResource}` for brushes — never hardcode colors

```xml
<!-- ❌ WRONG — ignores Light/Dark/HighContrast theme switching, and clashes with system accent -->
<Border Background="#FFFFFF" BorderBrush="#E0E0E0" />

<!-- ✅ CORRECT — theme-aware, respects Light/Dark/HighContrast automatically -->
<Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}" />
```

- **Always** use `{ThemeResource}` (not `{StaticResource}`) for brushes/colors that should adapt to the user's theme. `{StaticResource}` resolves once at load and never re-evaluates on a theme change; `{ThemeResource}` re-resolves every time the active theme changes.
- **Never hardcode raw color literals** (`#FFFFFF`, `Colors.White`, `Color.FromArgb(...)`) for standard UI surfaces — use the built-in system brush resources: `TextFillColorPrimaryBrush`, `TextFillColorSecondaryBrush`, `CardBackgroundFillColorDefaultBrush`, `CardStrokeColorDefaultBrush`, `ControlStrokeColorDefaultBrush`, `LayerFillColorDefaultBrush`, etc. These are defined by the framework's Fluent resource dictionaries and automatically track Light/Dark/HighContrast.
- Use `SystemAccentColor` (and the `SystemAccentColorLight1`–`Light3` / `SystemAccentColorDark1`–`Dark3` variants) to respect the user's chosen Windows accent color, rather than hardcoding a brand color for interactive accents.
- Inside your **own** `ThemeDictionaries`, define resources with `{StaticResource}` internally, not `{ThemeResource}` — the *outer* lookup (app code referencing your keyed resource) uses `{ThemeResource}`, but resource-to-resource references *inside* a specific theme dictionary use `{StaticResource}`. (Exception: theme-agnostic system accent/system-color resources can still use `{ThemeResource}` even inside a theme dictionary.)

### 10.2 Custom `ThemeDictionaries` structure

```xml
<!-- App.xaml -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            <ResourceDictionary Source="Styles/AppStyles.xaml" />
        </ResourceDictionary.MergedDictionaries>

        <ResourceDictionary.ThemeDictionaries>
            <ResourceDictionary x:Key="Light">
                <SolidColorBrush x:Key="AccentTextBrush" Color="#0066CC" />
            </ResourceDictionary>
            <ResourceDictionary x:Key="Dark">
                <SolidColorBrush x:Key="AccentTextBrush" Color="#66B2FF" />
            </ResourceDictionary>
            <ResourceDictionary x:Key="HighContrast">
                <SolidColorBrush x:Key="AccentTextBrush" Color="{ThemeResource SystemColorHighlightColor}" />
            </ResourceDictionary>
        </ResourceDictionary.ThemeDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

- Each `ResourceDictionary` inside `ThemeDictionaries` **must** be keyed with `x:Key` equal to a recognized theme name — `"Light"`, `"Dark"`, `"HighContrast"`, or `"Default"` (the fallback used if the active theme has no matching dictionary).
- Always define **both** `Light` and `Dark` (don't rely solely on `Default`) plus a `HighContrast` dictionary for accessibility compliance — this is the documented Microsoft guideline, not merely a suggestion.
- `MergedDictionaries` (plain, non-theme dictionaries — e.g. reusable `Style`/`DataTemplate` files) is a **separate** property from `ThemeDictionaries` — don't confuse the two; `MergedDictionaries` items are always active regardless of theme, `ThemeDictionaries` items are theme-conditional.
- Load order/precedence: resources declared closer to the point of use (`Page.Resources` beats `Application.Resources`) win in a lookup; among `MergedDictionaries`, later entries can override earlier ones. Don't rely on this for critical logic — keep resource keys unique across dictionaries where possible.

### 10.3 Typography — use built-in text styles, don't hand-set fonts

```xml
<!-- ❌ Avoid: hardcoded font sizing/weight scattered through the app -->
<TextBlock Text="Settings" FontSize="20" FontWeight="SemiBold" />

<!-- ✅ Preferred: built-in Fluent type-ramp styles -->
<TextBlock Text="Settings" Style="{StaticResource SubtitleTextBlockStyle}" />
```

Standard built-in `TextBlock` styles (from smallest to largest): `CaptionTextBlockStyle`, `BodyTextBlockStyle`, `BodyStrongTextBlockStyle`, `SubtitleTextBlockStyle`, `TitleTextBlockStyle`, `TitleLargeTextBlockStyle`, `DisplayTextBlockStyle`. Prefer these over ad hoc `FontSize`/`FontWeight`/`FontFamily` values — they keep the app visually consistent with the OS and update automatically if Microsoft revises the type ramp. The default font is **Segoe UI Variable** — don't override the font family unless there's a specific design reason to. Use sentence casing for UI text (e.g., "Save changes", not "Save Changes" or "SAVE CHANGES"), matching Windows 11 conventions.

### 10.4 Spacing, layout, and corner radius

- Use a **4px grid**: margins, padding, and `Spacing` values should be multiples of 4 (4, 8, 12, 16, 24 are the common steps: 4 = compact, 8 = between controls, 12 = small gutters, 16 = content padding, 24 = large section gutters).
- Prefer `Grid` (with `Auto`/`*` row/column sizing) over deeply nested `StackPanel` chains — nested `StackPanel`s are a common AI shortcut but hurt both layout performance and (past a couple of levels) predictability of measure/arrange behavior.
- Use `ControlCornerRadius` (4px) for small controls (buttons, text boxes) and `OverlayCornerRadius` (8px) for larger surfaces (cards, dialogs, flyouts) — these are theme resources, reference them rather than hardcoding `CornerRadius="4"` everywhere.
- For responsive layouts, use `VisualStateManager` with `AdaptiveTrigger`s at the standard Fluent breakpoints (640px and 1008px window width) rather than hand-rolled `SizeChanged` handlers.

---

## 11. Control selection guide

Use the framework-provided control that already solves the problem instead of hand-rolling a substitute — this is both less code and matches Fluent Design expectations users already have.

| Need | Use | Not |
|---|---|---|
| Primary app navigation (sidebar/hamburger) | `NavigationView` | A custom `SplitView` + hand-rolled list (very common in old UWP samples — actively avoid porting this pattern; `NavigationView` supersedes it) |
| Persistent in-app status/error banner | `InfoBar` | A custom `Border`+`TextBlock` "toast" |
| Contextual, anchored guidance/callout | `TeachingTip` | A custom `Popup` |
| Numeric input with validation/spinner | `NumberBox` | `TextBox` + manual parsing/validation |
| Boolean on/off setting | `ToggleSwitch` | `CheckBox` (reserve `CheckBox` for selection-in-a-list / multi-select scenarios, not single on/off settings) |
| Large virtualized list/grid with built-in selection | `ListView` / `GridView` | Manually virtualizing yourself |
| Modern data collection with flexible layout, virtualization, selection | `ItemsView` | — |
| Fully custom virtualized layout, no built-in selection needed | `ItemsRepeater` | `ListView` if you actually need its built-in selection/interaction — `ItemsRepeater` is lower-level and doesn't provide that for free |
| Collapsible section | `Expander` | Custom `Visibility` toggling on a `Grid`/`StackPanel` |
| Tabbed documents/workspaces | `TabView` | Custom tab strip |

---

## 12. Error handling and async patterns

```csharp
// ❌ Unhandled exception in an async void event handler crashes the whole app —
// there is no caller to catch/await the exception.
private async void SaveButton_Click(object sender, RoutedEventArgs e)
{
    await SaveFileAsync(); // if this throws, the app crashes
}

// ✅ Always wrap async void event handlers in try/catch
private async void SaveButton_Click(object sender, RoutedEventArgs e)
{
    try
    {
        await SaveFileAsync();
    }
    catch (Exception ex)
    {
        // Surface via InfoBar, not necessarily a blocking ContentDialog, for routine errors
        StatusInfoBar.Message = $"Save failed: {ex.Message}";
        StatusInfoBar.Severity = InfoBarSeverity.Error;
        StatusInfoBar.IsOpen = true;
    }
}
```

- `async void` is unavoidable for XAML event handlers (event delegate signatures require `void`), so it is the **one place** `async void` is acceptable — but it means exceptions **cannot be caught by a caller**; they will crash the app (or at best hit `Application.UnhandledException`) if not caught internally. Wrap the body in `try`/`catch`.
- For everything that isn't a XAML event handler (services, ViewModel command bodies, helper methods), use `async Task`, not `async void`.
- Handle `Application.UnhandledException` at the `App` level for logging/telemetry and, where appropriate, graceful recovery — but don't rely on it as a substitute for local `try`/`catch` around risky calls.
- Never block the UI thread with `.Result`/`.Wait()` on a `Task` from UI code — always `await`.

---

## 13. Accessibility

- Set `AutomationProperties.Name` on interactive controls that don't have inherently readable text content (icon-only buttons, custom controls) so screen readers announce something meaningful.
- Use `AutomationProperties.HeadingLevel` on section headers to give screen-reader users a navigable outline.
- Hide purely decorative visual elements (background shapes, spacer glyphs) from assistive tech with `AutomationProperties.AccessibilityView="Raw"`.
- Ensure every interactive element is reachable and operable via keyboard alone: Tab order, Enter/Space activation, arrow-key navigation within composite controls.
- Verify color contrast meets WCAG guidance — this is another reason to use the built-in theme brushes (Section 10) rather than hand-picked colors, since the built-in Fluent palette is designed to meet contrast requirements across Light/Dark/HighContrast.

---

## 14. Packaging: packaged (MSIX) vs unpackaged

- **WinUI 3 apps created from the standard Visual Studio/`dotnet new` templates are packaged (MSIX) by default.** You do not need to do anything special to get MSIX packaging for a typical app.
- Packaging (MSIX or "packaged with external location"/sparse packaging) gives the app **package identity**, which unlocks: background tasks, push/toast notifications, live tiles (deprecated — see below), custom context-menu extensions, share targets/`DataTransferManager` scenarios, and other identity-gated extensibility points, plus clean install/uninstall and differential auto-updates.
- **Unpackaged** apps (`<WindowsPackageType>None</WindowsPackageType>`) run as a plain folder of files with no package identity. They cannot use identity-gated APIs above, and calling into them typically throws `E_ILLEGAL_METHOD_CALL` / `APPMODEL_ERROR_NO_PACKAGE`-style errors rather than silently no-oping. Unpackaged apps are the right choice when you need to ship via a traditional installer (WiX/NSIS/InstallShield/SCCM/Intune push) instead of MSIX, or need `PublishSingleFile` (a single self-contained EXE — only supported for **unpackaged, self-contained** WinUI 3 apps, not for packaged or framework-dependent ones).
- Unpackaged and "packaged with external location" apps must **manually initialize** the Windows App SDK runtime via the **Bootstrapper API** at startup, and are responsible for ensuring the Windows App SDK runtime is present on the target machine (bundle the runtime installer, or deploy it via the framework packages).
- **Settings/local storage**: `ApplicationData.Current.LocalSettings` and `ApplicationData.Current.LocalFolder` work normally for **packaged** apps but **throw** (no package identity) for unpackaged ones. For unpackaged apps, use a hand-rolled JSON settings file under `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` instead. **Do not assume `ApplicationData` is always usable — check/branch on packaging status**, or design settings storage to work uniformly across both from day one.

| Scenario | Packaged app | Unpackaged app |
|---|---|---|
| Simple settings | `ApplicationData.Current.LocalSettings` | Hand-rolled JSON file in `LocalApplicationData` |
| Local file storage | `ApplicationData.Current.LocalFolder` | `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)` |
| Distribution | Microsoft Store, MSIX sideload, Intune/SCCM (MSIX-aware) | Traditional installer, xcopy deploy, `PublishSingleFile` |
| Auto-update | Built-in (MSIX) | You implement it |
| Background tasks / notifications / share targets | Supported | Not supported |

---

## 15. Migrating an existing UWP (or WPF) app to WinUI 3 — quick reference

If asked to port/migrate an app, apply these substitutions systematically rather than one-off guessing. (This mirrors Microsoft's own official AI-migration guidance.)

### 15.1 Namespaces
`Windows.UI.Xaml.*` → `Microsoft.UI.Xaml.*` (see full table in Section 1.1).

### 15.2 Threading
`CoreDispatcher` / `Dispatcher.RunAsync` → `DispatcherQueue.TryEnqueue` (see Section 5).

### 15.3 Windowing
`ApplicationView`/`CoreWindow` → `AppWindow` + `Microsoft.UI.Xaml.Window`; `CoreApplicationViewTitleBar` → `AppWindowTitleBar` (see Section 6).

### 15.4 Dialogs/pickers
`MessageDialog` → `ContentDialog` with `XamlRoot`; pickers need `InitializeWithWindow` (see Section 7).

### 15.5 Notifications

| UWP | WinUI 3 |
|---|---|
| `Windows.UI.Notifications.ToastNotificationManager` | `Microsoft.Windows.AppNotifications.AppNotificationManager` |
| `Windows.UI.Notifications.BadgeUpdateManager` | `Microsoft.Windows.BadgeNotifications.BadgeNotificationManager` |
| `Windows.UI.Notifications.TileUpdateManager` (live tiles) | **Deprecated entirely** — use app notifications or widgets instead; there is no tile equivalent to migrate to. |

### 15.6 App lifecycle — do not treat this as a simple rename

| UWP | WinUI 3 |
|---|---|
| `Application.Current.Suspending` | `Microsoft.Windows.AppLifecycle` — requires **architectural changes**, not a drop-in replacement. |
| `Application.Current.Resuming` | `AppInstance.GetCurrent().Activated` |
| `IBackgroundTask` / `BackgroundTaskBuilder` | `Microsoft.Windows.AppLifecycle` rich activation |

Windows App SDK apps are **multi-instanced by default** (launching the app a second time starts a new process instance), unlike UWP's single-instance-by-default model. If single-instancing matters, register/redirect via `AppInstance.FindOrRegisterForKey(...)` in `OnLaunched` yourself.

**Treat lifecycle/suspension migration as a dedicated rewrite, not an automated find-and-replace** — the activation and suspension model is fundamentally different, not just renamed.

### 15.7 Settings/storage — unchanged
`ApplicationData.Current.LocalSettings`, `ApplicationData.Current.LocalFolder`, `Windows.Storage.KnownFolders` — all **unchanged** for packaged apps (see Section 14 for unpackaged caveats).

### 15.8 Sharing and printing
`DataTransferManager` and `PrintManager` are no longer used "directly" the UWP way — go through `IDataTransferManagerInterop` / `IPrintManagerInterop` with a window handle, same `InitializeWithWindow`-style pattern as pickers.

### 15.9 Custom UWP navigation shells

Many older UWP samples hand-rolled hamburger navigation with a `SplitView` + a custom `NavMenuListView` (~500+ lines of bespoke code). **Replace this entire pattern with `NavigationView`** (Section 8) — it provides the same UX with built-in accessibility, responsive collapsing, and back-button support essentially for free. Don't port the old hand-rolled shell.

### 15.10 Project file
```xml
<!-- Before (UWP) -->
<TargetPlatformVersion>10.0.19041.0</TargetPlatformVersion>
<TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>

<!-- After (WinUI 3) -->
<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
<WindowsSdkPackageVersion>10.0.19041.31</WindowsSdkPackageVersion>
```
Add `Microsoft.WindowsAppSDK` via NuGet.

### 15.11 Testing
UWP unit test projects **do not run** against WinUI 3 XAML types. Migrate to the **Unit Test App (WinUI in Desktop)** project template (and, for reusable non-UI logic, a **Class Library (WinUI in Desktop)**). Use `[TestMethod]` for pure logic; use **`[UITestMethod]`** for anything that instantiates a `Microsoft.UI.Xaml` type (controls, pages, user controls) — this attribute runs the test on the XAML UI thread, which is required to construct any XAML type. A plain MSTest/xUnit project without the WinUI test host will fail or hang when it tries to `new` up a XAML control.

### 15.12 A caution about Windows-version-dependent APIs
When migrating, watch for `Windows.ApplicationModel.*`/`Windows.System.*` members that assume a UWP app-container context, and for any Windows-11-only APIs accidentally called on a Windows-10-targeted build — invoking an unavailable API during startup can terminate the app with a native exception (e.g., `0xc000027b`) rather than a clean managed exception. If the app must support Windows 10, verify API availability (e.g., via `ApiInformation.IsApiContractPresent`) before calling anything you're not sure shipped in the minimum target version.

---

## 16. The Golden Checklist (run this before finalizing any WinUI 3 code)

**Namespaces & project**
- [ ] No `Windows.UI.Xaml.*`, `Windows.UI.Composition`, or `Windows.UI.Colors` anywhere in `using` statements or XAML `xmlns` — all replaced with `Microsoft.UI.*` equivalents.
- [ ] `.csproj` has `<UseWinUI>true</UseWinUI>` and a TFM like `net10.0-windows10.0.XXXXX.0`.
- [ ] `InitializeComponent()` is the first line of every partial-class constructor, before any `x:Name`'d field is touched.
- [ ] `.cs`/`.xaml` files use CRLF line endings, or `.gitattributes`/`.editorconfig` normalize them (Section 2.3).

**Data binding**
- [ ] Every `{x:Bind}` on a value that changes at runtime has `Mode=OneWay` (or `TwoWay` for editable inputs) — never left at the silent `OneTime` default by accident.
- [ ] Source properties for `OneWay`/`TwoWay` bindings raise `INotifyPropertyChanged` (or are `DependencyProperty`s).
- [ ] Every `DataTemplate` using `{x:Bind}` has `x:DataType` set to the correct item type.
- [ ] Collections bound to `ItemsSource` that change at runtime are `ObservableCollection<T>`, not `List<T>`.
- [ ] Not mixing up `{x:Bind}`'s code-behind-class default source with `{Binding}`'s `DataContext` default source.

**Threading & windowing**
- [ ] No `CoreDispatcher` / `Dispatcher.RunAsync` — only `DispatcherQueue.TryEnqueue`.
- [ ] No `Window.Current` — using a static `App.MainWindow` (or equivalent) instead.
- [ ] No `ApplicationView`/`CoreWindow` for window management — using `AppWindow`.
- [ ] No `CoreApplicationViewTitleBar` — using `AppWindowTitleBar`.

**Dialogs & pickers**
- [ ] Every `ContentDialog` sets `XamlRoot` before `ShowAsync()`.
- [ ] No `Windows.UI.Popups.MessageDialog` — `ContentDialog` used instead.
- [ ] Only one `ContentDialog` open per thread at a time (guarded/serialized if triggered from multiple paths).
- [ ] Every `FileOpenPicker`/`FileSavePicker`/`FolderPicker` calls `InitializeWithWindow.Initialize(picker, hwnd)` before picking.

**Navigation**
- [ ] `NavigationView.ItemInvoked` (not an assumed automatic behavior) drives `Frame.Navigate(...)` explicitly.
- [ ] `NavigationView.BackRequested` explicitly calls `Frame.GoBack()`.

**Styling & theming**
- [ ] Brushes/colors use `{ThemeResource ...SomeSystemBrush}`, not hardcoded hex/`Colors.X` values.
- [ ] Text uses built-in `*TextBlockStyle` resources, not manual `FontSize`/`FontWeight`.
- [ ] Spacing/margins follow the 4px grid.
- [ ] Custom `ThemeDictionaries` define `Light`, `Dark`, and `HighContrast`.

**Errors & MVVM**
- [ ] `async void` event handlers are wrapped in `try`/`catch`.
- [ ] ViewModel classes using `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`) are declared `partial`.
- [ ] `TryEnqueue`'s `bool` return isn't accidentally `await`ed.

**Packaging**
- [ ] Settings/storage code doesn't unconditionally assume `ApplicationData.Current` works — packaged vs. unpackaged is accounted for.

**Testing**
- [ ] Any test project that instantiates `Microsoft.UI.Xaml` types is a **Unit Test App (WinUI in Desktop)** project using `[UITestMethod]`, not a plain MSTest/xUnit project.

---

## 17. Minimal correct app skeleton (reference)

```xml
<!-- App.xaml -->
<Application
    x:Class="MyApp.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

```csharp
// App.xaml.cs
using Microsoft.UI.Xaml;

namespace MyApp;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
```

```xml
<!-- MainWindow.xaml -->
<Window
    x:Class="MyApp.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Window.SystemBackdrop>
        <MicaBackdrop />
    </Window.SystemBackdrop>

    <Grid Padding="24" RowSpacing="12">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0"
                   Text="{x:Bind ViewModel.StatusText, Mode=OneWay}"
                   Style="{StaticResource BodyStrongTextBlockStyle}" />

        <Button Grid.Row="1"
                Content="Save"
                Command="{x:Bind ViewModel.SaveCommand}" />
    </Grid>
</Window>
```

```csharp
// MainWindow.xaml.cs
using Microsoft.UI.Xaml;

namespace MyApp;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        ViewModel = new MainViewModel();
        this.InitializeComponent(); // must come after ViewModel is set if XAML binds to it at load,
                                      // and must come before touching any x:Name'd elements either way
    }
}
```

```csharp
// MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string statusText = "Ready";

    [RelayCommand]
    private void Save()
    {
        StatusText = "Saved!";
    }
}
```

This skeleton demonstrates, in one place: correct namespaces, `Mode=OneWay` on a changing bind, `CommunityToolkit.Mvvm` `partial` ViewModel, `MicaBackdrop`, built-in text style, `Command` binding via `[RelayCommand]`, and correct `InitializeComponent()` ordering.

---

## 18. Quick-reference: UWP → WinUI 3 API substitution table

| Category | UWP | WinUI 3 |
|---|---|---|
| Namespace | `Windows.UI.Xaml.*` | `Microsoft.UI.Xaml.*` |
| Namespace | `Windows.UI.Composition` | `Microsoft.UI.Composition` |
| Threading | `CoreDispatcher` | `DispatcherQueue` |
| Threading | `Dispatcher.RunAsync(...)` | `DispatcherQueue.TryEnqueue(...)` |
| Threading | `CoreApplication.MainView.CoreWindow.Dispatcher` | `this.DispatcherQueue` |
| Windowing | `ApplicationView` | `AppWindow` |
| Windowing | `ApplicationView.GetForCurrentView()` | `AppWindow.GetFromWindowId(...)` |
| Windowing | `ApplicationViewTitleBar` | `AppWindowTitleBar` |
| Windowing | `CoreWindow` | `Microsoft.UI.Xaml.Window` |
| Windowing | `SystemNavigationManager` | Back handling via `AppWindowTitleBar` / `NavigationView` |
| Dialogs | `MessageDialog` | `ContentDialog` (set `XamlRoot`) |
| Pickers | `FileOpenPicker`/`FileSavePicker`/`FolderPicker` | Same types + `InitializeWithWindow` |
| Notifications | `Windows.UI.Notifications.ToastNotificationManager` | `Microsoft.Windows.AppNotifications.AppNotificationManager` |
| Notifications | `Windows.UI.Notifications.BadgeUpdateManager` | `Microsoft.Windows.BadgeNotifications.BadgeNotificationManager` |
| Notifications | `Windows.UI.Notifications.TileUpdateManager` | Deprecated — no tile equivalent |
| Navigation | `Frame.Navigate(typeof(MyPage))` | Unchanged |
| Navigation | `SystemNavigationManagerPreview` (close confirmation) | `AppWindow.Closing` |
| Lifecycle | `Application.Current.Suspending` | `Microsoft.Windows.AppLifecycle` (architectural rewrite) |
| Lifecycle | `Application.Current.Resuming` | `AppInstance.GetCurrent().Activated` |
| Lifecycle | `IBackgroundTask` | `Microsoft.Windows.AppLifecycle` rich activation |
| Storage | `ApplicationData.Current.LocalSettings` | Unchanged (packaged apps only) |
| Storage | `ApplicationData.Current.LocalFolder` | Unchanged (packaged apps only) |
| Auth | `WebAuthenticationBroker` | `OAuth2Manager` (Windows App SDK 1.7+) |
| Sharing | `DataTransferManager` (direct use) | `IDataTransferManagerInterop` + window handle |
| Printing | `PrintManager` (direct use) | `IPrintManagerInterop` + window handle |
| Unchanged | `Windows.Devices.*`, `Windows.Media.*`, `Windows.UI.ViewManagement.UISettings`, `Windows.UI.Color` (struct), most non-XAML WinRT | No change needed |

---

## 19. Sources

This guide synthesizes and reorganizes guidance published by Microsoft, primarily:

- Microsoft Learn — *WinUI 3 overview*: `learn.microsoft.com/windows/apps/winui/winui3/`
- Microsoft Learn — *Migrate a UWP app to WinUI 3 (AI-assisted)*: `learn.microsoft.com/windows/apps/develop/ai-assisted/migrate/uwp-to-winui`
- Microsoft Learn — *What's supported when migrating from UWP to WinUI*: `learn.microsoft.com/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/what-is-supported`
- Microsoft Learn — *Mapping UWP APIs and libraries to the Windows App SDK*: `learn.microsoft.com/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/api-mapping-table`
- Microsoft Learn — *Windowing overview for WinUI and Windows App SDK*
- Microsoft Learn — *Windows data binding in depth*, *xBind markup extension*, *Functions in x:Bind*, *Data Binding basics tutorial*
- Microsoft Learn — *NavigationView*, *Dialog controls*, *ResourceDictionary and XAML resource references*, *XAML theme resources*, *ResourceDictionary.ThemeDictionaries*
- Microsoft Learn — *Package and deploy Windows apps overview*, *Choose a packaging model*, *Distribute an unpackaged WinUI 3 app*, *Windows App SDK deployment guides*
- Microsoft Learn — *Application.OnLaunched*, *Window class*, *Application lifecycle functionality migration*
- Microsoft Learn — *Get started with WinUI*, *Quick start: Create your first WinUI 3 app*, *Windows developer FAQ*
- `github.com/github/awesome-copilot` — `instructions/winui3.instructions.md` and `skills/winui3-migration-guide/SKILL.md` (Microsoft/community-maintained Copilot guidance specifically for WinUI 3 code generation)
- `github.com/microsoft/win-dev-skills` — Microsoft's own agent/skill tooling for WinUI 3 development
- `github.com/microsoft/microsoft-ui-xaml` — issue tracker, for real-world documented runtime behaviors (e.g., single-`ContentDialog`-per-thread limitation)

Because the Windows App SDK ships on a faster, independent release cadence from Windows itself, always sanity-check version-specific details (exact NuGet version numbers, newest control availability such as `ItemsView`, latest TFM patch numbers) against the current `learn.microsoft.com/windows/apps` documentation and the Windows App SDK release notes before relying on them in production code, since specifics move faster than any static guide can track.
