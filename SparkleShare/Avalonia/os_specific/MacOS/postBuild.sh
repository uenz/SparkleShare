#!/bin/bash
set -euo pipefail

# Expect path to app bundle argument
export bundle=${1:-}
export runtimeidentifier=${2:-}
export projectFolder=$(cd "$(dirname "$0")" && pwd)

"${projectFolder}/checkGit.sh"

# Parameter 1 = Pfad + AssemblyName
INPUT_PATH="$1"

# AssemblyName extrahieren (Dateiname ohne Pfad)
APP_NAME=$(basename "$INPUT_PATH")
BUNDLE_NAME=${APP_NAME%%.*}
APP_PATH=$(dirname "$INPUT_PATH")
# .app Zielordner
APP_DIR="${projectFolder}/${BUNDLE_NAME}.app" 

echo "📦 Erstelle macOS App Bundle für App: $APP_NAME Bundle: $BUNDLE_NAME Path: $APP_PATH"
echo "📁 Input: $INPUT_PATH"
echo "📁 AppDir: $APP_DIR"

# Struktur anlegen
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS"
# copy git executables to Resources/git and afterwards to teh .app
rm -rf "${projectFolder}/Resources/git"
mkdir -p "${projectFolder}/Resources/git"
tar -x -f "${projectFolder}/git.tar.gz" --directory "${projectFolder}/Resources/git"
cp -R "${projectFolder}/Resources" "$APP_DIR/Contents"
# Build-Output kopieren
echo "➡️ Kopiere Build-Output in .app..."
mkdir -p "$APP_DIR/Contents/MacOS"
cp -R "$(dirname "$INPUT_PATH")"/* "$APP_DIR/Contents/MacOS/"

# Copy the macOS native libraries into the bundle root so Avalonia can resolve them.
if [ -f "$(dirname "$INPUT_PATH")/runtimes/osx/native/libSkiaSharp.dylib" ]; then
  cp "$(dirname "$INPUT_PATH")/runtimes/osx/native/libSkiaSharp.dylib" "$APP_DIR/Contents/MacOS/libSkiaSharp.dylib"
fi
if [ -f "$(dirname "$INPUT_PATH")/runtimes/osx/native/libHarfBuzzSharp.dylib" ]; then
  cp "$(dirname "$INPUT_PATH")/runtimes/osx/native/libHarfBuzzSharp.dylib" "$APP_DIR/Contents/MacOS/libHarfBuzzSharp.dylib"
fi
if [ -f "$(dirname "$INPUT_PATH")/runtimes/osx/native/libAvaloniaNative.dylib" ]; then
  cp "$(dirname "$INPUT_PATH")/runtimes/osx/native/libAvaloniaNative.dylib" "$APP_DIR/Contents/MacOS/libAvaloniaNative.dylib"
fi

# Only keep runtime assets relevant for macOS; this avoids shipping incompatible Windows/Linux runtime folders.
if [ -d "$APP_DIR/Contents/MacOS/runtimes" ]; then
  find "$APP_DIR/Contents/MacOS/runtimes" -mindepth 1 -maxdepth 1 -type d ! -name 'osx-*' -exec rm -rf {} +
fi

chmod +x "$APP_DIR/Contents/MacOS/${APP_NAME}"
"${projectFolder}/checkGit.sh" "$runtimeidentifier"
cp "${projectFolder}/Info.plist" "$APP_DIR/Contents"

# Clear quarantine attributes and remove any prior signature data so the bundle stays unsigned.
if command -v xattr >/dev/null 2>&1; then
  find "$APP_DIR" -exec xattr -c {} + 2>/dev/null || true
fi

find "$APP_DIR/Contents" -type d -name "_CodeSignature" -prune -exec rm -rf {} + 2>/dev/null || true

echo "ℹ️ App bundle is created unsigned; no code signature is applied."

# Icon kopieren (optional)
# if [ -f "$(dirname "$INPUT_PATH")/icon.icns" ]; then
#     echo "🎨 Icon gefunden — kopiere icon.icns"
#     cp "$(dirname "$INPUT_PATH")/icon.icns" "$APP_DIR/Contents/Resources/"
# fi
# Binary ausführbar machen
chmod +x "${APP_DIR}/Contents/MacOS/${APP_NAME}"

if command -v create-dmg >/dev/null 2>&1; then
  rm -f "${projectFolder}/SparkleShare-Installer.dmg"
  create-dmg --volname "${BUNDLE_NAME} Installer" \
    --volicon "${projectFolder}/Resources/sparkleshare-app.icns" \
    --background "${projectFolder}/../../../Common/Images/about.png" \
    --window-pos 200 120 \
    --window-size 680 50 \
    --icon-size 100 \
    --icon "${BUNDLE_NAME}.app" 200 300 \
    --hide-extension "${BUNDLE_NAME}.app" \
    --app-drop-link 400 300 \
##    --overwrite \
    "${projectFolder}/SparkleShare-Installer.dmg" \
    "${APP_DIR}/"
else
  echo "⚠️ create-dmg not available; skipping DMG creation."
fi

# hdiutil create -volname "${APP_NAME}" -srcfolder "${APP_DIR}" -ov -format UDZO "${APP_NAME}.dmg"
echo "✅ Fertig! App erstellt:"
echo "$APP_DIR"
