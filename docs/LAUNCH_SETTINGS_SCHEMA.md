# Schema-driven launch settings

## Goal

Render the existing polished launch form and additional runtime-supported settings through one extensible pipeline without making runtime help text the source of application policy.

The implementation deliberately separates four concerns:

1. **UI schema** — labels, sections, editor types, choices, picker behavior, and advanced visibility.
2. **Launch projection** — conversion from `AppSettings` to `RuntimeLaunchRequest` and then to argument tokens.
3. **Runtime capabilities** — options advertised by the selected executable's `--help` output.
4. **Safety policy** — flags owned by the application or unsafe to expose as generic launch fields.

## Rendering flow

`LaunchSettingUiSchema` is the curated schema for settings that need application-level polish or composite behavior. `LaunchSettingsPanelFactory` iterates this schema and creates text fields, dropdowns, and the existing model/head file pickers. The form binder continues to own parsing and cross-field validation, so the migration does not change existing values or validation rules.

After a runtime is selected:

1. `RuntimeLaunchOptionDiscoveryService` invokes that exact executable with `--help`.
2. Native and WSL runtimes are handled separately; stdout and stderr are always combined.
3. `RuntimeLaunchHelpParser` preserves every advertised alias and infers switch, choice, text, file, or directory semantics.
4. `RuntimeLaunchOptionPolicy` removes application-managed, credential-bearing, network/security, and utility/action flags.
5. `LaunchRuntimeOptionsPanel` renders the remaining options and emits the exact long alias advertised by the selected runtime.

Native discovery is cached by runtime identity, executable path, and file modification time. WSL discovery is repeated because a reliable remote executable timestamp is not available locally.

## Discovery diagnostics

Each discovery attempt writes a compact JSON record under `diagnostics/runtime-options` in the app workspace. The record contains the selected runtime identity, executable fingerprint, help exit code, first non-empty banner line, a SHA-256 fingerprint of the help output, parse/render counts, and a stable status such as `success`, `empty-help`, or `unrecognized-help`.

The full help output is deliberately not persisted. This keeps diagnostics useful for identifying runtime-help format changes without turning the diagnostic folder into a copy of arbitrary process output.

## Persistence and import

Additional structured values serialize into the existing `CustomParameters` profile field. This keeps global defaults, per-model profiles, and existing databases backward compatible.

On profile load, known tokens hydrate their structured runtime editors and unknown tokens remain in the raw fallback field. When the selected runtime changes, structured values are materialized before the old editor set is removed, so settings are not lost. Unsupported aliases remain raw rather than being silently rewritten.

The command preview is read-only. Importing edited raw parameters is an explicit action; editing the preview never mutates settings.

## Launch parity

`RuntimeLaunchRequestFactory` is shared by real launches and previews. `RuntimeAdapter.BuildArgs` remains the single token-emission implementation. The preview substitutes placeholders for the model and secret validation only; it does not display the API key, which continues to be passed through `LLAMA_API_KEY`.

Custom arguments are validated before application-owned arguments such as metrics are appended. Model, host, port, credentials, curated performance fields, and other managed flags cannot be overridden through raw parameters.

## Adding a polished setting

1. Add the persistent value to `AppSettings` and, when model-specific, `ModelLaunchSettings`.
2. Add a `LaunchSettingUiDefinition` to `LaunchSettingUiSchema` with the appropriate section and editor metadata.
3. Add binder parsing, application, and cross-field validation when the value is not a plain string.
4. Add the command projection to `RuntimeAdapter` and mark every owned alias in `RuntimeLaunchOptionPolicy`.
5. Add round-trip, validation, and argument-emission tests.

Settings discovered at runtime require none of these steps unless they need richer validation or composite behavior than help metadata can express.

## Verification

Tests cover:

- exact alias preservation and choice inference;
- safe/app-managed filtering;
- rejection of model, port, and credential overrides;
- shared preview/launch argument generation;
- curated schema validity and uniqueness;
- the existing application architecture and full regression suite.
