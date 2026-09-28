# Gates — LOT-013

> Before working on this lot, read the repository root README.md.

## G-013-001 — Rendered-preview regression tests

Type: AUTO
Status: TO TEST

Condition:
Deterministic tests prove that the supported Markdown constructs produce a native readable document under the dark theme; plain-text output remains plain; raw HTML is inert; links cannot activate; a renderer failure falls back to raw plain text; and Copy/Replace retain the exact raw Markdown result with their existing safe state gates.

Method:
Run `dotnet test -m:1` for applicable projects and inspect the built WPF application.

## G-013-002 — Windows rendered-preview walkthrough

Type: HUMAN
Status: TO TEST

Condition:
On Windows, a user reviews a Markdown result containing headings, emphasis, lists, quotation, code, and a link in the dark preview; confirms keyboard accessibility and no external navigation; then confirms Copy and safe Replace still use the raw Markdown text.

Method:
Exercise the published self-contained TextAid.exe.
