using System.Threading;
using System.Windows.Controls;
using LocalLlmConsole.Services;

namespace LocalLlmConsole.Tests;

public sealed class LaunchSettingsControlFactoryTests
{
    [Fact]
    public void SetComboValue_NormalizesNonCanonicalFirstClassValues()
    {
        RunInSta(() =>
        {
            var flag = LlamaServerFlagSchema.FindByName("--flash-attn")!;
            var control = LaunchSettingsControlFactory.CreateControl(flag, "true");
            var combo = (ComboBox)control;

            Assert.Equal("on", combo.SelectedItem);
        });
    }

    [Fact]
    public void SetControlValue_KeepsNullForUnmatchedGeneratedBoolean()
    {
        RunInSta(() =>
        {
            var flag = LlamaServerFlagSchema.FindByName("--version")!;
            var control = LaunchSettingsControlFactory.CreateControl(flag, null);

            Assert.Equal(-1, ((ComboBox)control).SelectedIndex);
            Assert.True(string.IsNullOrWhiteSpace(LaunchSettingsControlFactory.GetControlValue(control)));
        });
    }

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
}
