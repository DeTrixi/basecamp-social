# Basecamp Social — Mobile App

React Native (Expo) app for iOS and Android.

## Prerequisites

- [Node.js 22+](https://nodejs.org/)
- [Expo CLI](https://docs.expo.dev/get-started/installation/) (bundled with `npx`)
- **iOS Simulator**: Xcode (macOS only)
- **Android Emulator**: Android Studio
- **Physical device**: [Expo Go](https://expo.dev/go) app from App Store / Google Play

## Getting Started

```bash
# Install dependencies
npm install

# Start the development server
npx expo start
```

## Running the App

### Option 1: iOS Simulator (macOS only)

Requires Xcode with iOS Simulator installed.

```bash
# Boot iPhone 16 simulator first
xcrun simctl boot "iPhone 16"
open -a Simulator

# Then start Expo — it will use the already-running simulator
npx expo start --ios
```

> **Tip:** Press `Shift + i` after starting the dev server to choose a different simulator device.
>
> To list all available simulators:
> ```bash
> xcrun simctl list devices available | grep -i iphone
> ```

### Option 2: Physical iPhone or Android (Expo Go)

1. Install **Expo Go** on your device:
   - [iOS — App Store](https://apps.apple.com/app/expo-go/id982107779)
   - [Android — Google Play](https://play.google.com/store/apps/details?id=host.exp.exponent)
2. Start the dev server:
   ```bash
   npx expo start
   ```
3. Scan the **QR code** shown in the terminal:
   - **iPhone**: Use the Camera app — it will detect the Expo link
   - **Android**: Use the Expo Go app's built-in scanner

> Your phone and computer must be on the **same WiFi network**.

### Option 3: Web Browser

```bash
npx expo start --web

# Or press 'w' after starting the dev server
```

### Option 4: Android Emulator

Requires Android Studio with an emulator configured.

```bash
npx expo start --android

# Or press 'a' after starting the dev server
```

## Installing on a Physical Device (Permanent)

Expo Go runs your app inside a container. To install a **standalone app** on your device:

### Development Build (local)

```bash
# Install dev client
npx expo install expo-dev-client

# Build and install on a connected iPhone via USB
npx expo run:ios --device
```

- **Free Apple ID**: App expires after 7 days
- **Apple Developer account** ($99/yr): App lasts 1 year

### Development Build (cloud via EAS)

```bash
# One-time setup
npm install -g eas-cli
eas login
eas build:configure

# Build in the cloud
eas build --platform ios --profile development
```

Gives you a QR code / link to install the `.ipa` on registered devices.

## Useful Commands

| Command | Description |
|---|---|
| `npx expo start` | Start dev server |
| `npx expo start --clear` | Start with cleared Metro cache |
| `npx expo start --ios` | Start and open iOS Simulator |
| `npx expo start --android` | Start and open Android Emulator |
| `npx expo install <package>` | Install an Expo-compatible package |
| `npx expo doctor` | Check for common issues |

## Tech Stack

| Technology | Purpose |
|---|---|
| Expo SDK 54 | App framework |
| React Navigation 7 | Navigation |
| Zustand 5 | State management |
| React Native Paper 5 | UI components (Material Design 3) |
| SignalR | Real-time messaging |
| expo-sqlite | Local encrypted database |
| expo-secure-store | Keychain / secure storage |
| expo-crypto | Cryptographic primitives |

## Troubleshooting

### `simctl` error when starting iOS Simulator

Make sure `xcode-select` points to Xcode (not just Command Line Tools):

```bash
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
# or if you have Xcode-beta:
sudo xcode-select -s /Applications/Xcode-beta.app/Contents/Developer
```

### Metro bundler cache issues

```bash
npx expo start --clear
```

### Port 8081 already in use

```bash
# Find and kill the process using port 8081
lsof -i :8081
kill -9 <PID>
```
