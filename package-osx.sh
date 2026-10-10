#!/bin/bash

Arch="$1"
OutputPath="$2"
Version="$3"

FileName="RITMV-${Arch}.zip"
wget -nv -O $FileName "https://github.com/RITM-Fre/RITM-V-core-bin/raw/refs/heads/master/$FileName"
7z x $FileName
cp -rf RITMV-${Arch}/* $OutputPath

PackagePath="RITMV-Package-${Arch}"
mkdir -p "$PackagePath/RITMV.app/Contents/Resources"
cp -rf "$OutputPath" "$PackagePath/RITMV.app/Contents/MacOS"
cp -f "$PackagePath/RITMV.app/Contents/MacOS/RITMV.icns" "$PackagePath/RITMV.app/Contents/Resources/AppIcon.icns"
echo "When this file exists, app will not store configs under this folder" > "$PackagePath/RITMV.app/Contents/MacOS/NotStoreConfigHere.txt"
chmod +x "$PackagePath/RITMV.app/Contents/MacOS/RITMV"

cat >"$PackagePath/RITMV.app/Contents/Info.plist" <<-EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleLocalizations</key>
  <array>
    <string>zh-Hans</string>
    <string>zh-Hant</string>
    <string>en</string>
    <string>fa</string>
    <string>fr</string>
    <string>ru</string>
    <string>hu</string>
  </array>
  <key>CFBundleDisplayName</key>
  <string>RITMV</string>
  <key>CFBundleExecutable</key>
  <string>RITMV</string>
  <key>CFBundleIconFile</key>
  <string>AppIcon</string>
  <key>CFBundleIconName</key>
  <string>AppIcon</string>
  <key>CFBundleIdentifier</key>
  <string>RITM-Fre.RITMV</string>
  <key>CFBundleName</key>
  <string>RITMV</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>${Version}</string>
  <key>CSResourcesFileMapped</key>
  <true/>
  <key>NSHighResolutionCapable</key>
  <true/>
  <key>LSMinimumSystemVersion</key>
  <string>13.6</string>
</dict>
</plist>
EOF

create-dmg \
    --volname "RITMV Installer" \
    --window-size 700 420 \
    --icon-size 100 \
    --icon "RITMV.app" 160 185 \
    --hide-extension "RITMV.app" \
    --app-drop-link 500 185 \
    "RITMV-${Arch}.dmg" \
    "$PackagePath/RITMV.app"
