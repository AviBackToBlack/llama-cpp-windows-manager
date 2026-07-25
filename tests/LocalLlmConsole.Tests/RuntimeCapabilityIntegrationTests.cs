using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using LocalLlmConsole.Localization;
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
    public void AdvancedSettings_HiddenFromSearch_WhenAdvancedToggleIsOff()
    {
        RunInSta(() =>
        {
            var label = new TextBlock { Text = "Draft model" };
            var editor = new TextBox { Tag = "--draft" };
            var section = new Border();

            var panelControls = new LaunchSettingsPanelControls
            {
                Root = new StackPanel(),
                RuntimeCombo = new ComboBox(),
                ModelCapabilityText = new TextBlock(),
                LaunchSettingsSearchBox = new TextBox { Text = "draft" },
                AdvancedLaunchSettingsButton = new Button(),
                SaveModelLaunchSettingsButton = new Button(),
                SaveAsNewModelNameBox = new TextBox(),
                SaveAsNewModelButton = new Button(),
                FormControls = new LaunchSettingsFormControls(),
                LaunchSettingElements = new Dictionary<string, List<FrameworkElement>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Draft model"] = [label, editor]
                },
                AdvancedLaunchSettingLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Draft model" },
                LaunchSettingSections =
                [
                    new LaunchSettingsSectionElements("Speculative", section, ["Draft model"], true)
                ],
                AdvancedLaunchSections = [section]
            };

            var state = new LaunchSettingsPanelState();
            state.Apply(panelControls);

            var plan = new LaunchSettingsControlStatePlan(
                ShowAdvancedSections: false,
                GpuLayersAvailable: false,
                VisionLaunchSettingsAvailable: false,
                MtpHeadSettingsAvailable: false,
                DraftSpeculativeSettingsAvailable: false,
                VisibleSettings: new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase),
                EnabledSettings: new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase));

            state.ApplyControlState(plan);

            Assert.Equal(Visibility.Collapsed, label.Visibility);
            Assert.Equal(Visibility.Collapsed, editor.Visibility);
            Assert.Equal(Visibility.Collapsed, section.Visibility);

            plan = plan with { ShowAdvancedSections = true };
            state.ApplyControlState(plan);

            Assert.Equal(Visibility.Visible, label.Visibility);
            Assert.Equal(Visibility.Visible, editor.Visibility);
            Assert.Equal(Visibility.Visible, section.Visibility);
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

    [Fact]
    public async Task LaunchSettingsRuntimeCapabilityServicePreservesUnknownFlags()
    {
        var tempExecutable = Path.GetTempFileName();
        try
        {
            var runner = new FakeRuntimeFlagHelpRunner("--flash-attn\n--future-runtime-flag");
            var capabilityService = new RuntimeFlagCapabilityService(runner, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            var applicationService = new LaunchSettingsRuntimeCapabilityApplicationService(capabilityService);
            var runtimes = new List<RuntimeRecord>
            {
                new("runtime-1", "Cuda Runtime", RuntimeMode.Native, RuntimeBackend.Cuda, tempExecutable, "{}", DateTimeOffset.UtcNow)
            };

            var result = await applicationService.GetCapabilitiesAsync(
                "runtime-1",
                () => Task.FromResult<IReadOnlyList<RuntimeRecord>>(runtimes),
                null,
                TestContext.Current.CancellationToken);

            Assert.NotNull(result);
            Assert.Contains("--flash-attn", result.Supported);
            Assert.Contains("--future-runtime-flag", result.Unknown);
        }
        finally
        {
            try { File.Delete(tempExecutable); } catch { }
        }
    }

    [Fact]
    public void RuntimeUnknownFlagsAreSurfacedThroughCustomParametersFallback()
    {
        RunInSta(() =>
        {
            var state = new LaunchSettingsPanelState();
            state.FormControls.RuntimeDiscoveredFlagsText = new TextBlock();
            state.FormControls.CustomParametersBox = new TextBox();
            var label = new TextBlock();
            state.LaunchSettingElements[Loc.T("Launch.Field.RuntimeDiscoveredFlags")] =
            [
                label,
                state.FormControls.RuntimeDiscoveredFlagsText
            ];

            state.SetRuntimeCapabilities(new RuntimeFlagCapabilityResult(
                new HashSet<string>(StringComparer.Ordinal) { "--flash-attn" },
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal) { "--future-runtime-flag" }));
            state.ApplyControlState(EmptyPlan());

            Assert.Contains("--future-runtime-flag", state.FormControls.RuntimeDiscoveredFlagsText.Text);
            Assert.Contains("Custom params", state.FormControls.RuntimeDiscoveredFlagsText.Text);
            Assert.Contains("--future-runtime-flag", state.FormControls.CustomParametersBox.ToolTip?.ToString());
            Assert.Equal(Visibility.Visible, label.Visibility);

            state.SetRuntimeCapabilities(null);
            state.ApplyControlState(EmptyPlan());

            Assert.Equal("", state.FormControls.RuntimeDiscoveredFlagsText.Text);
            Assert.Equal(Visibility.Collapsed, label.Visibility);
        });
    }

    [Fact]
    public void RuntimeCapabilityRequestCoordinatorRejectsOlderSameRuntimeRequest()
    {
        var coordinator = new RuntimeCapabilityRequestCoordinator();
        var first = coordinator.Begin("runtime-1");
        var second = coordinator.Begin("runtime-1");

        Assert.False(coordinator.IsCurrent(first, "runtime-1"));
        Assert.True(coordinator.IsCurrent(second, "runtime-1"));
        Assert.False(coordinator.IsCurrent(second, "runtime-2"));
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
