#requires -Version 5.1
<#
.SYNOPSIS
  Stages a local NuGet feed from ../../artifacts and runs both native-loading scenarios in Docker.

.DESCRIPTION
  Scenario 1 (bundled): managed NuGet + native NuGet  -> engine loaded from the app's own dir.
  Scenario 2 (system) : managed NuGet only + .deb      -> engine loaded from the system.

  Build logs and run transcripts are written under ./logs.
#>
[CmdletBinding()]
param(
    [ValidateSet('both', 'bundled', 'system')]
    [string]$Scenario = 'both'
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$root = (Resolve-Path (Join-Path $here '..\..')).Path        # tools/csharp_api
$artifacts = Join-Path $root 'artifacts'
$version = (Get-Content (Join-Path $root 'version.txt') -Raw).Trim()
Write-Host "LadybugDB version: $version"

# 1) Stage the local feed (only the two packages the scenarios need).
$feed = Join-Path $here 'feed'
New-Item -ItemType Directory -Force -Path $feed | Out-Null
foreach ($pkg in @("LadybugDB.$version.nupkg", "LadybugDB.Native.linux-x64.$version.nupkg")) {
    $src = Join-Path $artifacts $pkg
    if (-not (Test-Path $src)) { throw "Missing artifact: $src (run the Pack target first)" }
    Copy-Item $src $feed -Force
    Write-Host "staged $pkg"
}

$logs = Join-Path $here 'logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null

function Invoke-Scenario([string]$name, [string]$dockerfile, [string]$tag) {
    # docker (and dotnet) stream progress to stderr. Merging it via PowerShell's `2>&1` wraps each line
    # in an ErrorRecord (noisy red output); instead let cmd merge the streams at the OS level so we get
    # plain text for both the console and the log file.
    $df = Join-Path $here $dockerfile

    Write-Host "`n=== building $name image ($tag) ==="
    cmd /c "docker build -f `"$df`" --build-arg LADYBUG_VERSION=$version -t $tag `"$here`" 2>&1" |
        Tee-Object (Join-Path $logs "$name.build.log")
    if ($LASTEXITCODE -ne 0) { throw "docker build failed for $name" }

    Write-Host "`n=== running $name scenario ==="
    cmd /c "docker run --rm $tag 2>&1" | Tee-Object (Join-Path $logs "$name.run.log")
    Write-Host "$name exit code: $LASTEXITCODE"
}

if ($Scenario -in @('both', 'bundled')) {
    Invoke-Scenario 'bundled' 'Dockerfile.bundled' 'lbug-example-bundled'
}
if ($Scenario -in @('both', 'system')) {
    Invoke-Scenario 'system' 'Dockerfile.system' 'lbug-example-system'
}
