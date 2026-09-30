# TextAid website: content and capture plan

This directory contains the English, static HTML website. It sits beside `src/` so the website can be published independently of the Windows executable. There is no build step or external runtime.

## Pages

The site has two clear entry points: **Discover TextAid** and **User Guide**. All pages will use relative links and local assets, so the directory can be copied to a web server without a build step or a fixed base URL.

The discovery pages prominently link to the public [source repository](https://github.com/odahan/TextAid) and [compiled releases](https://github.com/odahan/TextAid/releases). The header links to releases from every page. Product copy says that the source is publicly available; the repository's current CC BY-NC 4.0 terms are non-commercial and its README explicitly calls the project source-available rather than OSI-approved Open Source.

## Approved visual direction

Use the **TextAid native** dark direction selected for the website preview. Match the application's blue-black surfaces, cyan accent, restrained violet highlight, and clear panel treatment. The discovery pages may use a larger hero and visual examples; the guide should keep the same identity with a quieter, comfortable reading layout. All site copy and navigation are in English for the first release.

The site uses the actual `site/images/Logo-final.png` asset for branding and the supplied product captures in `site/images/`.

| Page | Purpose and content |
| --- | --- |
| `index.html` | Product introduction: what TextAid does, the three AI connection locations (device, administered network, external service), a short visual example, and clear links to the guide. |
| `features.html` | Short use cases for correcting, rewriting, generating, summarizing, and translating text; explain Choose and quick Translate at a glance. Each use case links to the relevant guide page. |
| `guide.html` | Guide landing page and reading path. State the prerequisite (configure a connection and model) in one short callout, linking to `guide-settings.html`. |
| `guide-first-result.html` | First success with `Ctrl+C`, then `C`: select text, choose an action, process, inspect the preview, Replace/Copy/Cancel. Introduce the main window only as needed. |
| `guide-quick-translate.html` | `Ctrl+C`, then `T`: automatic initial translation, destination language, changing destination and translating again, reviewing the result. Link to shortcut settings. |
| `guide-workspace.html` | Daily use: edit captured text, choose action/output language, use the four presets, Free input, one-off Instructions, New, and Markdown preview. |
| `guide-actions.html` | Compact inventory of supplied actions, default preset assignments, preset reassignment, and action creation/editing/enabling/deletion, with one small custom-action example. |
| `guide-settings.html` | Detailed configuration placed late in the guide: device/network/external connections, model profiles, preferred translation language, shortcuts, Windows startup, and diagnostic logs. |
| `guide-interface-language.html` | Interface language versus output language, generation and review of UI translations, personal corrections, language-pack import/export, and a short troubleshooting section. |

The guide should be concise and task-oriented. The discovery pages should present capabilities without duplicating the procedures. Navigation and examples should use the exact English labels displayed by the application.

## Image location and names

The **six supplied captures** live directly in `site/images/`. The old files in `assets/Screenshots/` are not used.

| File | What to capture | Used on |
| --- | --- | --- |
| `01-choose-result.png` | The complete TextAid main window after **Correct** has processed manually entered text in Choose mode. Replace is disabled because there is no captured source selection; Copy remains available. This is the primary product image. | Home, features, first result, workspace |
| `02-quick-translate.png` | The complete TextAid main window after `Ctrl+C`, then `T` has translated a short French passage into English. Keep the selected destination language visible. | Features, quick Translate |
| `03-actions-editor.png` | **Actions** window with a non-reserved action selected, so its name, profile, output language, instruction option, and prompt are visible. A harmless example custom action is ideal. | Actions guide |
| `04-settings-local.png` | **Settings → This device only** with a working local connection and a model selected. Show the tab name, connection status, model, and Save button. | Settings guide |
| `05-settings-language.png` | **Settings → Language** with application language, preferred translation language, and both shortcut fields visible. Scroll or enlarge the window enough to show them together if possible. | Quick Translate, settings, interface language |
| `06-translation-review.png` | **Translation review** with a few translated rows, one selected row, and the personal correction plus Import/Export language pack controls visible. | Interface language guide |

### Capture guidance

- Use the current Release build and set the **TextAid interface to English** for all captures. The text being translated may naturally be French.
- Capture the application window cleanly, without unrelated desktop or browser content. PNG at native resolution is preferred; do not upscale or add annotations. Leave enough room for all relevant labels to be legible.
- Use short, neutral example text. For `01-choose-result.png`, a useful input is: “Please send me you're feedback by Friday.” The precise AI wording need not match a script, but the before/after should be easy to understand.
- For `02-quick-translate.png`, a useful input is: “Merci de confirmer votre disponibilité pour la réunion de jeudi.”
- Hide personal text, API keys, internal server addresses, account names, and diagnostic data. For the local Settings image, a loopback endpoint is fine.
- Please keep the screenshots unannotated. Callouts and cropping can be handled in the site layout after reviewing the supplied images.

The existing logo has been copied from `assets/Logo-final.png` to `site/images/Logo-final.png`; no new logo capture is requested.

## Optional captures

These are useful if easy to obtain, but the site can be completed without them:

| File | What it would clarify |
| --- | --- |
| `07-preset-assignment.png` | The menu opened by a preset's gear button, showing how an action is assigned to one of the four buttons. |
| `08-settings-network.png` | The On-premises settings tab, with a harmless example address and no secret. |
| `09-settings-external.png` | The External settings tab, with no API key visible. |

## Preview and publish

Open `site/index.html` in a browser to preview the site. Upload the **contents** of `site/` to the target web directory, preserving the filenames and the `images/` folder. Every page, stylesheet, and image uses a relative URL, so the site works at a domain root or in a subdirectory. Update the application's About-box URL separately after the final public URL is known.
