using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using LocalLlmConsole.Models;
using LocalLlmConsole.Services;

namespace LocalLlmConsole.Tests;

public sealed class RuntimeCapabilityIntegrationTests
{
    [Fact]
    public void SupportedFlagEnablesControl()
    {
        RunInSta(() =>
        {
            var state = new LaunchSettingsPanelState();
            state.FormControls.FlashAttentionCombo = new ComboBox { Tag = "--flash-attn" };
            state.LaunchSettingElements["Flash attention"] = new List<FrameworkElement>
            {
                new TextBlock(),
                state.FormControls.FlashAttentionCombo
            };

            var plan = EmptyPlan();
            state.SetSupportedFlags(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--flash-attn" });
            state.ApplyControlState(plan);

            Assert.True(state.FormControls.FlashAttentionCombo.IsEnabled);
        });
    }

    [Fact]
    public void MissingSupportedFlagDisablesControl()
    {
        RunInSta(() =>
        {
            var state = new LaunchSettingsPanelState();
            state.FormControls.SpeculativeTypeCombo = new ComboBox { Tag = "--spec-type" };
            state.LaunchSettingElements["Spec type"] = new List<FrameworkElement>
            {
                new TextBlock(),
                state.FormControls.SpeculativeTypeCombo
            };

            var plan = EmptyPlan();
            state.SetSupportedFlags(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--flash-attn" });
            state.ApplyControlState(plan);

            Assert.False(state.FormControls.SpeculativeTypeCombo.IsEnabled);
            Assert.Contains("Not supported", state.FormControls.SpeculativeTypeCombo.ToolTip as string ?? "", StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void UnsupportedFlagThenSupportedReEnablesControl()
    {
        RunInSta(() =>
        {
            var state = new LaunchSettingsPanelState();
            state.FormControls.FlashAttentionCombo = new ComboBox { Tag = "--flash-attn" };
            state.LaunchSettingElements["Flash attention"] = new List<FrameworkElement>
            {
                new TextBlock(),
                state.FormControls.FlashAttentionCombo
            };

            var plan = EmptyPlan();
            state.SetSupportedFlags(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--ctx-size" });
            state.ApplyControlState(plan);
            Assert.False(state.FormControls.FlashAttentionCombo.IsEnabled);

            state.SetSupportedFlags(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--ctx-size", "--flash-attn" });
            state.ApplyControlState(plan);
            Assert.True(state.FormControls.FlashAttentionCombo.IsEnabled);
        });
    }

    [Fact]
    public void PlanDisabledControlIsNotReEnabledByRuntimeSupport()
    {
        RunInSta(() =>
        {
            var state = new LaunchSettingsPanelState();
            state.FormControls.FlashAttentionCombo = new ComboBox { Tag = "--flash-attn" };
            state.LaunchSettingElements["Flash attention"] = new List<FrameworkElement>
            {
                new TextBlock(),
                state.FormControls.FlashAttentionCombo
            };

            var plan = EmptyPlan();
            state.SetSupportedFlags(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--ctx-size" });
            state.ApplyControlState(plan);
            Assert.False(state.FormControls.FlashAttentionCombo.IsEnabled);

            var disabledPlan = EmptyPlan();
            disabledPlan = disabledPlan with
            {
                EnabledSettings = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Flash attention"] = false
                }
            };
            state.SetSupportedFlags(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--ctx-size", "--flash-attn" });
            state.ApplyControlState(disabledPlan);
            Assert.False(state.FormControls.FlashAttentionCombo.IsEnabled);
        });
    }

    [Fact]
    public async Task LaunchSettingsRuntimeCapabilityServiceResolvesRuntimeAndQueriesCapabilities()
    {
        var tempExecutable = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempExecutable, "");

            var runner = new FakeRuntimeFlagHelpRunner("--flash-attn\n--ctx-size");
            var capabilityService = new RuntimeFlagCapabilityService(runner, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            var applicationService = new LaunchSettingsRuntimeCapabilityApplicationService(capabilityService);
            var runtimes = new List<RuntimeRecord>
            {
                new("runtime-1", "Cuda Runtime", RuntimeMode.Native, RuntimeBackend.Cuda, tempExecutable, "{}", DateTimeOffset.UtcNow)
            };

            var result = await applicationService.GetSupportedFlagsAsync("runtime-1", () => Task.FromResult<IReadOnlyList<RuntimeRecord>>(runtimes), null);

            Assert.NotNull(result);
            Assert.Contains("--flash-attn", result);
        }
        finally
        {
            try { File.Delete(tempExecutable); } catch { }
        }
    }

    private static LaunchSettingsControlStatePlan EmptyPlan()
        => new(
            true,
            true,
            true,
            true,
            true,
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase));

    private static void RunInSta(Action action)
    {
        var tcs = new TaskCompletionSource<object?>();
        var thread = new Thread(() =>
        {
            try
            {
                action();
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        tcs.Task.GetAwaiter().GetResult();
    }

    private sealed class FakeRuntimeFlagHelpRunner : IRuntimeFlagHelpRunner
    {
        private readonly string _output;

        public FakeRuntimeFlagHelpRunner(string output)
        {
            _output = output;
        }

        public Task<ProcessRunResult> RunHelpAsync(string executablePath, RuntimeMode mode, string? wslDistro, CancellationToken cancellationToken = default)
            => Task.FromResult(new ProcessRunResult(0, _output, ""));
    }
}
