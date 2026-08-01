# GitHub Release v2.0.0 Draft

This file is the copy/paste source for the next GitHub release.

## Copy/Paste Release Notes

### llama.cpp Windows Manager v2.0.0

This is a substantial usability and reliability update focused on making model
launching clearer without taking away llama.cpp's flexibility.

- **Runtime-aware launch settings:** the app now reads the selected runtime's
  own `--help` output and renders its supported options automatically. Known
  choices use dropdowns, flags use buttons/toggles, values use validated fields,
  and the remaining CPU/GPU options are searchable instead of being limited to
  a hardcoded list.
- **Real launch profiles:** model files are kept separate from named launch
  profiles. Every model gets a Default profile, profiles can be edited, copied,
  selected in Overview, and safely removed while always leaving one usable
  profile. The loaded-session table shows exactly which profile is running.
- **Clearer Overview:** model/runtime names are shortened to their friendly
  names, loaded sessions and gateway routing are easier to understand, and the
  dashboard uses compact, consistently sized metric cards with CPU/GPU details.
- **Accurate multi-slot metrics:** token totals and rates account for parallel
  slot reuse. Tokens, speculative tokens, and KV-cache occupancy now have
  aligned 60-sample trend graphs.
- **Safer runtime lifecycle:** loading work stays responsive, endpoint failures
  are distinguished from process state, unloads are verified before a session
  disappears, and recent failures/unloads retain a reason. Lifecycle events are
  recorded for later diagnosis.
- **Modernized interface:** improved typography, contrast, spacing, component
  hierarchy, navigation state, compact settings controls, readable search, and
  clearer action emphasis while retaining the familiar layout.
- **Release hardening:** version 2.0.0 metadata, expanded automated/WPF tests,
  signed-release workflow support, SHA-256 companions, safer process cleanup,
  localization improvements, and removal of the legacy `LlamaCppConsole.exe`
  alias.

Existing models, runtimes, settings, and app data are preserved during upgrade.
Unsigned local builds may still show a Windows SmartScreen warning; verify the
included SHA-256 companion files.

#### Artifacts To Upload

- `dist\LlamaCppWindowsManager-win-x64.zip`
- `dist\LlamaCppWindowsManager-win-x64.zip.sha256`
- `dist\installer\LlamaCppWindowsManager-Setup-2.0.0-win-x64.exe`
- `dist\installer\LlamaCppWindowsManager-Setup-2.0.0-win-x64.exe.sha256`

## Verification

Full local release gate:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\test-release-gate.ps1 -Runtime win-x64 -Configuration Release -IncludePublish -IncludeInstaller -InnoSetupPath "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
```

Public releases should use the protected GitHub release workflow so the
portable executable is signed before upload.
