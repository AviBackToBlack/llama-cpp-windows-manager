# GitHub Release v1.1.5 Draft

This file is the copy/paste source for the next GitHub release. It tracks only
changes made after the published `v1.1.4` release.

## Copy/Paste Release Notes

### llama.cpp Windows Manager v1.1.5

v1.1.5 is a focused follow-up release for OpenCode output limits, runtime
dashboard accuracy, context checkpoint compatibility, and clearer companion
GGUF naming guidance.

#### Latest Changes

- Added **Settings > OpenCode > Limit output** and made synced OpenCode
  `limit.output` values follow that setting instead of each model's llama.cpp
  **Max tokens** launch setting.
- Renamed the OpenCode sync preference to **Auto-sync entries** and made the
  setting gate automatic OpenCode rewrites after Settings saves, launch-setting
  saves, variant saves, and generated API-key persistence.
- Updated OpenCode config writes so model `limit` blocks preserve context/output
  values intentionally and omit default output caps when no output limit should
  be written.
- Fixed context checkpoint launch arguments for current llama.cpp builds by
  using `--checkpoint-min-step` and no longer sending the removed
  `--checkpoint-every-n-tokens` flag.
- Improved Overview runtime metrics so context display uses the correct total
  for parallel slots and KV-unified mode, and so multi-slot context sizes are
  not double-counted from `/slots`.
- Polished the Model Status card sizing and loaded-duration handling so stale
  loading timers do not refresh or overwrite status when no load was in
  progress.
- Documented exact companion GGUF auto-detection naming rules in README and
  in-app Help for vision/projector files and upstream draft/MTP assistant files.
- Added an advanced **Custom params** launch setting for raw llama-server flags
  such as `--n-cpu-moe`, with quoted path parsing for values that contain
  spaces.
- Bumped app metadata and visible UI version labels to `v1.1.5`.

#### Upgrade Notes

- Existing models, runtimes, logs, cache, state, and settings are preserved by
  installer update/repair and by default uninstall.
- Existing OpenCode entries are rewritten only when **Auto-sync entries** is
  enabled or when you update/add entries from the OpenCode page.
- This release is unsigned. Verify the `.sha256` companion assets and expect
  Windows SmartScreen or publisher warnings until a trusted signing certificate
  is used.

#### Artifacts To Upload

- `dist\LlamaCppWindowsManager-win-x64.zip`
- `dist\LlamaCppWindowsManager-win-x64.zip.sha256`
- `dist\installer\LlamaCppWindowsManager-Setup-1.1.5-win-x64.exe`
- `dist\installer\LlamaCppWindowsManager-Setup-1.1.5-win-x64.exe.sha256`

## Verification

Full release gate verified locally on 2026-06-08:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\test-release-gate.ps1 -Runtime win-x64 -Configuration Release -IncludePublish -IncludeInstaller -InnoSetupPath "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
```

Result: Release build succeeded with zero warnings, release-hardening tests
passed (`442/442`), formatting was clean, no vulnerable packages were found,
the diff had no whitespace errors, and portable/installer artifact checks
passed locally.
