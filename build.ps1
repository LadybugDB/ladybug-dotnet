#!/usr/bin/env pwsh
# Bootstrap for the Cake Frosting packaging pipeline. Forwards all arguments to the build project, e.g.
#   .\build.ps1 --target Test
#   .\build.ps1 --target Pack # version + engine release come from version.txt; override with --package-version / --engine-version
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

dotnet run --project "$PSScriptRoot/cake/LadybugDB.Build.csproj" -- $Arguments
exit $LASTEXITCODE
