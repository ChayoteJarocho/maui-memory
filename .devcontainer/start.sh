#!/usr/bin/env bash
# .devcontainer/start.sh
# Runs on every container start.
# Starts: /dev/kvm fix → Xvfb → x11vnc → noVNC → Android emulator.
# All processes are idempotent — safe to re-run if already running.

set -euo pipefail

AVD_NAME="maui_api36"
DISPLAY_NUM=":1"
VNC_PORT=5900
NOVNC_PORT=6080

# ── 1. /dev/kvm permissions ───────────────────────────────────────────────────
sudo chmod a+rw /dev/kvm 2>/dev/null || true

# ── 2. Virtual framebuffer (Xvfb) ────────────────────────────────────────────
# Provides a headless display for the emulator window.
# Resolution 1280x800 is large enough to hold the emulator without wasting RAM.
if ! pgrep -x Xvfb > /dev/null; then
    echo "==> Starting Xvfb on display ${DISPLAY_NUM}..."
    Xvfb "${DISPLAY_NUM}" -screen 0 1280x800x24 &
fi

export DISPLAY="${DISPLAY_NUM}"

# Wait for Xvfb to create its socket before starting anything that needs the display.
echo "==> Waiting for Xvfb socket..."
until [ -S "/tmp/.X11-unix/X${DISPLAY_NUM#:}" ]; do
    sleep 0.5
done

# ── 3. VNC server (x11vnc) ───────────────────────────────────────────────────
# Shares the Xvfb display over VNC on localhost so noVNC can proxy it.
if ! pgrep -x x11vnc > /dev/null; then
    echo "==> Starting x11vnc on port ${VNC_PORT}..."
    (unset WAYLAND_DISPLAY; exec x11vnc -display "${DISPLAY_NUM}" -nopw -localhost \
           -rfbport "${VNC_PORT}" -forever -shared) \
           > /tmp/x11vnc.log 2>&1 &
    # Wait up to 10 s for x11vnc to bind the port (bash /dev/tcp — no nc needed).
    for _i in $(seq 1 20); do
        (echo >/dev/tcp/localhost/${VNC_PORT}) 2>/dev/null && break
        sleep 0.5
    done
    if ! (echo >/dev/tcp/localhost/${VNC_PORT}) 2>/dev/null; then
        echo "ERROR: x11vnc failed to start on port ${VNC_PORT}. Log:" >&2
        cat /tmp/x11vnc.log >&2
        exit 1
    fi
    echo "==> x11vnc ready."
fi

# ── 4. noVNC web proxy (websockify) ──────────────────────────────────────────
# Serves the browser-based VNC client at http://localhost:6080/vnc.html
if ! pgrep -f "websockify" > /dev/null; then
    echo "==> Starting noVNC on port ${NOVNC_PORT}..."
    websockify --web=/usr/share/novnc/ "${NOVNC_PORT}" "localhost:${VNC_PORT}" \
               --log-file /tmp/websockify.log &
fi

# ── 5. Android emulator ───────────────────────────────────────────────────────
if ! pgrep -x emulator > /dev/null; then
    echo "==> Starting Android emulator (${AVD_NAME})..."
    DISPLAY="${DISPLAY_NUM}" emulator -avd "${AVD_NAME}" \
        -no-audio -gpu swiftshader_indirect -no-snapshot &

    # Ensure ADB daemon is running, then wait for the emulator to register.
    # The MAUI extension queries `adb devices`; if the device isn't listed it tries
    # to start a second emulator instance, which fails.  Connecting here first lets
    # the extension find a ready device instead of attempting a duplicate launch.
    adb start-server
    echo "==> Waiting for emulator to appear in ADB (this takes ~30–60 s)..."
    until adb devices | grep -q "emulator-5554"; do
        sleep 3
    done
    echo "==> Emulator registered in ADB. Waiting for Android to fully boot..."
    until adb -s emulator-5554 shell getprop sys.boot_completed 2>/dev/null | grep -q "^1$"; do
        sleep 3
    done
    echo "==> Android fully booted and ready."
fi

echo ""
echo "========================================================"
echo "  Emulator starting. Open the emulator screen at:"
echo ""
echo "    http://localhost:${NOVNC_PORT}/vnc.html"
echo ""
echo "  Wait ~30s for the emulator to boot, then press F5"
echo "  in VS Code to build, deploy, and debug."
echo "========================================================"
