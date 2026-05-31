#!/usr/bin/env bash
# Scenario 1 runner (executed inside the container as ENTRYPOINT).
# App = managed NuGet + native NuGet. Expectation: the engine loads from the app's OWN directory,
# and there is NO system-wide liblbug present.
set -uo pipefail

APP=/app/publish
echo "############################################################"
echo "# Scenario 1: BUNDLED native (managed NuGet + native NuGet)"
echo "############################################################"

echo "--- distro ---"
. /etc/os-release && echo "$PRETTY_NAME"

echo "--- app deployment dir (expect liblbug.so present) ---"
ls -l "$APP" | grep -i lbug || echo "(no liblbug in app dir - UNEXPECTED)"

echo "--- system state (expect NO system liblbug) ---"
ldconfig -p | grep -i lbug || echo "(no liblbug in ld.so cache - good)"

echo "--- run the app ---"
"$APP/ConsumerApp"
rc=$?
echo "--- exit code: $rc ---"
exit $rc
