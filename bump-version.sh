#!/usr/bin/env bash
# Bump the wire-compatible game version across all three places it lives:
#   - root build.gradle.kts (allprojects.version)
#   - common/src/.../Version.java (server / shared)
#   - unity-client/Assets/Scripts/Network/Version.cs (client)
#
# A Gradle :common:checkVersionSync task verifies the three are aligned on
# every build, so forgetting to run this just fails the build with a clear
# message — it does not silently ship a desync.
#
# Usage: ./bump-version.sh 0.2.0
set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <X.Y.Z>" >&2
    exit 1
fi

NEW="$1"
if [[ ! "$NEW" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Version must be semver MAJOR.MINOR.PATCH (got: $NEW)" >&2
    exit 1
fi

cd "$(dirname "$0")"

# -i.bak keeps this script portable between GNU sed and BSD sed (macOS).
sed -i.bak -E "s/^(version = )\"[^\"]+\"/\1\"$NEW\"/" build.gradle.kts
sed -i.bak -E "s/(CURRENT = )\"[^\"]+\"/\1\"$NEW\"/" common/src/main/java/com/astrion/common/Version.java
sed -i.bak -E "s/(Current = )\"[^\"]+\"/\1\"$NEW\"/" unity-client/Assets/Scripts/Network/Version.cs

rm -f build.gradle.kts.bak \
      common/src/main/java/com/astrion/common/Version.java.bak \
      unity-client/Assets/Scripts/Network/Version.cs.bak

echo "Bumped to $NEW"
echo "Changed files:"
git diff --stat -- build.gradle.kts \
    common/src/main/java/com/astrion/common/Version.java \
    unity-client/Assets/Scripts/Network/Version.cs 2>/dev/null || true
