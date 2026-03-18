# syntax=docker/dockerfile:1
# Base: official MS devcontainers image — .NET 10 LTS on Ubuntu 24.04 (Noble)
# Already contains: dotnet 10 SDK, git, curl, sudo, "vscode" user (UID 1000)
FROM mcr.microsoft.com/devcontainers/dotnet:2-10.0-noble

# ── 1. System packages ────────────────────────────────────────────────────────
# openjdk-21       : Java 21 LTS — required by sdkmanager and Android build-tools
# unzip            : extract Android cmdline-tools zip
# libgl1           : emulator OpenGL software renderer (Swiftshader)
# libpulse0        : emulator audio
# libc6-i386/lib32z1 : 32-bit deps for some emulator binaries
# libncurses5      : required by some Android SDK binaries
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update -qq && apt-get install -y --no-install-recommends \
        openjdk-21-jdk-headless \
        unzip \
        libgl1 \
        libpulse0 \
        libglu1-mesa \
        libc6-i386 \
        lib32z1 \
        libstdc++6 \
        libncurses5 \
    && rm -rf /var/lib/apt/lists/*

# ── 2. Java environment ───────────────────────────────────────────────────────
ENV JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64
ENV PATH="${JAVA_HOME}/bin:${PATH}"

# ── 3. Android SDK directory layout ──────────────────────────────────────────
# sdkmanager REQUIRES cmdline-tools at: $ANDROID_HOME/cmdline-tools/latest/
# Any other path causes "Warning: Could not create settings" and sdkmanager exits.
ENV ANDROID_HOME=/opt/android-sdk
ENV ANDROID_SDK_ROOT=${ANDROID_HOME}
ENV PATH="${ANDROID_HOME}/cmdline-tools/latest/bin:${ANDROID_HOME}/platform-tools:${ANDROID_HOME}/emulator:${PATH}"

# ── 4. Android cmdline-tools ──────────────────────────────────────────────────
# Pin version for reproducibility. Check latest at:
# https://developer.android.com/studio#command-line-tools-only
ARG CMDLINE_TOOLS_VERSION=13114758
RUN mkdir -p ${ANDROID_HOME}/cmdline-tools \
    && curl -fsSL \
       "https://dl.google.com/android/repository/commandlinetools-linux-${CMDLINE_TOOLS_VERSION}_latest.zip" \
       -o /tmp/cmdline-tools.zip \
    && unzip -q /tmp/cmdline-tools.zip -d /tmp/cmdline-tools-extract \
    && mv /tmp/cmdline-tools-extract/cmdline-tools ${ANDROID_HOME}/cmdline-tools/latest \
    && rm -rf /tmp/cmdline-tools.zip /tmp/cmdline-tools-extract

# ── 5. Android SDK packages ───────────────────────────────────────────────────
# API 36 = Android 16 (stable since March 2025) — matches net10.0-android
# google_apis image (not google_apis_playstore) allows root adb for debugger
RUN yes | sdkmanager --licenses > /dev/null 2>&1 \
    && sdkmanager --install \
        "platforms;android-36" \
        "build-tools;36.0.0" \
        "platform-tools" \
        "emulator" \
        "system-images;android-36;google_apis;x86_64"

# Fix permissions so vscode user can read the SDK
RUN chmod -R o+rX ${ANDROID_HOME}

# ── 6. MAUI Android workload ──────────────────────────────────────────────────
# Installed as root into /usr/share/dotnet — baked into the image layer.
# --skip-sign-check avoids NuGet signature validation failures in Docker builds.
RUN dotnet workload install maui-android --skip-sign-check
