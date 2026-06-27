# GitHub Release v1.1.7 Draft

This file is the copy/paste source for the next GitHub release. It tracks only
changes made after the published `v1.1.6` release.

## Copy/Paste Release Notes

### llama.cpp Windows Manager v1.1.7

v1.1.7 is a major release with multi-language support (21 languages), security
hardening across process supervision, the API key lifecycle, and the model
gateway, plus metrics accuracy improvements for parallel-slot workloads.

#### Multi-Language Support

- **21 languages** now available in the sidebar language selector, covering
  ~96% of global users: Bulgarian (human-translated), plus 17 machine-translated
  languages at 87-98% coverage (Spanish, Russian, German, French, Japanese,
  Portuguese, Chinese, Italian, Turkish, Persian, Polish, Dutch, Vietnamese,
  Korean, Indonesian, Czech, Swedish). Arabic and Hindi use English fallback
  text pending native-script translations.
- All navigation buttons, page titles, column headers, field labels, dialog
  strings, status messages, tray menu items, and tooltip templates are now
  localised across all 530 translation keys. The language selection is persisted
  and applied immediately on app restart.
- English is always loaded as a fallback, so missing translations display
  gracefully in English rather than showing raw key names.

#### Security Hardening

- **Custom parameter guard** — User-supplied custom launch parameters can no
  longer override security-critical `llama-server` flags (`--host`, `--port`,
  `--api-key`). Attempting to do so now shows a clear error message.
- **Native process cleanup** — Native Windows `llama-server.exe` processes are
  now bound to a Windows Job Object with `KILL_ON_JOB_CLOSE`, so they are
  terminated automatically if the app crashes or is force-killed. Previously,
  native processes could become orphaned and continue running on their ports.
- **API key lifecycle** — Disabling API key authentication now clears the
  stored key from settings and triggers an OpenCode credential wipe (writes
  `"EMPTY"` to the provider config) instead of leaving the old key in plaintext.
- **API key in process commands** — The model API key is now passed via the
  `LLAMA_API_KEY` environment variable instead of a command-line argument,
  keeping it out of process command lines visible in Task Manager or WMI. For
  WSL launches, `WSLENV` forwarding is used to avoid embedding the key in the
  bash command string.
- **Gateway upstream timeout** — Added a 15-minute HTTP timeout on gateway
  upstream proxy requests to prevent thread-pool exhaustion during hung model
  inference.
- **Null-byte rejection** — WSL shell quoting (`BashQuote`) now rejects null
  bytes as a defense-in-depth measure against command truncation.
- **Partial download cleanup** — Cancelled or failed Hugging Face model
  downloads now delete their `.partial` files automatically, preventing
  multi-gigabyte orphaned files from accumulating in the models folder.

#### Metrics Accuracy

- **Parallel-slot rate correction** — The generation live rate now uses llama.cpp's
  reported active-generation seconds (`tokens_predicted_seconds_total`)
  instead of wall-clock time between polls. This prevents rate dilution when
  the model is idle between requests — a 2-slot session that generates at
  35 t/s during bursts no longer shows an artificially low 13 t/s average
  during quiet periods.
- **Newer llama.cpp support** — The `/slots` endpoint parser now handles the
  current `next_token` object format (single object) in addition to the
  legacy array format, so per-slot generated-token counters are correctly
  populated across recent `llama.cpp` builds.
- **Broader metric matching** — Prometheus counter patterns expanded to
  match additional metric name variants (`tokens_decoded_total`,
  `tokens_generated` without `_total` suffix), improving compatibility
  with evolving upstream `llama.cpp` metric naming.

#### Localization Fixes

- Fixed navigation buttons showing raw key names (e.g. `Nav.Overview`) instead
  of translated labels on app restart with a non-English saved language.
- Fixed the language selector combo being empty on first launch after
  initialisation.
- Fixed active navigation highlighting breaking for non-English languages
  due to `Loc.T()` being used for internal page identifiers.
- Fixed the `UpdatesNavButton` label being overwritten with hardcoded
  English by the background update checker.
- Localised ~80 previously hardcoded English strings across page factories,
  view models, dialog factories, column headers, and status messages.

#### UI

- Added a visual separator between the language selector and the navigation
  buttons in the sidebar, matching the existing Tools section divider.

#### Build & Dependencies

- Bumped app metadata, visible UI version labels, and docs to `v1.1.7`.
- 19 new embedded localisation JSON resources added to the build.
- New `ProcessJobObjectService` infrastructure service for native Windows
  Job Object management.

#### Upgrade Notes

- Existing models, runtimes, logs, cache, state, and settings are preserved
  by installer update/repair and by default uninstall.
- No configuration changes are required. The language selector defaults to
  English; select another language from the sidebar dropdown to switch.
- This release is unsigned. Verify the `.sha256` companion assets and expect
  Windows SmartScreen or publisher warnings until a trusted signing certificate
  is used.

#### Artifacts To Upload

- `dist\LlamaCppWindowsManager-win-x64.zip`
- `dist\LlamaCppWindowsManager-win-x64.zip.sha256`
- `dist\installer\LlamaCppWindowsManager-Setup-1.1.7-win-x64.exe`
- `dist\installer\LlamaCppWindowsManager-Setup-1.1.7-win-x64.exe.sha256`

## Verification

Full release gate:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\test-release-gate.ps1 -Runtime win-x64 -Configuration Release -IncludePublish -IncludeInstaller -InnoSetupPath "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
```

Result: Release build succeeded with zero warnings, release-hardening tests
passed, formatting was clean, no vulnerable packages were found, the diff had
no whitespace errors, and portable/installer artifact checks passed locally.
