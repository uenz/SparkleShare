#!/bin/sh
# Expect path to app bundle argument
export bundle=$1
export projectFolder=$(dirname $0)
${projectFolder}/checkGit.sh


#!/bin/bash
set -e

# Parameter 1 = Pfad + AssemblyName
INPUT_PATH="$1"

# AssemblyName extrahieren (Dateiname ohne Pfad)
APP_NAME=$(basename "$INPUT_PATH")

# .app Zielordner
APP_DIR="${INPUT_PATH}.app"

echo "📦 Erstelle macOS App Bundle für: $APP_NAME"
echo "📁 Input: $INPUT_PATH"
echo "📁 AppDir: $APP_DIR"

# Struktur anlegen
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

# Build-Output kopieren
echo "➡️ Kopiere Build-Output in .app..."
cp -R "$(dirname "$INPUT_PATH")"/* "$APP_DIR/Contents/MacOS/"

# Info.plist erzeugen
cat > "$APP_DIR/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
 "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>${APP_NAME}</string>
    <key>CFBundleExecutable</key><string>${APP_NAME}</string>
    <key>CFBundleIdentifier</key><string>com.sparkleshare.${APP_NAME}</string>
    <key>CFBundleVersion</key><string>1.0</string>
    <key>CFBundlePackageType</key><string>APPL</string>
</dict>
</plist>
EOF

# Icon kopieren (optional)
if [ -f "$(dirname "$INPUT_PATH")/icon.icns" ]; then
    echo "🎨 Icon gefunden — kopiere icon.icns"
    cp "$(dirname "$INPUT_PATH")/icon.icns" "$APP_DIR/Contents/Resources/"
fi

# Binary ausführbar machen
chmod +x "$APP_DIR/Contents/MacOS/${APP_NAME}"

echo "✅ Fertig! App erstellt:"
echo "$APP_DIR"
