#!/usr/bin/env bash
# Scenario 2 runner (executed inside the container as ENTRYPOINT).
# App = managed NuGet ONLY. The native engine is provided system-wide by a Debian package.
#
# Three phases:
#   A. show the app ships no native of its own
#   B. NEGATIVE CONTROL: run before installing the .deb -> must FAIL (proves nothing is bundled)
#   C. install the .deb, then run -> must SUCCEED and load from the SYSTEM path
set -uo pipefail

APP=/app/publish
DEB="$(ls /tmp/liblbug_*.deb | head -n1)"

echo "############################################################"
echo "# Scenario 2: SYSTEM native (managed NuGet only + .deb)"
echo "############################################################"

echo "--- distro ---"
. /etc/os-release && echo "$PRETTY_NAME"

echo "=== Phase A: what does the app ship? (expect NO liblbug) ==="
if ls -l "$APP" | grep -i lbug; then
  echo "ERROR: app unexpectedly ships a native library"
  exit 1
else
  echo "(no liblbug in app dir - good, this is a managed-only deployment)"
fi

echo "=== Phase B: NEGATIVE CONTROL - run with NO system engine (expect failure) ==="
if "$APP/ConsumerApp"; then
  echo "ERROR: app ran without any native engine present - unexpected!"
  exit 1
else
  echo "(expected) app failed to load the native engine before the .deb was installed"
fi

echo "=== Phase C: install the Debian package, then run again ==="
echo "--- dpkg -i $DEB ---"
dpkg -i "$DEB"
echo "--- ld.so cache after install ---"
ldconfig -p | grep -i lbug || echo "(liblbug not listed by soname in ld cache)"
echo "--- installed file ---"
ls -l /usr/lib/x86_64-linux-gnu/liblbug.so
echo "--- run the app (expect SUCCESS, loaded from SYSTEM) ---"
"$APP/ConsumerApp"
rc=$?
echo "--- exit code: $rc ---"
exit $rc
