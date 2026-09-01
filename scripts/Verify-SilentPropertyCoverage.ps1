<#
.SYNOPSIS
Open-ended detection gate for R-07: fails the build the moment a public, inherited property is
silently ineffective (Method == 'platform-dp-contract' in the newest runtime evidence) and is not
yet accounted for anywhere - instead of only re-checking a fixed, closed list of 12 known traps.
Also self-verifies (R-07 refinement) that the accounting does not mislabel a genuinely-consumed
or genuinely-functional property as "silently ineffective".

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
     each bucketed into one of FIVE buckets - see that file's header comment for the full rule
     and the R-07 REFINEMENT note explaining why two buckets is not enough).
  2. Loads the NEWEST artifacts/audit-runs/consumer-runtime-evidence-*/runtime-result.json (by
     the timestamp encoded in the directory name - consumer-runtime-evidence-yyyyMMdd-HHmmssfff)
     and collects every property whose attachedVisualProperties.Evidence entry has
     Method == 'platform-dp-contract' (a real, rendered, attached control's DP round-tripped
     without throwing, but its RenderTargetBitmap did not change). IMPORTANT: this Method means
     "the visual gate observed no pixel difference" - it does NOT by itself mean "the property is
     ineffective". A property can be platform-dp-contract and still be genuinely consumed (via a
     TemplateBinding whose probed value happened not to shift rendering) or genuinely functional
     (via WinUI base-class behavior the template never needs to touch). Distinguishing those cases
     is exactly what the TemplateBinding cross-check (step 6) exists for.
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
     Property in 'Control.Property' form, a non-empty Reason, and a Bucket that is exactly one of
     'design-system-owned', 'consumed-visually-stable', 'behavioral', 'platform-noop', or
     'needs-review'; no duplicate Property values; no property is claimed by both Entries and
     AcknowledgedSilent at once (that would make the accounting ambiguous about which section
     owns it).
  6. TEMPLATEBINDING CROSS-CHECK (R-07 refinement - this is the fix for the mislabeling bug): for
     every AcknowledgedSilent entry whose Bucket is 'design-system-owned' or
     'consumed-visually-stable', this script looks up the entry's Control in
     UnsupportedProperties.psd1's TemplateFiles map, reads that ControlTemplate .xaml, and checks
     for the literal token `{TemplateBinding <Property>}` - the SAME regex
     scripts/Verify-UnsupportedProperties.ps1 already uses for the 9 closed Entries
     (`\{TemplateBinding\s+<Property>\b`). The label must agree with what is actually in the file:
       - 'design-system-owned' + a TemplateBinding IS found -> FAIL (mislabeled: it is actually
         consumed by the template; should be 'consumed-visually-stable')
       - 'consumed-visually-stable' + NO TemplateBinding found -> FAIL (mislabeled: it is not
         actually consumed; should be 'design-system-owned' or 'platform-noop')
       - a Control with no TemplateFiles entry -> FAIL (cannot verify the label at all, so it may
         not claim design-system-owned/consumed-visually-stable without a template to check
         against; 'behavioral'/'platform-noop'/'needs-review' do not require this lookup)
     'behavioral' and 'needs-review' entries are NOT TemplateBinding-checked (by definition they
     are not claiming template consumption either way) - but 'needs-review' entries are printed as
     a loud WARNING (not a failure) so a human can see the open questions.

This is a 'local-runtime' gate (see scripts/Gates.psd1): it needs a runtime evidence file that
only a local GUI/desktop Verify-ConsumerFixtures.ps1 pass produces, so hosted CI cannot run it.
If no evidence directory exists yet, this script SKIPS with an explicit, loud message and exits 0
- it does not silently pass by treating "no evidence" the same as "nothing to check".

.PARAMETER EvidenceDir
R-12 freshness fix: full path to a specific artifacts/audit-runs/consumer-runtime-evidence-*
directory to grade, instead of scanning artifacts/audit-runs/ for whatever is newest on disk.
Falls back to $env:ETHER_CONSUMER_EVIDENCE_DIR when not passed. A caller that just ran
Verify-ConsumerFixtures.ps1 itself (Publish-Internal.ps1, Verify-RuntimeGates.ps1) should always
pass this - it is the only way to guarantee this gate grades the evidence THIS run produced rather
than a stale directory left over from an earlier run (the newest-on-disk scan below cannot tell
the difference). When given, a missing directory or missing runtime-result.json is a HARD FAILURE
(not a skip): the caller asserted this evidence exists, so a scan-and-skip fallback would silently
defeat the whole point of passing the hint. Omit this (and the env var) for a standalone/manual
run - the original newest-on-disk behavior is preserved unchanged for that case.

.PARAMETER MinCreationTimeUtc
Optional belt-and-suspenders staleness guard, only meaningful together with -EvidenceDir. An ISO
8601 / round-trip ('o') timestamp string; falls back to $env:ETHER_CONSUMER_EVIDENCE_MIN_UTC. When
given, the evidence directory named by -EvidenceDir must have a CreationTimeUtc at or after this
timestamp, or the gate FAILS loudly - protecting against a future caller bug that resolves
-EvidenceDir to a directory older than the run that was supposed to have produced it. Never
required, and never applied when -EvidenceDir is not also given, so a standalone/manual run is
never failed just because its evidence happens to be old.
#>

[CmdletBinding()]
param(
    [string]$EvidenceDir,
    [string]$MinCreationTimeUtc
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$dataPath = Join-Path $PSScriptRoot 'UnsupportedProperties.psd1'
$auditRunsRoot = Join-Path $repoRoot 'artifacts\audit-runs'

if (-not (Test-Path -LiteralPath $dataPath -PathType Leaf)) {
    throw "Single source of truth file not found: $dataPath"
}

if ([string]::IsNullOrWhiteSpace($EvidenceDir)) {
    $EvidenceDir = $env:ETHER_CONSUMER_EVIDENCE_DIR
}
if ([string]::IsNullOrWhiteSpace($MinCreationTimeUtc)) {
    $MinCreationTimeUtc = $env:ETHER_CONSUMER_EVIDENCE_MIN_UTC
}
$hintedMode = -not [string]::IsNullOrWhiteSpace($EvidenceDir)

# ---------------------------------------------------------------------------
# Locate the evidence file. HINTED MODE (-EvidenceDir / $env:ETHER_CONSUMER_EVIDENCE_DIR): the
# caller says this is the exact evidence this run produced, so a missing directory or missing
# runtime-result.json is a hard failure, not a skip - falling back to "newest on disk" here would
# silently reintroduce the R-12 staleness bug this parameter exists to close. STANDALONE MODE (no
# hint): scan artifacts/audit-runs/ for the newest consumer-runtime-evidence-* directory, and SKIP
# (not fail) when nothing has produced runtime evidence yet - unchanged from before R-12.
# ---------------------------------------------------------------------------
if ($hintedMode) {
    if (-not (Test-Path -LiteralPath $EvidenceDir -PathType Container)) {
        throw "Verify-SilentPropertyCoverage was given an evidence directory hint ('$EvidenceDir', via -EvidenceDir or `$env:ETHER_CONSUMER_EVIDENCE_DIR) that does not exist. The caller (Publish-Internal.ps1 / Verify-RuntimeGates.ps1) says this is the evidence directory THIS run just produced - failing loudly instead of silently falling back to 'newest on disk', which would defeat the entire point of the hint."
    }
    $newestEvidenceDir = Get-Item -LiteralPath $EvidenceDir
    $evidencePath = Join-Path $newestEvidenceDir.FullName 'runtime-result.json'
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "Verify-SilentPropertyCoverage was given evidence directory hint '$($newestEvidenceDir.FullName)' but it has no runtime-result.json."
    }
    if (-not [string]::IsNullOrWhiteSpace($MinCreationTimeUtc)) {
        $minUtc = [DateTime]::Parse($MinCreationTimeUtc, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind)
        $minUtc = $minUtc.ToUniversalTime()
        if ($newestEvidenceDir.CreationTimeUtc -lt $minUtc) {
            throw "Verify-SilentPropertyCoverage's evidence directory hint '$($newestEvidenceDir.FullName)' (created $($newestEvidenceDir.CreationTimeUtc.ToString('o'))) predates this run's start ($($minUtc.ToString('o'))) given via -MinCreationTimeUtc. The caller asserted this evidence was produced by THIS run - a directory older than that means the freshness guarantee was violated somewhere upstream (this is the R-12 staleness guard). Do not investigate by removing -MinCreationTimeUtc; re-run the whole gate chain."
        }
    }
    Write-Host "Verify-SilentPropertyCoverage: using hinted evidence directory (not a newest-on-disk scan): $($newestEvidenceDir.FullName)"
}
else {
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
if (-not $data.ContainsKey('TemplateFiles')) {
    throw "$dataPath has no 'TemplateFiles' key. R-07 refinement requires a Control -> ControlTemplate map so 'design-system-owned' / 'consumed-visually-stable' labels can be TemplateBinding-cross-checked."
}
$templateFiles = $data.TemplateFiles

$trapKeys = New-Object System.Collections.Generic.HashSet[string]
foreach ($entry in $trapEntries) {
    [void]$trapKeys.Add("$($entry.Control).$($entry.Property)")
}

$validBuckets = @('design-system-owned', 'consumed-visually-stable', 'behavioral', 'platform-noop', 'needs-review')
$bucketsRequiringTemplateCheck = @('design-system-owned', 'consumed-visually-stable')
$failures = New-Object System.Collections.Generic.List[string]
$acknowledgedKeys = New-Object System.Collections.Generic.HashSet[string]
$acknowledgedByKey = @{}
$needsReviewProperties = New-Object System.Collections.Generic.List[string]

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
    if (-not $entry.ContainsKey('Reason') -or [string]::IsNullOrWhiteSpace([string]$entry.Reason)) {
        $failures.Add("AcknowledgedSilent entry '$property' has a missing/blank Reason. Every entry must explain why it is bucketed the way it is.")
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
    if ([string]$entry.Bucket -eq 'needs-review') {
        $needsReviewProperties.Add($property)
    }
}

# ---------------------------------------------------------------------------
# TemplateBinding cross-check (R-07 refinement): 'design-system-owned' and
# 'consumed-visually-stable' are claims about whether the control's template actually contains
# `{TemplateBinding <Property>}` - re-derive that from the template file text instead of trusting
# the Bucket label, using the same regex scripts/Verify-UnsupportedProperties.ps1 uses for Entries.
# ---------------------------------------------------------------------------
$templateTextCache = @{}
function Get-CoverageTemplateText {
    param([string]$RelativePath)
    if (-not $templateTextCache.ContainsKey($RelativePath)) {
        $fullPath = Join-Path $repoRoot ($RelativePath -replace '/', '\')
        if (-not (Test-Path -LiteralPath $fullPath)) {
            throw "TemplateFiles entry points at a file that does not exist: $RelativePath"
        }
        $templateTextCache[$RelativePath] = Get-Content -LiteralPath $fullPath -Raw
    }
    return $templateTextCache[$RelativePath]
}

foreach ($property in $acknowledgedKeys) {
    $entry = $acknowledgedByKey[$property]
    $bucket = [string]$entry.Bucket
    if ($bucketsRequiringTemplateCheck -notcontains $bucket) {
        continue
    }
    $parts = $property -split '\.', 2
    $control = $parts[0]
    $propName = $parts[1]

    if (-not $templateFiles.ContainsKey($control)) {
        $failures.Add(
            "$property is bucketed '$bucket' but TemplateFiles in $dataPath has no entry for control " +
            "'$control', so the TemplateBinding claim cannot be verified. Add a TemplateFiles['$control'] " +
            "mapping, or re-bucket this entry as 'behavioral'/'platform-noop'/'needs-review' if the " +
            "control has no ControlTemplate to check (e.g. it is a Panel, not a templated Control).")
        continue
    }

    $templateFile = [string]$templateFiles[$control]
    $templateText = Get-CoverageTemplateText -RelativePath $templateFile
    $pattern = "\{TemplateBinding\s+$([regex]::Escape($propName))\b"
    $hasTemplateBinding = $templateText -match $pattern

    if ($bucket -eq 'design-system-owned' -and $hasTemplateBinding) {
        $failures.Add(
            "$property is labeled 'design-system-owned' but $templateFile contains " +
            "'{TemplateBinding $propName}' - the property IS consumed by the template. Re-bucket it " +
            "as 'consumed-visually-stable'.")
    }
    elseif ($bucket -eq 'consumed-visually-stable' -and -not $hasTemplateBinding) {
        $failures.Add(
            "$property is labeled 'consumed-visually-stable' but $templateFile does NOT contain " +
            "'{TemplateBinding $propName}' - the property is not actually consumed by the template. " +
            "Re-bucket it as 'design-system-owned' (if it is an appearance property) or 'platform-noop'.")
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
        "a newly-discovered silently-ineffective-LOOKING property - a human must classify it: add " +
        "it to Entries with an Alternative if a consumer would reasonably expect it to work (a " +
        "trap), or to AcknowledgedSilent with one of Bucket='design-system-owned' (appearance " +
        "property, confirmed no TemplateBinding)/'consumed-visually-stable' (confirmed IS " +
        "TemplateBound - the pixel-diff probe just did not catch it)/'behavioral' (base-class " +
        "behavior, not template-dependent)/'platform-noop' (low-level platform DP)/'needs-review' " +
        "(genuinely uncertain) plus a Reason explaining the classification. It is never auto-added.")
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
$consumedStableCount = @($acknowledgedEntries | Where-Object { [string]$_.Bucket -eq 'consumed-visually-stable' }).Count
$behavioralCount = @($acknowledgedEntries | Where-Object { [string]$_.Bucket -eq 'behavioral' }).Count
$platformNoopCount = @($acknowledgedEntries | Where-Object { [string]$_.Bucket -eq 'platform-noop' }).Count
$needsReviewCount = $needsReviewProperties.Count

Write-Host ("Verify-SilentPropertyCoverage passed: {0} platform-dp-contract propert{1} in {2} (newest evidence) all accounted for - {3} covered by the 9 closed-list traps, {4} in AcknowledgedSilent ({5} design-system-owned + {6} consumed-visually-stable + {7} behavioral + {8} platform-noop + {9} needs-review). No stale AcknowledgedSilent entries. TemplateBinding cross-check passed on every design-system-owned/consumed-visually-stable label." -f `
    $platformDpContractKeys.Count, `
    $(if ($platformDpContractKeys.Count -eq 1) { 'y' } else { 'ies' }), `
    $newestEvidenceDir.Name, `
    ($platformDpContractKeys.Count - $acknowledgedKeys.Count), `
    $acknowledgedKeys.Count, `
    $designOwnedCount, `
    $consumedStableCount, `
    $behavioralCount, `
    $platformNoopCount, `
    $needsReviewCount)

if ($needsReviewProperties.Count -gt 0) {
    Write-Host ""
    Write-Host "WARNING: $($needsReviewProperties.Count) AcknowledgedSilent entr$(if ($needsReviewProperties.Count -eq 1) { 'y is' } else { 'ies are' }) bucketed 'needs-review' - the TemplateBinding cross-check could not confidently place them as behavioral or design-system-owned. Not a failure, but pending a human decision (see each entry's Reason in $dataPath):" -ForegroundColor Yellow
    foreach ($property in ($needsReviewProperties | Sort-Object)) {
        Write-Host "  - $property" -ForegroundColor Yellow
    }
}
