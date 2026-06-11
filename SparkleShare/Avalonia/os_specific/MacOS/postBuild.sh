#!/bin/sh
# Expect path to app bundle argument
export bundle=$1
export runtimeidentifier=$2
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

echo "📦 Erstelle macOS App Bundle für: $APP_NAME : $runtimeidentifier"
echo "📁 Input: $INPUT_PATH"
echo "📁 AppDir: $APP_DIR"

# Struktur anlegen
mkdir -p "$APP_DIR/Contents/MacOS"
# copy git executables to Resources/git and afterwards to teh .app
rm -rf "${projectFolder}/Resources/git"
mkdir -p "${projectFolder}/Resources/git"
tar -x -f "${projectFolder}/git.tar.gz" --directory "${projectFolder}/Resources/git"
cp -R "${projectFolder}/Resources" "$APP_DIR/Contents"
# Build-Output kopieren
echo "➡️ Kopiere Build-Output in .app..."
cp -R "$(dirname "$INPUT_PATH")"/* "$APP_DIR/Contents/MacOS/"
${projectFolder}/checkGit.sh $runtimeidentifier
cp "${projectFolder}/Info.plist" "$APP_DIR/Contents"
# Icon kopieren (optional)
# if [ -f "$(dirname "$INPUT_PATH")/icon.icns" ]; then
#     echo "🎨 Icon gefunden — kopiere icon.icns"
#     cp "$(dirname "$INPUT_PATH")/icon.icns" "$APP_DIR/Contents/Resources/"
# fi
# Binary ausführbar machen
chmod +x "$APP_DIR/Contents/MacOS/${APP_NAME}"
hdiutil create -volname "${APP_NAME}" -srcfolder "${APP_DIR}" -ov -format UDZO "${APP_NAME}.dmg"
echo "✅ Fertig! App erstellt:"
echo "$APP_DIR"
