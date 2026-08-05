# Generates the Moayad AR release keystore LOCALLY (Windows). Never commits secrets.
$ErrorActionPreference = "Stop"
$ks = "moayad-release.keystore"
$alias = "moayad"

if (Test-Path $ks) { Write-Error "Keystore already exists at $ks — refusing to overwrite."; exit 1 }

$storePass = $env:MOAYAD_STORE_PASSWORD
if (-not $storePass) { $storePass = Read-Host "Keystore password" -AsSecureString | ConvertFrom-SecureString -AsPlainText }
$keyPass = $env:MOAYAD_KEY_PASSWORD
if (-not $keyPass) { $keyPass = $storePass }

keytool -genkeypair -v -keystore $ks -alias $alias -keyalg RSA -keysize 4096 -validity 10950 `
  -storepass $storePass -keypass $keyPass -dname "CN=Moayad AR, OU=Personal, O=Moayad, C=SA"

Remove-Variable storePass, keyPass -ErrorAction SilentlyContinue
Write-Host "Keystore created: $ks (git-ignored). Back it up privately — losing it blocks future updates."
