# TextAid language-pack layout

Language packs are optional, offline JSON files. TextAid never downloads them, does not require a GitHub account, and never includes personal corrections in an exported pack.

## Repository-ready layout

```text
language-packs/
  fr/
    fr.textaid-language-pack.json
    README.md
```

The JSON document uses format version `1` and contains `language` (a BCP-47 tag), `sourceFingerprint`, every English-catalog key under `translations`, `provenance`, and optional `reviewMetadata`. A pack is accepted only when its fingerprint matches the installed English catalog, its key set is exact, and all placeholders (for example `{name}`) are unchanged.

Importing replaces only the compatible suggested cache for the pack language. TextAid validates the complete document before writing it; an invalid, mismatched, or unreadable pack leaves both the prior cache and the user's `locales/overrides/<language>.json` file unchanged. Personal corrections take precedence over the suggested cache and can be restored per key in the translation-review editor.

The active translation origin is one of: **Generated locally**, **Imported pack**, or **Personal override**. Pack provenance and review metadata are stored with an imported cache for display; they are not transmitted by TextAid.
