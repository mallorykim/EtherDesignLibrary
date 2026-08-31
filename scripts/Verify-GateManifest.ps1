<#
.SYNOPSIS
Anti-drift gate for scripts/Gates.psd1 (R-02): fails the build the moment the single-source-of-
truth gate manifest and its consumers disagree, instead of letting them silently drift the way
the four hand-maintained copies (build.yml, Publish-Internal.ps1's hardcoded "22", Verify-
RuntimeGates.ps1, docs) already had.

.DESCRIPTION
Three independent checks, each a hard failure on its own:

  1. Every Gates[].Script and UnmanifestedScripts[].Script entry in scripts/Gates.psd1 must exist
     as a file under scripts/. Every scripts/Verify-*.ps1 file on disk must appear as a Script
     value somewhere in the manifest (either as a Gates entry or an UnmanifestedScripts entry with
     a non-empty Reason) - so a newly added gate script can never be silently ignored by every
     consumer at once.

  2. The set of scripts/Verify-*.ps1 the manifest tags 'ci', in manifest order, must exactly equal
     the set of "./scripts/Verify-*.ps1" run: lines actually present in
     .github/workflows/build.yml's package-consumers job, in the order they run. build.yml is YAML
     and cannot read the .psd1 at author time, so its steps stay written out explicitly and are
     validated against the manifest here instead of being generated from it.

  3. This script itself (Verify-GateManifest.ps1) must have a Gates entry tagged 'ci' in the
     manifest - it is a gate, and it must not be able to remove itself from the list it enforces
     without failing check #1/#2 the same way any other gate script would.

This script does not execute any of the gate scripts it inventories - it only reads
scripts/Gates.psd1 and .github/workflows/build.yml as data.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$scriptsDir = Join-Path $repoRoot 'scripts'
$manifestPath = Join-Path $scriptsDir 'Gates.psd1'
$workflowPath = Join-Path $repoRoot '.github\workflows\build.yml'
$thisScriptName = Split-Path -Path $PSCommandPath -Leaf

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Gate manifest is missing: $manifestPath"
}
if (-not (Test-Path -LiteralPath $workflowPath -PathType Leaf)) {
    throw "Workflow file is missing: $workflowPath"
}

$manifest = Import-PowerShellDataFile -Path $manifestPath
if (-not $manifest.ContainsKey('Gates')) {
    throw "$manifestPath is missing its top-level 'Gates' key."
}
$gates = @($manifest.Gates)
$unmanifested = @()
if ($manifest.ContainsKey('UnmanifestedScripts')) {
    $unmanifested = @($manifest.UnmanifestedScripts)
}

$violations = New-Object System.Collections.Generic.List[string]

# ---------------------------------------------------------------------------
# Check 1a: every declared Script (Gates + UnmanifestedScripts) exists on disk.
# ---------------------------------------------------------------------------
foreach ($gate in $gates) {
    $gatePath = Join-Path $scriptsDir $gate.Script
    if (-not (Test-Path -LiteralPath $gatePath -PathType Leaf)) {
        $violations.Add("Gates entry '$($gate.Name)' references '$($gate.Script)', which does not exist under $scriptsDir.")
    }
}
foreach ($entry in $unmanifested) {
    $entryPath = Join-Path $scriptsDir $entry.Script
    if (-not (Test-Path -LiteralPath $entryPath -PathType Leaf)) {
        $violations.Add("UnmanifestedScripts entry '$($entry.Script)' does not exist under $scriptsDir.")
    }
    if ([string]::IsNullOrWhiteSpace($entry.Reason)) {
        $violations.Add("UnmanifestedScripts entry '$($entry.Script)' has no Reason. Every intentionally-unmanifested script must say why.")
    }
}

# ---------------------------------------------------------------------------
# Check 1b: every scripts/Verify-*.ps1 on disk is accounted for exactly once,
# either as a Gates[].Script or an UnmanifestedScripts[].Script.
# ---------------------------------------------------------------------------
$manifestedScriptNames = New-Object System.Collections.Generic.HashSet[string]
foreach ($gate in $gates) { [void]$manifestedScriptNames.Add($gate.Script) }
foreach ($entry in $unmanifested) { [void]$manifestedScriptNames.Add($entry.Script) }

$onDisk = @(Get-ChildItem -LiteralPath $scriptsDir -Filter 'Verify-*.ps1' | ForEach-Object { $_.Name } | Sort-Object)
foreach ($fileName in $onDisk) {
    if (-not $manifestedScriptNames.Contains($fileName)) {
        $violations.Add("$fileName exists under $scriptsDir but is neither a Gates entry nor an UnmanifestedScripts entry in $manifestPath. Add it to one of the two - a new gate script must not be silently unaccounted for.")
    }
}

# ---------------------------------------------------------------------------
# Check 2: manifest's 'ci'-tagged gates (in declared order) must exactly match
# build.yml's package-consumers job "./scripts/Verify-*.ps1" run lines (in run order).
# ---------------------------------------------------------------------------
$manifestCiScripts = @($gates | Where-Object { @($_.Environments) -contains 'ci' } | ForEach-Object { $_.Script })

$workflowLines = Get-Content -LiteralPath $workflowPath
$jobStartIndex = -1
for ($i = 0; $i -lt $workflowLines.Count; $i++) {
    if ($workflowLines[$i] -match '^  package-consumers:\s*$') {
        $jobStartIndex = $i
        break
    }
}
if ($jobStartIndex -lt 0) {
    throw "Could not find a 'package-consumers:' job at 2-space indentation in $workflowPath. This check needs to know where that job's steps start."
}

$jobEndIndex = $workflowLines.Count - 1
for ($i = $jobStartIndex + 1; $i -lt $workflowLines.Count; $i++) {
    if ($workflowLines[$i] -match '^  \S.*:\s*$') {
        $jobEndIndex = $i - 1
        break
    }
}

$workflowCiScripts = New-Object System.Collections.Generic.List[string]
for ($i = $jobStartIndex; $i -le $jobEndIndex; $i++) {
    if ($workflowLines[$i] -match 'run:\s*\./scripts/(Verify-[A-Za-z0-9]+\.ps1)') {
        $workflowCiScripts.Add($Matches[1])
    }
}

$manifestCiSet = @($manifestCiScripts | Sort-Object -Unique)
$workflowCiSet = @($workflowCiScripts | Sort-Object -Unique)
$onlyInManifest = @(Compare-Object -ReferenceObject $manifestCiSet -DifferenceObject $workflowCiSet | Where-Object { $_.SideIndicator -eq '<=' } | ForEach-Object { $_.InputObject })
$onlyInWorkflow = @(Compare-Object -ReferenceObject $manifestCiSet -DifferenceObject $workflowCiSet | Where-Object { $_.SideIndicator -eq '=>' } | ForEach-Object { $_.InputObject })

if ($onlyInManifest.Count -gt 0) {
    $violations.Add("Gates.psd1 tags these scripts 'ci' but build.yml's package-consumers job does not run them: $($onlyInManifest -join ', ')")
}
if ($onlyInWorkflow.Count -gt 0) {
    $violations.Add("build.yml's package-consumers job runs these scripts but Gates.psd1 does not tag them 'ci': $($onlyInWorkflow -join ', ')")
}

if ($onlyInManifest.Count -eq 0 -and $onlyInWorkflow.Count -eq 0) {
    if ($manifestCiScripts.Count -ne $workflowCiScripts.Count) {
        $violations.Add("Gates.psd1 'ci' entries ($($manifestCiScripts.Count)) and build.yml run: lines ($($workflowCiScripts.Count)) contain the same set of scripts but a different COUNT of entries - a script is duplicated in one but not the other.")
    }
    else {
        for ($i = 0; $i -lt $manifestCiScripts.Count; $i++) {
            if ($manifestCiScripts[$i] -ne $workflowCiScripts[$i]) {
                $violations.Add("Order mismatch at position $($i + 1): Gates.psd1 has '$($manifestCiScripts[$i])', build.yml runs '$($workflowCiScripts[$i])'.")
                break
            }
        }
    }
}

# ---------------------------------------------------------------------------
# Check 3: this script must be a 'ci' gate in the manifest it enforces.
# ---------------------------------------------------------------------------
$selfGate = @($gates | Where-Object { $_.Script -eq $thisScriptName -and (@($_.Environments) -contains 'ci') })
if ($selfGate.Count -eq 0) {
    $violations.Add("$thisScriptName has no Gates entry tagged 'ci' in $manifestPath. This script must be a manifest-declared 'ci' gate (and a build.yml step) so it cannot remove itself from what it enforces.")
}

# ---------------------------------------------------------------------------
# Check 4 (R-04): every Gates[].Args must be a [hashtable]. Gates.psd1's own header
# comment (see lines 12-25) requires hashtable splat for every consumer - an array
# (e.g. @('-RequireHighContrastParity')) or a bare dash-prefixed string binds
# POSITIONALLY when splatted, silently leaving switches $false while the string
# lands in the first positional parameter. That exact defect is what let the
# HighContrast parity gate run disabled while writing a JSON audit to a repo-root
# file literally named "-RequireHighContrastParity". Asserting the type here means
# a future entry cannot reintroduce that defect without failing this gate.
# ---------------------------------------------------------------------------
foreach ($gate in $gates) {
    if ($null -eq $gate.Args) {
        $violations.Add("Gates entry '$($gate.Name)' has no 'Args' key. Every entry must declare Args as a hashtable (use @{} for no arguments).")
        continue
    }
    if (-not ($gate.Args -is [hashtable])) {
        $actualType = $gate.Args.GetType().FullName
        $violations.Add("Gates entry '$($gate.Name)' has Args of type '$actualType', not [hashtable]. Args must always be a hashtable (e.g. @{} or @{ SomeSwitch = `$true }) - an array or bare string splats POSITIONALLY and can silently leave switches unbound (R-04).")
    }
}

if ($violations.Count -gt 0) {
    Write-Host 'Verify-GateManifest failures:' -ForegroundColor Red
    foreach ($violation in $violations) { Write-Host "  - $violation" -ForegroundColor Red }
    throw "Verify-GateManifest found $($violations.Count) issue(s). scripts/Gates.psd1 and its consumers have drifted - see above."
}

Write-Host "Verify-GateManifest passed: $($gates.Count) manifest entries ($($manifestCiScripts.Count) tagged 'ci'), all with hashtable Args, $($unmanifested.Count) intentionally-unmanifested script(s), $($onDisk.Count) Verify-*.ps1 files on disk all accounted for, and Gates.psd1's 'ci' set/order matches build.yml's package-consumers job exactly."
