#!/usr/bin/env bash
# Generates the Moayad AR release keystore LOCALLY. Never commits secrets.
# Reads MOAYAD_STORE_PASSWORD / MOAYAD_KEY_PASSWORD from env if set; otherwise prompts interactively.
set -euo pipefail

KS="moayad-release.keystore"
ALIAS="moayad"

if [[ -f "$KS" ]]; then
  echo "Keystore already exists at $KS — refusing to overwrite." >&2
  exit 1
fi

if [[ -z "${MOAYAD_STORE_PASSWORD:-}" ]]; then
  read -rs -p "Keystore password: " MOAYAD_STORE_PASSWORD; echo
fi
if [[ -z "${MOAYAD_KEY_PASSWORD:-}" ]]; then
  read -rs -p "Key password (Enter = same as keystore): " MOAYAD_KEY_PASSWORD; echo
  MOAYAD_KEY_PASSWORD="${MOAYAD_KEY_PASSWORD:-$MOAYAD_STORE_PASSWORD}"
fi

keytool -genkeypair -v \
  -keystore "$KS" \
  -alias "$ALIAS" \
  -keyalg RSA -keysize 4096 -validity 10950 \
  -storepass "$MOAYAD_STORE_PASSWORD" \
  -keypass "$MOAYAD_KEY_PASSWORD" \
  -dname "CN=Moayad AR, OU=Personal, O=Moayad, C=SA"

chmod 600 "$KS"
unset MOAYAD_STORE_PASSWORD MOAYAD_KEY_PASSWORD
echo "Keystore created: $KS (git-ignored). Back it up privately — losing it blocks future updates."
