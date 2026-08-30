<#
    Verify-HighContrastPairing.ps1

    Regression gate for the Windows High Contrast foreground/background pairing rule:
    a control painted with the opaque SystemColorHighlightColor fill (hover/pressed/
    selected/checked states repaint as Highlight in every HighContrast dictionary in this
    repo) must pair it with SystemColorHighlightTextColor for whatever sits on top of that
    fill (label text, icon Fill/Stroke, a delineating BorderBrush). Pairing it with
    SystemColorWindowTextColor or SystemColorButtonTextColor instead — the pairing that is
    correct against the *default* Window/ButtonFace fill — produces unreadable text/icons
    once Windows High Contrast is turned on, because WindowText/ButtonText are tuned for
    contrast against Window/ButtonFace, not against Highlight. See the Task B section of
    docs/handoff/2026-08-29-winui3-full-property-audit-handoff.md for the bug this closes
    and the fixed files (EtherSegmentedControl, EtherDropdown, EtherButton, EtherMasthead,
    EtherSwitch).

    SCOPE AND LIMITATIONS (read before extending this script)
      This is a static, regex-based regression guard over the XAML source text, not a full
      XAML/visual-tree analyzer. It runs two complementary checks against every *.xaml file
      under src/Ether.DesignSystem.Controls/Controls that declares a ControlTemplate (pure
      token catalogs such as EtherColors.xaml are intentionally excluded — see Task B's
      write-up on why pairing is a per-consumer concern, not something the token catalog
      itself can violate on its own):

      Rule A (VisualState pairing, precise): within each <VisualState>...</VisualState>
      block, if any Setter targets a *.Background property with a brush that resolves (in
      that file's own HighContrast ThemeDictionary) to SystemColorHighlightColor, then every
      Setter in that SAME block targeting *.Foreground / *.Stroke / *.BorderBrush / *.Fill
      must resolve to SystemColorHighlightTextColor, not SystemColorWindowTextColor or
      SystemColorButtonTextColor. This catches the exact shape of bug this gate exists for:
      a Hover/Pressed VisualState that repaints the background but leaves the foreground
      Setter pointing at the wrong brush (or missing entirely, in which case the *file-level*
      Rule B below is what catches it, since Rule A can only inspect Setters that exist).

      Rule B (file-level token symmetry, coarse): a file that defines any HighContrast brush
      key whose name suggests an interactive-state background fill (contains Hover, Pressed,
      Selected, Checked, or Active) and which resolves to SystemColorHighlightColor must also
      define at least one HighContrast brush key that looks like a foreground/icon/stroke
      token (contains Foreground, Stroke, Glyph, Text, Icon, Fg, or Dot) resolving to
      SystemColorHighlightTextColor somewhere in the same file. This is intentionally coarse
      (it does not verify the two keys are actually used together) so it also catches
      component-specific patterns this repo uses where the background fill is applied via a
      static XAML attribute or from code-behind rather than a VisualState.Setter (e.g.
      EtherSegmentedControl's HoverLayer/PressedLayer opacity, driven from outside XAML, and
      EtherMasthead's caption icons, recolored from EtherMasthead.xaml.cs pointer handlers
      because Shape.Fill/Stroke cannot be reached by a same-template VisualState.Setter — see
      the comments in both files). Before this fix, none of the five affected files defined
      any such paired key at all, so this check would have caught the original regression.

      Neither rule attempts to resolve gradient brushes (LinearGradientBrush) or brushes
      defined outside the file's own HighContrast dictionary (e.g. a shared EtherColors.xaml
      token referenced via {ThemeResource text/primary}) — those are left unclassified and
      silently skipped rather than guessed at, to keep false positives low. Extend the
      keyword lists above (not per-property special cases) if a future component uses a
      naming convention these regexes do not already recognize.

      $script:exemptHighlightKeys below is a small, explicit, individually-justified list of
      brush keys Rule B would otherwise flag but that are not a text/icon-bearing fill: plain
      drag thumbs/knobs (EtherScrollBar, EtherSlider), a decorative track marker and thumb
      sheen (EtherSteeringBar), and a hover focus-ring stroke plus a blurred decorative glow
      layer (EtherIntelligenceButton) whose button text sits on a separate, unchanged
      ButtonFace fill. Each entry was verified by reading that brush's actual template usage
      before being added — do not add to this list without doing the same; if a future brush
      genuinely has no foreground content on top of it, explain why in the comment next to it.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlsRoot = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls'

$wrongForegroundColors = @('SystemColorWindowTextColor', 'SystemColorButtonTextColor')
$highlightColor = 'SystemColorHighlightColor'
$highlightTextColor = 'SystemColorHighlightTextColor'

$stateKeywordPattern = 'Hover|Pressed|Selected|Checked|Active'
$foregroundKeywordPattern = 'Foreground|Stroke|Glyph|Text|Icon|Fg|Dot'

# Rule B, unlike Rule A, cannot see whether a Highlight-colored brush is actually painted
# under text/icon content or is a plain decorative/functional element with nothing on top of
# it (a drag thumb, a track marker, a blurred glow layer, a hover focus-ring stroke around
# content that keeps its own unchanged fill). Each entry here was checked against that brush's
# real template usage — see the header comment above before adding to this list.
$exemptHighlightKeys = @(
    'EtherScrollBarThumbHoverBrush',            # Plain draggable scrollbar thumb; no text/icon renders on it.
    'EtherSliderKnobBrush',                     # Plain circular drag knob; no text/icon renders on it.
    'EtherSliderKnobPressedBrush',              # Same knob, pressed state.
    'EtherSteeringBarThumbTopHighlightBrush',   # Decorative sheen overlay on the glass thumb; no text content.
    'EtherSteeringBarStopMarkerActiveBrush',    # Small track marker dot; no text content.
    'EtherIntelligenceButtonBorderHoverBrush',  # Hover focus-ring stroke; the button's own text sits on a
                                                 # separate ButtonFace fill that never changes to Highlight.
    'EtherIntelligenceButtonBlueGlowPressedCoreBrush' # Blurred decorative glow layer behind the button; not a
                                                       # text-bearing fill (the button text/fill are unaffected).
)

function Get-HighContrastBrushMap {
    param([Parameter(Mandatory)][string]$Text)

    $map = @{}
    $dictMatch = [regex]::Match($Text, '(?s)<ResourceDictionary\s+x:Key="HighContrast"\s*>(?<body>.*?)</ResourceDictionary>')
    if (-not $dictMatch.Success) {
        return $map
    }

    $body = $dictMatch.Groups['body'].Value
    $brushMatches = [regex]::Matches($body, '<SolidColorBrush\b(?<attrs>[^>]*)/>')
    foreach ($brush in $brushMatches) {
        $attrs = $brush.Groups['attrs'].Value
        $keyMatch = [regex]::Match($attrs, 'x:Key="(?<key>[^"]+)"')
        $colorMatch = [regex]::Match($attrs, '\{ThemeResource\s+(?<color>SystemColor\w+)\}')
        if ($keyMatch.Success -and $colorMatch.Success) {
            $map[$keyMatch.Groups['key'].Value] = $colorMatch.Groups['color'].Value
        }
    }

    return $map
}

function Test-RuleA {
    param(
        [Parameter(Mandatory)][string]$FileName,
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][hashtable]$BrushMap
    )

    $localViolations = New-Object System.Collections.Generic.List[string]
    $stateMatches = [regex]::Matches($Text, '(?s)<VisualState\s+x:Name="(?<name>[^"]+)"\s*>(?<body>.*?)</VisualState>')
    foreach ($state in $stateMatches) {
        $stateName = $state.Groups['name'].Value
        $body = $state.Groups['body'].Value
        $setterMatches = [regex]::Matches($body, '<Setter\b(?<attrs>[^>]*)/>')

        $hasHighlightBackground = $false
        $foregroundSetters = New-Object System.Collections.Generic.List[object]
        foreach ($setter in $setterMatches) {
            $attrs = $setter.Groups['attrs'].Value
            $targetMatch = [regex]::Match($attrs, 'Target="(?<target>[^"]+)"')
            $valueMatch = [regex]::Match($attrs, '\{ThemeResource\s+(?<brush>[^}]+)\}')
            if (-not $targetMatch.Success -or -not $valueMatch.Success) {
                continue
            }

            $target = $targetMatch.Groups['target'].Value
            $brushKey = $valueMatch.Groups['brush'].Value
            $color = $BrushMap[$brushKey]

            if ($target -match '\.Background$') {
                if ($color -eq $highlightColor) {
                    $hasHighlightBackground = $true
                }
            } elseif ($target -match '\.(Foreground|Stroke|BorderBrush|Fill)$') {
                $foregroundSetters.Add([PSCustomObject]@{ Target = $target; BrushKey = $brushKey; Color = $color })
            }
        }

        if (-not $hasHighlightBackground) {
            continue
        }

        foreach ($fg in $foregroundSetters) {
            if ($wrongForegroundColors -contains $fg.Color) {
                $localViolations.Add("$FileName [VisualState '$stateName']: '$($fg.Target)' uses $($fg.BrushKey) ($($fg.Color)) alongside a *.Background Setter that resolves to $highlightColor in the same state. Pair it with a brush that resolves to $highlightTextColor instead.")
            }
        }
    }

    return ,$localViolations
}

function Test-RuleB {
    param(
        [Parameter(Mandatory)][string]$FileName,
        [Parameter(Mandatory)][hashtable]$BrushMap
    )

    $localViolations = New-Object System.Collections.Generic.List[string]
    $highlightStateKeys = @($BrushMap.Keys | Where-Object {
        $BrushMap[$_] -eq $highlightColor -and
        $_ -match $stateKeywordPattern -and
        $_ -notmatch $foregroundKeywordPattern -and
        $exemptHighlightKeys -notcontains $_
    })
    if ($highlightStateKeys.Count -eq 0) {
        return ,$localViolations
    }

    $pairedForegroundKeys = @($BrushMap.Keys | Where-Object {
        $BrushMap[$_] -eq $highlightTextColor -and
        $_ -match $foregroundKeywordPattern
    })
    if ($pairedForegroundKeys.Count -eq 0) {
        $sample = ($highlightStateKeys | Select-Object -First 3) -join ', '
        $localViolations.Add("${FileName}: defines interactive-state HighContrast brush(es) resolving to $highlightColor ($sample, ...) but no HighContrast brush resolving to $highlightTextColor was found with a Foreground/Stroke/Glyph/Text/Icon/Fg/Dot-shaped key. Add a paired brush and wire it into the relevant VisualState(s)/code-behind.")
    }

    return ,$localViolations
}

$xamlFiles = Get-ChildItem -LiteralPath $controlsRoot -Filter *.xaml -Recurse
$violations = New-Object System.Collections.Generic.List[string]
$checkedFiles = 0

foreach ($file in $xamlFiles) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    if ($text -notmatch '<ControlTemplate\b') {
        # Pure token/style catalogs (no ControlTemplate) do not themselves pair a
        # background with a foreground; see the header comment above.
        continue
    }

    $checkedFiles++
    $brushMap = Get-HighContrastBrushMap -Text $text
    if ($brushMap.Count -eq 0) {
        continue
    }

    foreach ($v in (Test-RuleA -FileName $file.Name -Text $text -BrushMap $brushMap)) {
        $violations.Add($v)
    }
    foreach ($v in (Test-RuleB -FileName $file.Name -BrushMap $brushMap)) {
        $violations.Add($v)
    }
}

if ($violations.Count -gt 0) {
    Write-Host "High Contrast foreground/background pairing audit found $($violations.Count) violation(s):"
    foreach ($violation in $violations) {
        Write-Host "  - $violation"
    }
    throw "High Contrast foreground/background pairing audit failed with $($violations.Count) violation(s)."
}

Write-Host "High Contrast foreground/background pairing audit passed: checked $checkedFiles templated XAML file(s); no Highlight-filled state paired a WindowText/ButtonText foreground, and every interactive-state Highlight fill has a matching HighlightText-shaped brush defined in the same file."
