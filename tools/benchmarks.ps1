param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$SkipBuild,

    [switch]$SkipNative,

    [switch]$List,

    [string]$Filter = "*",

    [switch]$DryRun,

    [string[]]$BenchmarkArgs
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ArtifactsPath = Join-Path $RepoRoot "artifacts"
$BuildScriptPath = Join-Path $PSScriptRoot "build.ps1"
$BenchmarkProjectPath = Join-Path $RepoRoot "src\NumericsSharp.Benchmarks\NumericsSharp.Benchmarks.csproj"
$AdditionalBenchmarkArgs = @(@($BenchmarkArgs) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE`: $FilePath $($Arguments -join ' ')"
    }
}

if (-not $SkipBuild) {
    $BuildArguments = @("-Configuration", $Configuration)
    if ($SkipNative) {
        $BuildArguments += "-SkipNative"
    }

    $PowerShellArguments = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $BuildScriptPath) + $BuildArguments
    Invoke-CheckedCommand -FilePath powershell -Arguments $PowerShellArguments
}

$BenchmarkArtifactsPath = Join-Path $ArtifactsPath "benchmarks"
$RunArguments = @(
    "run",
    "--configuration",
    $Configuration,
    "--project",
    $BenchmarkProjectPath,
    "--",
    "--artifacts",
    $BenchmarkArtifactsPath
)

if ($List) {
    $RunArguments += @("--list", "flat")
}

if ($AdditionalBenchmarkArgs.Length -gt 0) {
    $RunArguments += $AdditionalBenchmarkArgs
}
elseif (-not $List) {
    $RunArguments += @("--filter", $Filter)
}

if ($DryRun) {
    $RunArguments += @("--job", "Dry")
}

Invoke-CheckedCommand -FilePath dotnet -Arguments $RunArguments
