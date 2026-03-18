#!/usr/bin/env bash
# .devcontainer/setup.sh
# Post-create: creates a default Android API 36 AVD for MAUI debugging.
# Idempotent — safe to re-run (skips creation if AVD already exists).

set -euo pipefail

AVD_NAME="maui_api36"
API_LEVEL="36"

echo "==> Checking for AVD: ${AVD_NAME}"

if avdmanager list avd | grep -q "Name: ${AVD_NAME}"; then
    echo "    AVD '${AVD_NAME}' already exists — skipping creation."
else
    echo "==> Creating AVD '${AVD_NAME}' (API ${API_LEVEL}, x86_64)..."
    echo "no" | avdmanager create avd \
        --name "${AVD_NAME}" \
        --package "system-images;android-${API_LEVEL};google_apis;x86_64" \
        --device "pixel_6" \
        --force
    echo "==> AVD created."
fi

# Patch hardware config for headless KVM operation (no GPU passthrough in container)
HARDWARE_CONFIG="${HOME}/.android/avd/${AVD_NAME}.avd/config.ini"

if [ -f "${HARDWARE_CONFIG}" ]; then
    echo "==> Patching AVD hardware config..."

    # Software OpenGL renderer — no GPU passthrough inside the container
    if grep -q "^hw.gpu.mode=" "${HARDWARE_CONFIG}"; then
        sed -i 's/^hw.gpu.mode=.*/hw.gpu.mode=swiftshader_indirect/' "${HARDWARE_CONFIG}"
    else
        echo "hw.gpu.mode=swiftshader_indirect" >> "${HARDWARE_CONFIG}"
    fi

    # Disable snapshots to avoid stale state across container restarts
    if grep -q "^snapshot.present=" "${HARDWARE_CONFIG}"; then
        sed -i 's/^snapshot.present=.*/snapshot.present=no/' "${HARDWARE_CONFIG}"
    else
        echo "snapshot.present=no" >> "${HARDWARE_CONFIG}"
    fi

    echo "==> Hardware config patched."
fi

echo ""
echo "========================================================"
echo "  Dev container ready. To start the Android emulator:"
echo ""
echo "    emulator -avd ${AVD_NAME} -no-window -no-audio \\"
echo "             -gpu swiftshader_indirect &"
echo ""
echo "  Wait ~30s, then verify:"
echo "    adb wait-for-device && adb devices"
echo ""
echo "  Then press F5 in VS Code to build, deploy, and debug."
echo "========================================================"
