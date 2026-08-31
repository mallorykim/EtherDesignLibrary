<#
.SYNOPSIS
    Verifies that every property listed in scripts/UnsupportedProperties.psd1 (the single
    source of truth for "public, inherited, but silently ineffective" properties) is genuinely
    zero-consumption in its control's template, and that
    docs/consumers/getting-started.md documents exactly that same set - no more, no less.

.DESCRIPTION
    Two independent drifts are guarded against:
      1. Someone implements one of the listed properties (adds a TemplateBinding, or a named
         template part the property depends on) but forgets to remove it from the psd1 list -
         the list would then be lying about what is unsupported. Caught by the per-entry
         "still absent" check below.
      2. Someone edits the psd1 list (add/remove/rename an entry) but forgets to update the
         docs, or edits the docs table but forgets the psd1 - caught by the doc <-> psd1
         cross-check.

    This is a static, text/regex-based check against source files. It does not build or launch
    WinUI, so it can run on a hosted (non-GUI) runner.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$dataPath = Join-Path $PSScriptRoot 'UnsupportedProperties.psd1'
$docsPath = Join-Path $repoRoot 'docs\consumers\getting-started.md'

if (-not (Test-Path -LiteralPath $dataPath)) {
    throw "Single source of truth file not found: $dataPath"
}
if (-not (Test-Path -LiteralPath $docsPath)) {
    throw "Docs file not found: $docsPath"
}

$data = Import-PowerShellDataFile -LiteralPath $dataPath
$entries = @($data.Entries)
if ($entries.Count -eq 0) {
    throw "$dataPath declares zero entries; expected the curated unsupported-property list."
}

$failures = New-Object System.Collections.Generic.List[string]
$templateTextCache = @{}

function Get-TemplateText {
    param([string]$RelativePath)

    if (-not $templateTextCache.ContainsKey($RelativePath)) {
        $fullPath = Join-Path $repoRoot ($RelativePath -replace '/', '\')
        if (-not (Test-Path -LiteralPath $fullPath)) {
            throw "TemplateFile does not exist: $RelativePath"
        }
        $templateTextCache[$RelativePath] = Get-Content -LiteralPath $fullPath -Raw
    }
    return $templateTextCache[$RelativePath]
}

$seenKeys = New-Object System.Collections.Generic.HashSet[string]

foreach ($entry in $entries) {
    foreach ($required in 'Control', 'Property', 'BaseType', 'TemplateFile', 'CheckKind', 'Alternative') {
        if (-not $entry.ContainsKey($required) -or [string]::IsNullOrWhiteSpace([string]$entry[$required])) {
            $failures.Add("Entry missing required field '$required': $($entry | Out-String)")
        }
    }

    $key = "$($entry.Control).$($entry.Property)"
    if (-not $seenKeys.Add($key)) {
        $failures.Add("Duplicate entry in UnsupportedProperties.psd1: $key")
    }

    $text = Get-TemplateText -RelativePath $entry.TemplateFile

    switch ($entry.CheckKind) {
        'TemplateBindingAbsent' {
            $pattern = "\{TemplateBinding\s+$([regex]::Escape($entry.Property))\b"
            if ($text -match $pattern) {
                $failures.Add(
                    "$key is listed as unsupported (CheckKind=TemplateBindingAbsent) but " +
                    "$($entry.TemplateFile) contains '{TemplateBinding $($entry.Property)}'. " +
                    "The property has been implemented - remove this entry from UnsupportedProperties.psd1 " +
                    "and from docs/consumers/getting-started.md.")
            }
        }
        'TemplatePartAbsent' {
            if (-not $entry.ContainsKey('TemplatePartName') -or [string]::IsNullOrWhiteSpace([string]$entry.TemplatePartName)) {
                $failures.Add("$key has CheckKind=TemplatePartAbsent but no TemplatePartName.")
                continue
            }
            $partName = [regex]::Escape($entry.TemplatePartName)
            $pattern = "(?:x:Name|Name)\s*=\s*""$partName"""
            if ($text -match $pattern) {
                $failures.Add(
                    "$key is listed as unsupported (CheckKind=TemplatePartAbsent) but " +
                    "$($entry.TemplateFile) declares a part named `"$($entry.TemplatePartName)`". " +
                    "The property may now be functional - remove this entry from UnsupportedProperties.psd1 " +
                    "and from docs/consumers/getting-started.md, or re-verify.")
            }
        }
        default {
            $failures.Add("$key has unknown CheckKind '$($entry.CheckKind)'.")
        }
    }
}

# --- Cross-check against docs/consumers/getting-started.md ---
$docsText = Get-Content -LiteralPath $docsPath -Raw
$startMarker = '<!-- UNSUPPORTED-PROPERTIES:START -->'
$endMarker = '<!-- UNSUPPORTED-PROPERTIES:END -->'
$startIndex = $docsText.IndexOf($startMarker, [System.StringComparison]::Ordinal)
$endIndex = $docsText.IndexOf($endMarker, [System.StringComparison]::Ordinal)
if ($startIndex -lt 0 -or $endIndex -lt 0 -or $endIndex -le $startIndex) {
    $failures.Add(
        "docs/consumers/getting-started.md is missing the '$startMarker' / '$endMarker' " +
        "markers that bound the machine-checked unsupported-properties table.")
}
else {
    $section = $docsText.Substring($startIndex, $endIndex - $startIndex)
    $rowMatches = [regex]::Matches($section, '^\|\s*`(?<control>[^`]+)`\s*\|\s*`(?<property>[^`]+)`\s*\|', 'Multiline')
    $docKeys = New-Object System.Collections.Generic.HashSet[string]
    foreach ($m in $rowMatches) {
        $docKeys.Add("$($m.Groups['control'].Value).$($m.Groups['property'].Value)") | Out-Null
    }

    $canonicalKeys = @($entries | ForEach-Object { "$($_.Control).$($_.Property)" })

    $missingFromDocs = @($canonicalKeys | Where-Object { $_ -cnotin @($docKeys) })
    $extraInDocs = @($docKeys | Where-Object { $_ -cnotin $canonicalKeys })

    foreach ($m in $missingFromDocs) {
        $failures.Add("UnsupportedProperties.psd1 lists $m but docs/consumers/getting-started.md's known-boundaries table does not document it.")
    }
    foreach ($e in $extraInDocs) {
        $failures.Add("docs/consumers/getting-started.md documents $e as unsupported but UnsupportedProperties.psd1 has no such entry (single source of truth drift).")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Verify-UnsupportedProperties failures:" -ForegroundColor Red
    foreach ($f in $failures) { Write-Host "  - $f" -ForegroundColor Red }
    throw "Verify-UnsupportedProperties found $($failures.Count) issue(s). See above."
}

Write-Host "Verify-UnsupportedProperties passed: $($entries.Count) documented-unsupported properties across $(@($entries | Select-Object -ExpandProperty Control -Unique).Count) controls are all genuinely zero-consumption, and docs/consumers/getting-started.md matches UnsupportedProperties.psd1 exactly."
