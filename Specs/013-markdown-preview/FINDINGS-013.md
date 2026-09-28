# Findings — LOT-013

> Before working on this lot, read the repository root README.md.

## F-013-001 — Renderer selection completed

Status: RESOLVED
Recorded: 2026-09-28T00:00:00+02:00

Observation: Package research compared a native WPF FlowDocument renderer with HTML and RTF conversion routes. `MdXaml` 1.27.0 supports Markdown-to-FlowDocument and is compatible with `net10.0-windows`. The direct `Markdig.Wpf` route is archived; the HTML-to-RTF route is unnecessary for preview and adds a large commercial conversion dependency.

Evidence: D-042; NuGet package pages for MdXaml, Markdig.Wpf, and SautinSoft.HtmlToRtf.

Impact: LOT-013 renders Markdown through MdXaml, keeps raw Markdown as the authoritative result, and excludes HTML and RTF rendering.

Destination: REQ-013-001 through REQ-013-004; D-042.
