#!/bin/sh
set -eu

# Use this project's Unity SDK so Editor deployment and terminal ADB agree.
arise_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
arise_version="$(sed -n 's/^m_EditorVersion: //p' "$arise_root/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"
arise_sdk="${UNITY_ANDROID_SDK:-/Applications/Unity/Hub/Editor/$arise_version/PlaybackEngines/AndroidPlayer/SDK}"
arise_adb="$arise_sdk/platform-tools/adb"

if [ ! -x "$arise_adb" ]; then
    echo "Unity Android SDK not found: $arise_sdk" >&2
    echo "Install Android Build Support, SDK & NDK Tools, and OpenJDK in Unity Hub." >&2
    echo "For another SDK location, set UNITY_ANDROID_SDK." >&2
    exit 1
fi

exec "$arise_adb" "$@"
