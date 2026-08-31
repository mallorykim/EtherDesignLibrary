<#
.SYNOPSIS
Cross-checks every marker-field reference in Verify-ConsumerFixtures.ps1 against the C# record
declarations under tests/Ether.DesignSystem.ConsumerFixtures/ that actually produce the marker
JSON, so a PowerShell/C# field-name mismatch fails fast here instead of burning a full GUI
runtime-verification round.

.DESCRIPTION
Verify-ConsumerFixtures.ps1 reads the runtime harness's marker JSON (built by
RuntimeVerification.WriteMarker) through PowerShell's case-insensitive property access, e.g.
$runtimeResult.dropdown.displayMemberPathTriggerText or $dropdown.automationName. Nothing
statically confirms those dotted chains still line up with the C# records that shape the JSON.
When a C# record field is renamed but a PowerShell reference is not updated to match, the
mismatch is currently only caught by actually running the packaged runtime harness and reading
"The property 'x' cannot be found on this object." - a multi-minute GUI round trip.

This script parses:
  1. Every `record TypeName(...)` declaration under tests/Ether.DesignSystem.ConsumerFixtures/
     (recursively), building a field list per record type.
  2. RuntimeVerification.WriteMarker's anonymous JSON object, to learn which top-level marker
     field names exist and, where applicable, which VerificationResult property (and therefore
     which nested record type) backs each one.
  3. Every `$runtimeResult....` dotted chain, alias-variable assignment (`$dropdown =
     $runtimeResult.dropdown`), and `$_.field` pipeline reference inside
     scripts/Verify-ConsumerFixtures.ps1.

...and confirms each referenced field actually exists (case-insensitively - the JSON is
camelCase, the C# records are PascalCase) on the record type that chain resolves to. A mismatch
fails with the offending line and the record where the field was expected.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$verifyScriptPath = Join-Path $repoRoot 'scripts\Verify-ConsumerFixtures.ps1'
$fixturesDir = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures'
$runtimeVerificationCorePath = Join-Path $fixturesDir 'RuntimeVerification.cs'

foreach ($required in @($verifyScriptPath, $fixturesDir, $runtimeVerificationCorePath)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Marker contract audit could not find required path: $required"
    }
}

function Get-LineNumber {
    param([string]$Text, [int]$Index)
    if ($Index -le 0) { return 1 }
    $slice = $Text.Substring(0, [Math]::Min($Index, $Text.Length))
    return (($slice.ToCharArray() | Where-Object { $_ -eq "`n" }).Count + 1)
}

function Get-MatchingParenSpan {
    # $Text[$OpenIndex] must be '('. Returns the index just past the matching ')', or -1.
    param([string]$Text, [int]$OpenIndex)
    $depth = 0
    for ($i = $OpenIndex; $i -lt $Text.Length; $i++) {
        $ch = $Text[$i]
        if ($ch -eq '(') { $depth++ }
        elseif ($ch -eq ')') {
            $depth--
            if ($depth -eq 0) { return $i + 1 }
        }
    }
    return -1
}

function Split-TopLevel {
    # Splits $Text on top-level commas only, ignoring commas nested inside <...> or (...).
    param([string]$Text, [char]$Separator = ',')
    $parts = New-Object System.Collections.Generic.List[string]
    $depth = 0
    $current = New-Object System.Text.StringBuilder
    foreach ($ch in $Text.ToCharArray()) {
        if ($ch -eq '<' -or $ch -eq '(') { $depth++ }
        elseif ($ch -eq '>' -or $ch -eq ')') { $depth-- }
        if ($ch -eq $Separator -and $depth -eq 0) {
            [void]$parts.Add($current.ToString())
            $current = New-Object System.Text.StringBuilder
        }
        else {
            [void]$current.Append($ch)
        }
    }
    if ($current.Length -gt 0) { [void]$parts.Add($current.ToString()) }
    return $parts
}

function Resolve-ElementTypeName {
    param([string]$TypeText)
    $t = $TypeText.Trim()
    $t = $t.TrimEnd('?')
    if ($t.EndsWith('[]')) { $t = $t.Substring(0, $t.Length - 2).Trim() }
    if ($t.Contains('.')) { $t = $t.Substring($t.LastIndexOf('.') + 1) }
    return $t
}

# ---------------------------------------------------------------------------
# Pass 1: parse every `record TypeName( ... )` declaration into a field list.
# ---------------------------------------------------------------------------

# TypeName -> List of @{ Name = 'FieldName'; TypeText = 'string[]' }
$recordFields = @{}
$recordDeclPattern = [regex]'\brecord\s+(?<name>\w+)\s*\('

$csFiles = Get-ChildItem -LiteralPath $fixturesDir -Filter '*.cs' -File -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

if ($csFiles.Count -eq 0) {
    throw "Marker contract audit found no C# source files under $fixturesDir."
}

foreach ($file in $csFiles) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($match in $recordDeclPattern.Matches($text)) {
        $typeName = $match.Groups['name'].Value
        $openParenIndex = $match.Index + $match.Length - 1
        $closeSpan = Get-MatchingParenSpan -Text $text -OpenIndex $openParenIndex
        if ($closeSpan -lt 0) { continue }

        $paramText = $text.Substring($openParenIndex + 1, $closeSpan - 1 - ($openParenIndex + 1))
        $paramText = [regex]::Replace($paramText, '//[^\r\n]*', '')

        $fields = New-Object System.Collections.Generic.List[object]
        foreach ($rawParam in (Split-TopLevel -Text $paramText -Separator ',')) {
            $trimmed = $rawParam.Trim()
            if ([string]::IsNullOrWhiteSpace($trimmed)) { continue }
            # Drop a default-value expression, e.g. "string? Screen = null".
            $eqIndex = $trimmed.IndexOf('=')
            if ($eqIndex -ge 0) { $trimmed = $trimmed.Substring(0, $eqIndex).Trim() }
            $tokens = [regex]::Split($trimmed, '\s+') | Where-Object { $_ -ne '' }
            if ($tokens.Count -lt 2) { continue }
            $fieldName = $tokens[$tokens.Count - 1]
            $typeTokens = $tokens[0..($tokens.Count - 2)]
            [void]$fields.Add([pscustomobject]@{ Name = $fieldName; TypeText = ($typeTokens -join ' ') })
        }

        if (-not $recordFields.ContainsKey($typeName)) {
            $recordFields[$typeName] = $fields
        }
    }
}

if (-not $recordFields.ContainsKey('VerificationResult')) {
    throw "Marker contract audit could not locate the VerificationResult record declaration under $fixturesDir."
}

function Get-FieldMap {
    param([string]$TypeName)
    if (-not $recordFields.ContainsKey($TypeName)) { return $null }
    $map = @{}
    foreach ($f in $recordFields[$TypeName]) {
        $map[$f.Name.ToLowerInvariant()] = $f
    }
    return $map
}

# ---------------------------------------------------------------------------
# Pass 2: parse RuntimeVerification.WriteMarker's anonymous JSON object to learn
# the true top-level marker field set, and which VerificationResult property (if
# any) backs each one.
# ---------------------------------------------------------------------------

$coreText = Get-Content -LiteralPath $runtimeVerificationCorePath -Raw
$serializeIndex = $coreText.IndexOf('JsonSerializer.Serialize(new')
if ($serializeIndex -lt 0) {
    throw "Marker contract audit could not find the WriteMarker JsonSerializer.Serialize(new {...}) call in $runtimeVerificationCorePath."
}

$openBraceIndex = $coreText.IndexOf('{', $serializeIndex)
if ($openBraceIndex -lt 0) {
    throw "Marker contract audit could not find the opening brace of WriteMarker's anonymous object."
}

$depth = 0
$closeBraceIndex = -1
for ($i = $openBraceIndex; $i -lt $coreText.Length; $i++) {
    $ch = $coreText[$i]
    if ($ch -eq '{') { $depth++ }
    elseif ($ch -eq '}') {
        $depth--
        if ($depth -eq 0) { $closeBraceIndex = $i; break }
    }
}
if ($closeBraceIndex -lt 0) {
    throw "Marker contract audit could not find the closing brace of WriteMarker's anonymous object."
}

$anonymousBody = $coreText.Substring($openBraceIndex + 1, $closeBraceIndex - $openBraceIndex - 1)
$anonymousBody = [regex]::Replace($anonymousBody, '//[^\r\n]*', '')

# jsonFieldLower -> VerificationResult property name, or $null for a literal/computed field
# (e.g. `marker = "ETHER_CONSUMER_SMOKE"`) that has no nested record to drill into.
$jsonFieldToProperty = @{}
$jsonFieldOriginalCase = @{}

foreach ($rawEntry in (Split-TopLevel -Text $anonymousBody -Separator ',')) {
    $entry = $rawEntry.Trim()
    if ([string]::IsNullOrWhiteSpace($entry)) { continue }
    $eqIndex = $entry.IndexOf('=')
    if ($eqIndex -lt 0) { continue }
    $fieldName = $entry.Substring(0, $eqIndex).Trim()
    $expr = $entry.Substring($eqIndex + 1).Trim()
    if ([string]::IsNullOrWhiteSpace($fieldName)) { continue }

    $jsonFieldOriginalCase[$fieldName.ToLowerInvariant()] = $fieldName
    $propMatch = [regex]::Match($expr, 'result\?\.(?<prop>\w+)')
    if ($propMatch.Success) {
        $jsonFieldToProperty[$fieldName.ToLowerInvariant()] = $propMatch.Groups['prop'].Value
    }
    else {
        $jsonFieldToProperty[$fieldName.ToLowerInvariant()] = $null
    }
}

if ($jsonFieldToProperty.Count -eq 0) {
    throw "Marker contract audit parsed zero fields out of WriteMarker's anonymous object - the parser is out of sync with RuntimeVerification.cs."
}

# ---------------------------------------------------------------------------
# Chain resolution helpers.
# ---------------------------------------------------------------------------

$errors = New-Object System.Collections.Generic.List[string]

# Resolves a dotted chain rooted at $runtimeResult (segments AFTER the root, e.g. for
# "$runtimeResult.propertyConsumption.subscriptionValidatedEagerly" this is
# @('propertyConsumption','subscriptionValidatedEagerly')). Returns the type name the chain
# ends at (possibly array-suffixed, e.g. 'VisualPropertyEvidence[]'), or $null if a segment
# does not resolve (an error is recorded in that case). Returns '' (empty string) if the chain
# ends on a leaf/scalar field that has nothing further to validate.
function Resolve-RuntimeResultChain {
    param([string[]]$Segments, [string]$LineContext)

    if ($Segments.Count -eq 0) { return '' }

    $first = $Segments[0].ToLowerInvariant()
    if (-not $jsonFieldToProperty.ContainsKey($first)) {
        $errors.Add("$LineContext : `$runtimeResult.$($Segments[0]) - '$($Segments[0])' is not a field WriteMarker puts in the marker JSON (see RuntimeVerification.cs WriteMarker).")
        return $null
    }

    $propName = $jsonFieldToProperty[$first]

    if (-not $propName) {
        if ($Segments.Count -eq 1) { return '' }
        $errors.Add("$LineContext : `$runtimeResult.$($Segments[0]).$($Segments[1]) - '$($Segments[0])' is a literal/computed marker field with no nested record, but the script accesses '.$($Segments[1])' on it.")
        return $null
    }

    $vrMap = Get-FieldMap 'VerificationResult'
    $vrField = $vrMap[$propName.ToLowerInvariant()]
    if (-not $vrField) {
        $errors.Add("$LineContext : WriteMarker maps '$($Segments[0])' to VerificationResult.$propName, but VerificationResult declares no such field (internal parser inconsistency - check RuntimeVerification.cs).")
        return $null
    }

    $typeName = Resolve-ElementTypeName $vrField.TypeText
    for ($i = 1; $i -lt $Segments.Count; $i++) {
        $seg = $Segments[$i]
        $fieldMap = Get-FieldMap $typeName
        if ($null -eq $fieldMap) {
            # Not a record we parsed (e.g. a primitive or an unmodeled type) - nothing further
            # to validate; treat as a pass rather than a false positive.
            return ''
        }
        $field = $fieldMap[$seg.ToLowerInvariant()]
        if (-not $field) {
            $chainSoFar = '$runtimeResult.' + ($Segments -join '.')
            $errors.Add("$LineContext : $chainSoFar - record '$typeName' has no field '$seg' (expected a matching property in a record $typeName(...) under tests/Ether.DesignSystem.ConsumerFixtures/).")
            return $null
        }
        $typeName = Resolve-ElementTypeName $field.TypeText
    }

    return $typeName
}

# ---------------------------------------------------------------------------
# Pass 3: scan Verify-ConsumerFixtures.ps1 for marker-field references.
# ---------------------------------------------------------------------------

$scriptText = Get-Content -LiteralPath $verifyScriptPath -Raw
$scriptLines = Get-Content -LiteralPath $verifyScriptPath

function Format-Location {
    param([string]$Text, [int]$Index)
    $line = Get-LineNumber -Text $Text -Index $Index
    return "Verify-ConsumerFixtures.ps1:$line"
}

# --- 3a: every `$runtimeResult(.segment)+` chain -------------------------------------------

$runtimeResultChainPattern = [regex]'\$runtimeResult((?:\.\w+)+)'
foreach ($match in $runtimeResultChainPattern.Matches($scriptText)) {
    $segments = $match.Groups[1].Value.TrimStart('.').Split('.')
    $location = Format-Location -Text $scriptText -Index $match.Index
    [void](Resolve-RuntimeResultChain -Segments $segments -LineContext $location)
}

# --- 3b: alias-variable assignments: $var = $runtimeResult.field  (single hop) -------------

$simpleAliasType = @{}   # varNameLower -> type name (possibly array-suffixed)
$aliasAssignPattern = [regex]'(?m)^\s*\$(?<var>\w+)\s*=\s*\$runtimeResult\.(?<field>\w+)\s*$'
foreach ($match in $aliasAssignPattern.Matches($scriptText)) {
    $location = Format-Location -Text $scriptText -Index $match.Index
    $typeName = Resolve-RuntimeResultChain -Segments @($match.Groups['field'].Value) -LineContext $location
    if ($typeName) {
        $simpleAliasType[$match.Groups['var'].Value.ToLowerInvariant()] = $typeName
    }
}

# --- 3c: array-wrap alias assignments: $var = @($runtimeResult.field) ----------------------

$arrayAliasElementType = @{}   # varNameLower -> element type name
$arrayAliasAssignPattern = [regex]'\$(?<var>\w+)\s*=\s*@\(\s*\$runtimeResult\.(?<field>\w+)\s*\)'
foreach ($match in $arrayAliasAssignPattern.Matches($scriptText)) {
    $location = Format-Location -Text $scriptText -Index $match.Index
    $typeName = Resolve-RuntimeResultChain -Segments @($match.Groups['field'].Value) -LineContext $location
    if ($typeName) {
        $arrayAliasElementType[$match.Groups['var'].Value.ToLowerInvariant()] = Resolve-ElementTypeName $typeName
    }
}

# --- 3d: field access on alias variables: $dropdown.field, $checkbox.field, ... ------------

foreach ($varName in $simpleAliasType.Keys) {
    $typeName = $simpleAliasType[$varName]
    $pattern = [regex]::new('\$' + [regex]::Escape($varName) + '\.(\w+)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    foreach ($match in $pattern.Matches($scriptText)) {
        $field = $match.Groups[1].Value
        $location = Format-Location -Text $scriptText -Index $match.Index
        $fieldMap = Get-FieldMap $typeName
        if ($null -eq $fieldMap) { continue }
        if (-not $fieldMap.ContainsKey($field.ToLowerInvariant())) {
            $errors.Add("$location : `$$varName.$field - record '$typeName' has no field '$field' (expected a matching property in a record $typeName(...) under tests/Ether.DesignSystem.ConsumerFixtures/).")
        }
    }
}

# --- 3e: `$_.field` inside a Where-Object/ForEach-Object pipeline sourced from a resolvable
#         marker chain or alias, e.g. `$runtimeResult.attachedVisualProperties.evidence |
#         Where-Object { ... $_.property ... }` or `$reportedAssets | Where-Object { $_.size }`.

$pipelinePattern = [regex]'(?<source>\$[\w\.]+)\s*\|\s*(?:Where-Object|ForEach-Object)\s*\{(?<body>[^{}]*)\}'
foreach ($lineIndex in 0..($scriptLines.Count - 1)) {
    $line = $scriptLines[$lineIndex]
    $lineNumber = $lineIndex + 1
    foreach ($match in $pipelinePattern.Matches($line)) {
        $sourceExpr = $match.Groups['source'].Value
        $body = $match.Groups['body'].Value

        $elementType = $null
        if ($sourceExpr -match '^\$runtimeResult((?:\.\w+)+)$') {
            $segments = $Matches[1].TrimStart('.').Split('.')
            $resolved = Resolve-RuntimeResultChain -Segments $segments -LineContext "Verify-ConsumerFixtures.ps1:$lineNumber"
            if ($resolved) { $elementType = Resolve-ElementTypeName $resolved }
        }
        else {
            $bareVar = $sourceExpr.TrimStart('$').ToLowerInvariant()
            if ($arrayAliasElementType.ContainsKey($bareVar)) {
                $elementType = $arrayAliasElementType[$bareVar]
            }
            elseif ($simpleAliasType.ContainsKey($bareVar)) {
                $elementType = Resolve-ElementTypeName $simpleAliasType[$bareVar]
            }
        }

        if (-not $elementType) { continue }
        $fieldMap = Get-FieldMap $elementType
        if ($null -eq $fieldMap) { continue }

        foreach ($fieldMatch in [regex]::Matches($body, '\$_\.(\w+)')) {
            $field = $fieldMatch.Groups[1].Value
            if (-not $fieldMap.ContainsKey($field.ToLowerInvariant())) {
                $errors.Add("Verify-ConsumerFixtures.ps1:$lineNumber : `$_.$field (piped from $sourceExpr) - record '$elementType' has no field '$field' (expected a matching property in a record $elementType(...) under tests/Ether.DesignSystem.ConsumerFixtures/).")
            }
        }
    }
}

# ---------------------------------------------------------------------------
# Report.
# ---------------------------------------------------------------------------

if ($errors.Count -gt 0) {
    Write-Host "Marker contract audit found $($errors.Count) mismatch(es) between Verify-ConsumerFixtures.ps1 and the C# marker records:" -ForegroundColor Red
    foreach ($e in ($errors | Select-Object -Unique)) {
        Write-Host "  - $e" -ForegroundColor Red
    }
    throw "Marker contract audit failed: Verify-ConsumerFixtures.ps1 references at least one field that does not exist on the corresponding C# record. Fix the PowerShell reference (or the C# record) so they agree, then rerun."
}

Write-Host "Marker contract audit passed: every `$runtimeResult / alias-variable / `$_ field reference in Verify-ConsumerFixtures.ps1 matches a declared field on its C# record."
