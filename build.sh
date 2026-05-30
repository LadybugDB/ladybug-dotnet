#!/usr/bin/env bash
# Bootstrap for the Cake Frosting packaging pipeline. Forwards all arguments to the build project, e.g.
#   ./build.sh --target Test
#   ./build.sh --target Pack # version + engine release come from version.txt; override with --package-version / --engine-version
set -euo pipefail

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
dotnet run --project "$script_dir/cake/LadybugDB.Build.csproj" -- "$@"
