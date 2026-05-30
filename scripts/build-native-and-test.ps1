#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build the native Ladybug C-API shared library (lbug_shared) on Windows, drop it into the C#
    binding's runtime folder, and run the xUnit suite end-to-end.

.DESCRIPTION
    Sets up the MSVC x64 toolchain (via vcvars64.bat) and ensures CMake/Ninja are on PATH, then:
      1. configures build/<config> with Ninja (shared-lib only) if not already configured,
      2. builds the lbug_shared target,
      3. copies lbug_shared.dll into tools/csharp_api/lib/runtimes/win-x64/native,
      4. runs the C# tests (unless -SkipTests).

    The dotnet/C# build itself does NOT need MSVC; only the native CMake build does.

.PARAMETER Configuration
    Native CMake build type: Release (default), RelWithDebInfo, or Debug.

.PARAMETER Reconfigure
    Delete and regenerate the CMake build directory before building.

.PARAMETER SkipTests
    Build and stage the native lib but do not run dotnet test.
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'RelWithDebInfo', 'Debug')]
    [string]$Configuration = 'Release',
    [switch]$Reconfigure,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$csharpDir = Split-Path $PSScriptRoot -Parent
$repoRoot = (Resolve-Path (Join-Path $csharpDir '..\..')).Path
$buildDir = Join-Path $repoRoot ("build/" + $Configuration.ToLowerInvariant())
$nativeDest = Join-Path $csharpDir 'lib/runtimes/win-x64/native'
$rid = 'win-x64'

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }

# --- 1. MSVC environment -----------------------------------------------------------------------
if (-not (Get-Command cl -ErrorAction SilentlyContinue)) {
    Write-Step 'Importing MSVC x64 environment (vcvars64.bat)'
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    $vcvars = $null
    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -latest -prerelease -products * -property installationPath | Select-Object -First 1
        if ($vsPath) {
            $candidate = Join-Path $vsPath 'VC\Auxiliary\Build\vcvars64.bat'
            if (Test-Path $candidate) { $vcvars = $candidate }
        }
    }
    if (-not $vcvars) { throw "Could not locate vcvars64.bat. Install the 'Desktop development with C++' workload." }

    cmd /c "`"$vcvars`" && set" | ForEach-Object {
        if ($_ -match '^([^=]+)=(.*)$') { Set-Item -Path ("Env:\" + $matches[1]) -Value $matches[2] }
    }
}

# --- 2. CMake / Ninja on PATH ------------------------------------------------------------------
if (-not (Get-Command cmake -ErrorAction SilentlyContinue) -or -not (Get-Command ninja -ErrorAction SilentlyContinue)) {
    Write-Step 'Adding pip user Scripts dir to PATH for cmake/ninja'
    $scripts = & python -c "import sysconfig; print(sysconfig.get_path('scripts', 'nt_user'))" 2>$null
    if ($scripts -and (Test-Path $scripts)) { $env:Path = "$scripts;$env:Path" }
    if (-not (Get-Command cmake -ErrorAction SilentlyContinue)) {
        throw "cmake not found. Run: python -m pip install --user cmake ninja"
    }
}

Write-Step "Toolchain: cl=$((Get-Command cl).Source); cmake=$((cmake --version | Select-Object -First 1)); ninja=$(ninja --version)"

# --- 3. Configure ------------------------------------------------------------------------------
if ($Reconfigure -and (Test-Path $buildDir)) {
    Write-Step "Removing $buildDir"
    Remove-Item -Recurse -Force $buildDir
}

if (-not (Test-Path (Join-Path $buildDir 'build.ninja'))) {
    Write-Step "Configuring $buildDir ($Configuration, shared-lib only)"
    $cmakeArgs = @(
        '-B', $buildDir,
        '-G', 'Ninja',
        "-DCMAKE_BUILD_TYPE=$Configuration",
        '-DBUILD_SHELL=OFF',
        '-DBUILD_SINGLE_FILE_HEADER=OFF',
        '-DBUILD_STATIC_LBUG=OFF',
        '-DBUILD_TESTS=OFF',
        '-DCMAKE_POLICY_VERSION_MINIMUM=3.5',   # CMake 4.x floor for vendored third_party deps
        $repoRoot
    )
    & cmake @cmakeArgs
} else {
    Write-Step "Reusing existing configuration at $buildDir (pass -Reconfigure to regenerate)"
}

# --- 4. Build the shared C-API library ---------------------------------------------------------
Write-Step 'Building target lbug_shared'
& cmake --build $buildDir --target lbug_shared

$dll = Join-Path $buildDir 'src/lbug_shared.dll'
if (-not (Test-Path $dll)) { throw "Expected output not found: $dll" }

# --- 5. Stage into the C# binding --------------------------------------------------------------
Write-Step "Staging lbug_shared.dll into lib/runtimes/$rid/native"
New-Item -ItemType Directory -Force -Path $nativeDest | Out-Null
Copy-Item $dll -Destination $nativeDest -Force
$mb = [math]::Round((Get-Item $dll).Length / 1MB, 1)
Write-Host "    -> $nativeDest\lbug_shared.dll ($mb MB)" -ForegroundColor Green

# --- 6. Run the C# suite -----------------------------------------------------------------------
if (-not $SkipTests) {
    Write-Step 'Running dotnet test'
    & dotnet test (Join-Path $csharpDir 'test/LadybugDB.Tests/LadybugDB.Tests.csproj') -v minimal
}

Write-Step 'Done.'
