[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Interactions\Ether.DesignSystem.Interactions.csproj'
$contractsPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Interactions\InteractionContracts.cs'
$adapterPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Interactions\ControlInteractionAdapter.cs'
$testProjectPath = Join-Path $repoRoot 'tests\Ether.DesignSystem.Interactions.ContractTests\Ether.DesignSystem.Interactions.ContractTests.csproj'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'

function Assert-Contains {
    param([string]$Text, [string]$Pattern, [string]$Description)
    if ($Text -notmatch $Pattern) {
        throw "$Description is missing required pattern '$Pattern'."
    }
}

foreach ($path in @($projectPath, $contractsPath, $adapterPath, $testProjectPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required interaction-contract artifact is missing: $path"
    }
}

$project = Get-Content -LiteralPath $projectPath -Raw
$contracts = Get-Content -LiteralPath $contractsPath -Raw
$adapter = Get-Content -LiteralPath $adapterPath -Raw
$ci = Get-Content -LiteralPath $ciPath -Raw

Assert-Contains $project 'Ether.DesignSystem.Controls.csproj' 'Interactions project reference'
foreach ($field in 'EventId', 'IdempotencyKey', 'Type', 'SchemaVersion', 'OccurredAtUtc', 'CorrelationId', 'ComponentId', 'Data') {
    Assert-Contains $contracts $field "Interaction envelope field $field"
}
Assert-Contains $contracts 'IInteractionSink' 'Durable outbox sink boundary'
foreach ($method in 'ObserveButton', 'ObserveInput', 'ObserveDropdown', 'ObserveToggle', 'ObserveSwitch', 'ObserveRange', 'ObserveSegmentedControl', 'ObserveSteeringBar', 'ObserveProperty') {
    Assert-Contains $adapter "public IDisposable $method" "Control interaction adapter method $method"
}
if ($adapter -match 'HttpClient|HttpRequestMessage|Authorization|WebSocket|Grpc') {
    throw 'ControlInteractionAdapter must not own backend transport or credentials.'
}
Assert-Contains $ci 'Verify-InteractionContracts\.ps1' 'CI wiring for interaction contract verifier'

& dotnet run --project $testProjectPath --no-restore -p:Platform=x64
if ($LASTEXITCODE -ne 0) {
    throw "Interaction contract smoke test failed with exit code $LASTEXITCODE."
}

Write-Host 'Interaction contract audit passed: versioned envelope, outbox boundary, control adapters, mock-consumer smoke test, and CI wiring are present.'
