param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$SkipNative,

    [switch]$NoClean
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ArtifactsBinPath = Join-Path $RepoRoot "artifacts\bin"
$NativeProjectPath = Join-Path $RepoRoot "src\NumericsSharp.Mkl.Native"
$NativeBuildPreset = "win-x64-$($Configuration.ToLowerInvariant())"
$ManagedProjects = @(
    "NumericsSharp.Core",
    "NumericsSharp.Solvers",
    "NumericsSharp.Mkl"
)

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE`: $FilePath $($Arguments -join ' ')"
    }
}

if (-not $NoClean -and (Test-Path $ArtifactsBinPath)) {
    Remove-Item -LiteralPath $ArtifactsBinPath -Recurse -Force
}

New-Item -ItemType Directory -Path $ArtifactsBinPath -Force | Out-Null

if (-not $SkipNative) {
    Push-Location $NativeProjectPath
    try {
        Invoke-CheckedCommand cmake --preset win-x64
        Invoke-CheckedCommand cmake --build --preset $NativeBuildPreset
    }
    finally {
        Pop-Location
    }
}

foreach ($ProjectName in $ManagedProjects) {
    $ProjectPath = Join-Path $RepoRoot "src\$ProjectName\$ProjectName.csproj"
    Invoke-CheckedCommand dotnet build $ProjectPath --configuration $Configuration

    $OutputPath = Join-Path $RepoRoot "src\$ProjectName\bin\$Configuration\net8.0"

    if (-not (Test-Path $OutputPath)) {
        throw "Build output directory was not found: $OutputPath"
    }

    Get-ChildItem -LiteralPath $OutputPath | Copy-Item -Destination $ArtifactsBinPath -Recurse -Force
}

if (-not $SkipNative) {
    $NativeOutputPath = Join-Path $NativeProjectPath "build\win-x64\$Configuration"
    $NativeFiles = @(
        "NumericsSharp.Mkl.Native.dll",
        "NumericsSharp.Mkl.Native.pdb"
    )

    foreach ($FileName in $NativeFiles) {
        $FilePath = Join-Path $NativeOutputPath $FileName
        if (Test-Path $FilePath) {
            Copy-Item -LiteralPath $FilePath -Destination $ArtifactsBinPath -Force
        }
    }

    if (-not (Test-Path (Join-Path $ArtifactsBinPath "NumericsSharp.Mkl.Native.dll"))) {
        throw "Native build completed, but NumericsSharp.Mkl.Native.dll was not found in $NativeOutputPath"
    }
}

Write-Host "Build artifacts copied to $ArtifactsBinPath"
