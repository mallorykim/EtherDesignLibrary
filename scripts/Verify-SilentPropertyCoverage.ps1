<#
.SYNOPSIS
Open-ended detection gate for R-07: fails the build the moment a public, inherited property is
silently ineffective (Method == 'platform-dp-contract' in the newest runtime evidence) and is not
yet accounted for anywhere - instead of only re-checking a fixed, closed list of 12 known traps.

.DESCRIPTION
scripts/Verify-UnsupportedProperties.ps1 is a CLOSED allowlist: it re-verifies that 12 specific,
already-known "trap" properties (Header/HeaderTemplate/Description/PlaceholderText/
PlaceholderForeground/Text/IsEditable on EtherDropdown/EtherInput/EtherSwitch) are still
zero-consumption. It cannot discover a 13th silently-ineffective property that nobody has looked
at yet - by construction, a closed allowlist only ever shrinks the set of things it checks to
exactly what was hand-typed into it.

This script closes that gap by treating scripts/UnsupportedProperties.psd1's two sections as a
COMPLETE accounting that must cover every property the newest runtime evidence classifies as
silently ineffective, rather than as an independent hand-maintained list:

  1. Loads scripts/UnsupportedProperties.psd1: the 12-entry closed 'Entries' allowlist (traps)
     plus the open 'AcknowledgedSilent' list (every other known platform-dp-contract property,
     each bucketed 'design-system-owned' or 'platform-noop' - see that file's header comment for
     the full rule).
  2. Loads the NEWEST artifacts/audit-runs/consumer-runtime-evidence-*/runtime-result.json (by
     the timestamp encoded in the directory name - consumer-runtime-evidence-yyyyMMdd-HHmmssfff)
     and collects every property whose attachedVisualProperties.Evidence entry has
     Method == 'platform-dp-contract' (a real, rendered, attached control's DP round-tripped
     without throwing, but its RenderTargetBitmap did not change - i.e. genuinely
     silently-ineffective, not merely "untested").
  3. FORWARD check: every platform-dp-contract property from evidence must appear in EITHER the
     12 Entries (as Control.Property) OR AcknowledgedSilent (as Property). A property in neither
     FAILS the gate, naming the property and instructing a human to classify it as a trap
     (Entries, with an Alternative) or acknowledge it (AcknowledgedSilent, with a Bucket) -
     nothing is ever auto-added by this script. THIS is the open-ended part: a brand-new
     silently-ineffective property (the "13th trap") is caught the moment fresh evidence exists
     for it, with zero code changes required to notice it.
  4. REVERSE check: every AcknowledgedSilent entry must STILL be platform-dp-contract in that same
     evidence. If a property became genuinely wired (pixel/layout/visibility difference now
     observed) or simply stopped existing in the evidence, its AcknowledgedSilent entry is stale
     and FAILS the gate - so this list cannot silently accumulate entries that no longer describe
     reality.
  5. Sanity checks on the accounting data itself: every AcknowledgedSilent entry has a non-empty
     Property in 'Control.Property' form and a Bucket that is exactly 'design-system-owned' or
     'platform-noop'; no duplicate Property values; no property is claimed by both Entries and
     AcknowledgedSilent at once (that would make the accounting ambiguous about which section
     owns it).

This is a 'local-runtime' gate (see scripts/Gates.psd1): it needs a runtime evidence file that
only a local GUI/desktop Verify-ConsumerFixtures.ps1 pass produces, so hosted CI cannot run it.
If no evidence directory exists yet, this script SKIPS with an explicit, loud message and exits 0
- it does not silently pass by treating "no evidence" the same as "nothing to check".

Classification rule for 'design-system-owned' (kept in sync with scripts/UnsupportedProperties.
psd1's own header comment - both describe the same rule so a reviewer does not have to trust one
over the other): the property name is exactly one of, or ends with one of (same appearance
family - e.g. PlaceholderForeground ends with Foreground), the following appearance-property
names: Background, BackgroundSizing, BorderBrush, BorderThickness, CornerRadius, Padding,
Foreground, FontSize, FontFamily, FontWeight, FontStyle, FontStretch, CharacterSpacing,
HorizontalContentAlignment, VerticalContentAlignment. This script does not itself re-derive the
Bucket value from that rule - it trusts the human-reviewable Bucket already recorded in
UnsupportedProperties.psd1 and only checks that the value is one of the two known buckets. The
rule is documented here (and enforced structurally by check 5) so a reviewer can audit whether a
given AcknowledgedSilent entry was bucketed correctly - it is not re-run as an automated
assertion, because "is this appearance or not" is ultimately a product judgment call, not a
mechanical fact the way "does this template contain a TemplateBinding" is.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$dataPath = Join-Path $PSScriptRoot 'UnsupportedProperties.psd1'
$auditRunsRoot = Join-Path $repoRoot 'artifacts\audit-runs'

if (-not (Test-Path -LiteralPath $dataPath -PathType Leaf)) {
    throw "Single source of truth file not found: $dataPath"
}

# ---------------------------------------------------------------------------
# Locate the newest evidence file. This is a 'local-runtime' gate: it is expected to be skipped
# (not failed) when nothing has produced runtime evidence yet.
# ---------------------------------------------------------------------------
if (-not (Test-Path -LiteralPath $auditRunsRoot -PathType Container)) {
    Write-Host "SKIP: Verify-SilentPropertyCoverage found no $auditRunsRoot directory yet. This gate needs a runtime evidence file produced by a local GUI Verify-ConsumerFixtures.ps1 pass (see scripts/Verify-RuntimeGates.ps1 / Publish-Internal.ps1). Run that first, then re-run this gate." -ForegroundColor Yellow
    exit 0
}

$evidenceDirs = @(Get-ChildItem -LiteralPath $auditRunsRoot -Directory -Filter 'consumer-runtime-evidence-*' | Sort-Object Name -Descending)
if ($evidenceDirs.Count -eq 0) {
    Write-Host "SKIP: Verify-SilentPropertyCoverage found $auditRunsRoot but no consumer-runtime-evidence-* subdirectory. This gate needs a runtime evidence file produced by a local GUI Verify-ConsumerFixtures.ps1 pass. Run that first, then re-run this gate." -ForegroundColor Yellow
    exit 0
}

$newestEvidenceDir = $evidenceDirs[0]
$evidencePath = Join-Path $newestEvidenceDir.FullName 'runtime-result.json'
if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
    Write-Host "SKIP: Verify-SilentPropertyCoverage found the newest evidence directory ($($newestEvidenceDir.Name)) but it has no runtime-result.json. Re-run a local GUI Verify-ConsumerFixtures.ps1 pass, then re-run this gate." -ForegroundColor Yellow
    exit 0
}

# ---------------------------------------------------------------------------
# Load the accounting data.
# ---------------------------------------------------------------------------
$data = Import-PowerShellDataFile -LiteralPath $dataPath
$trapEntries = @($data.Entries)
if ($trapEntries.Count -eq 0) {
    throw "$dataPath declares zero Entries; expected the curated 12-trap list."
}
if (-not $data.ContainsKey('AcknowledgedSilent')) {
    throw "$dataPath has no 'AcknowledgedSilent' key. R-07 requires the open accounting section alongside the closed 'Entries' list."
}
$acknowledgedEntries = @($data.AcknowledgedSilent)

$trapKeys = New-Object System.Collections.Generic.HashSet[string]
foreach ($entry in $trapEntries) {
    [void]$trapKeys.Add("$($entry.Control).$($entry.Property)")
}

$validBuckets = @('design-system-owned', 'platform-noop')
$failures = New-Object System.Collections.Generic.List[string]
$acknowledgedKeys = New-Object System.Collections.Generic.HashSet[string]
$acknowledgedByKey = @{}

foreach ($entry in $acknowledgedEntries) {
    if (-not $entry.ContainsKey('Property') -or [string]::IsNullOrWhiteSpace([string]$entry.Property)) {
        $failures.Add("AcknowledgedSilent has an entry with a missing/blank Property: $($entry | Out-String)")
        continue
    }
    $property = [string]$entry.Property
    if ($property -notmatch '^[A-Za-z0-9]+\.[A-Za-z0-9]+$') {
        $failures.Add("AcknowledgedSilent entry Property '$property' is not in 'Control.Property' form.")
    }
    if (-not $entry.ContainsKey('Bucket') -or [string]::IsNullOrWhiteSpace([string]$entry.Bucket)) {
        $failures.Add("AcknowledgedSilent entry '$property' has a missing/blank Bucket.")
    }
    elseif ($validBuckets -notcontains [string]$entry.Bucket) {
        $failures.Add("AcknowledgedSilent entry '$property' has Bucket '$($entry.Bucket)', which is not one of: $($validBuckets -join ', ').")
    }
    if (-not $acknowledgedKeys.Add($property)) {
        $failures.Add("Duplicate AcknowledgedSilent entry: $property")
    }
    else {
        $acknowledgedByKey[$property] = $entry
    }
    if ($trapKeys.Contains($property)) {
        $failures.Add("$property is listed in BOTH Entries (a trap) AND AcknowledgedSilent - it must only be in one section. Remove it from AcknowledgedSilent (Entries already covers it).")
    }
}

# ---------------------------------------------------------------------------
# Load evidence and collect platform-dp-contract properties.
# ---------------------------------------------------------------------------
$runtimeResultText = Get-Content -LiteralPath $evidencePath -Raw
$runtimeResult = $runtimeResultText | ConvertFrom-Json

$evidenceRecords = @($runtimeResult.attachedVisualProperties.evidence)
if ($evidenceRecords.Count -eq 0) {
    throw "$evidencePath has no attachedVisualProperties.Evidence records. This does not look like a valid consumer runtime evidence file."
}

$platformDpContractKeys = New-Object System.Collections.Generic.HashSet[string]
foreach ($record in $evidenceRecords) {
    if ([string]$record.method -eq 'platform-dp-contract') {
        [void]$platformDpContractKeys.Add([string]$record.property)
    }
}
if ($platformDpContractKeys.Count -eq 0) {
    throw "$evidencePath has zero Evidence records with Method == 'platform-dp-contract'. Expected at least the previously-known 156 - this looks like a stale or malformed evidence file, not zero real coverage."
}

# ---------------------------------------------------------------------------
# Forward check: every platform-dp-contract property must be accounted for (trap or acknowledged).
# ---------------------------------------------------------------------------
$unaccounted = New-Object System.Collections.Generic.List[string]
foreach ($property in $platformDpContractKeys) {
    if (-not $trapKeys.Contains($property) -and -not $acknowledgedKeys.Contains($property)) {
        $unaccounted.Add($property)
    }
}
foreach ($property in ($unaccounted | Sort-Object)) {
    $failures.Add(
        "$property is Method == 'platform-dp-contract' in $evidencePath (newest evidence) but is " +
        "neither one of the 12 Entries traps nor an AcknowledgedSilent entry in $dataPath. This is " +
        "a newly-discovered silently-ineffective property - a human must classify it: add it to " +
        "Entries with an Alternative if a consumer would reasonably expect it to work (a trap), or " +
        "to AcknowledgedSilent with Bucket='design-system-owned' (intentional appearance lock) or " +
        "Bucket='platform-noop' (low-level platform DP) otherwise. It is never auto-added.")
}

# ---------------------------------------------------------------------------
# Reverse check: every AcknowledgedSilent entry must still be platform-dp-contract in this evidence.
# ---------------------------------------------------------------------------
foreach ($property in ($acknowledgedKeys | Sort-Object)) {
    if (-not $platformDpContractKeys.Contains($property)) {
        $failures.Add(
            "AcknowledgedSilent lists $property in $dataPath, but $evidencePath (newest evidence) " +
            "does not classify it as Method == 'platform-dp-contract' any more (it may now be " +
            "genuinely wired/observable, or the property may no longer exist). Remove this stale " +
            "entry from AcknowledgedSilent - or, if it is now observable, that is a real product " +
            "change worth calling out, not just a list edit.")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Verify-SilentPropertyCoverage failures:" -ForegroundColor Red
    foreach ($f in $failures) { Write-Host "  - $f" -ForegroundColor Red }
    throw "Verify-SilentPropertyCoverage found $($failures.Count) issue(s). See above."
}

$designOwnedCount = @($acknowledgedEntries | Where-Object { [string]$_.Bucket -eq 'design-system-owned' }).Count
$platformNoopCount = @($acknowledgedEntries | Where-Object { [string]$_.Bucket -eq 'platform-noop' }).Count
Write-Host ("Verify-SilentPropertyCoverage passed: {0} platform-dp-contract propert{1} in {2} (newest evidence) all accounted for - {3} covered by the 12 closed-list traps, {4} in AcknowledgedSilent ({5} design-system-owned + {6} platform-noop). No stale AcknowledgedSilent entries." -f `
    $platformDpContractKeys.Count, `
    $(if ($platformDpContractKeys.Count -eq 1) { 'y' } else { 'ies' }), `
    $newestEvidenceDir.Name, `
    ($platformDpContractKeys.Count - $acknowledgedKeys.Count), `
    $acknowledgedKeys.Count, `
    $designOwnedCount, `
    $platformNoopCount)
