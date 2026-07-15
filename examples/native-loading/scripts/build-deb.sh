#!/usr/bin/env bash
# Builds a minimal Debian package that installs the Ladybug native engine system-wide, exactly the
# way a distro-provided native package would (file under /usr/lib/<triplet> + ldconfig in postinst).
#
# The shipped liblbug.so is the SAME binary that the NuGet native package carries: we extract it from
# LadybugDB.Native.linux-x64.*.nupkg so the bundled-vs-system comparison is apples-to-apples.
#
# Usage: build-deb.sh <feed-dir> <output-dir> [version]
set -euo pipefail

FEED="${1:?feed dir required}"
OUT="${2:?output dir required}"
VER="${3:-0.18.2}"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

nupkg="$(ls "$FEED"/LadybugDB.Native.linux-x64.*.nupkg | head -n1)"
echo "[build-deb] extracting liblbug.so from: $nupkg"
unzip -o "$nupkg" 'runtimes/linux-x64/native/*' -d "$work/x" >/dev/null
so="$(find "$work/x" -name 'liblbug.so' | head -n1)"
if [[ -z "$so" ]]; then
  echo "[build-deb] ERROR: liblbug.so not found inside the native nupkg" >&2
  exit 1
fi

echo "[build-deb] SONAME of the staged engine:"
readelf -d "$so" | grep -i soname || echo "  (no DT_SONAME)"

root="$work/deb"
mkdir -p "$root/DEBIAN" "$root/usr/lib/x86_64-linux-gnu"
cp "$so" "$root/usr/lib/x86_64-linux-gnu/liblbug.so"

cat > "$root/DEBIAN/control" <<EOF
Package: liblbug
Version: $VER
Section: libs
Priority: optional
Architecture: amd64
Maintainer: LadybugDB example <noreply@ladybugdb.local>
Description: Ladybug native engine shared library (example)
 System-installed native engine for the LadybugDB .NET binding. Installs liblbug.so into the
 standard multiarch library directory and refreshes the dynamic linker cache.
EOF

cat > "$root/DEBIAN/postinst" <<'EOF'
#!/bin/sh
set -e
ldconfig
EOF
chmod 0755 "$root/DEBIAN/postinst"

deb="$OUT/liblbug_${VER}_amd64.deb"
dpkg-deb --build --root-owner-group "$root" "$deb"
echo "[build-deb] built: $deb"
