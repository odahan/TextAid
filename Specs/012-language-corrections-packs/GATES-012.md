# Gates — LOT-012

## G-012-001 — Locale-pack safety tests

Type: AUTO
Status: TO TEST

Condition: Tests cover override precedence and restoration; valid import/export; fingerprint mismatch; missing/extra keys; placeholder loss; invalid JSON; and preservation of the previous active cache after every rejected import.

Method: Run `dotnet test -m:1` for Core and App-related tests using deterministic temporary directories.

## G-012-002 — Translation correction walkthrough

Type: HUMAN
Status: TO TEST

Condition: A user corrects one translation, confirms it survives a suggested-cache replacement, restores the suggested value, exports a pack, and imports a compatible pack without any network access.

Method: Exercise the themed review editor on Windows.
