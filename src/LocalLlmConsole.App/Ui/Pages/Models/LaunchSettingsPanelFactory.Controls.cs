using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;
using WpfBrush = System.Windows.Media.Brush;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

public static partial class LaunchSettingsPanelFactory
{
    private static Grid RuntimeAndPortRow(WpfComboBox runtimeCombo, WpfTextBox launchPortBox)
    {
        var runtimeGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        runtimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(98) });
        runtimeGrid.ColumnDefinitions.Add(new ColumnDefinition());
        runtimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        runtimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        runtimeGrid.Children.Add(new TextBlock
        {
            Text = Loc.T("Launch.RuntimeLabel"),
            Foreground = (WpfBrush)WpfApplication.Current.Resources["TextMuted"],
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 2)
        });
        Grid.SetColumn(runtimeCombo, 1);
        runtimeGrid.Children.Add(runtimeCombo);

        var portLabel = new TextBlock
        {
            Text = Loc.T("Launch.PortLabel"),
            Foreground = (WpfBrush)WpfApplication.Current.Resources["TextMuted"],
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 7, 2),
            ToolTip = Loc.T("Tooltip.LaunchPort")
        };
        Grid.SetColumn(portLabel, 2);
        runtimeGrid.Children.Add(portLabel);
        Grid.SetColumn(launchPortBox, 3);
        runtimeGrid.Children.Add(launchPortBox);
        return runtimeGrid;
    }

    private static WpfComboBox RuntimeCombo(LaunchSettingsPanelRequest request)
    {
        var combo = new WpfComboBox
        {
            ItemsSource = request.RuntimeChoices,
            DisplayMemberPath = nameof(RuntimeChoice.Label),
            SelectedValuePath = nameof(RuntimeChoice.Id),
            MinHeight = 29,
            Margin = new Thickness(0, 0, 4, 2),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            ToolTip = Loc.T("Tooltip.RuntimeCombo")
        };
        combo.SelectionChanged += (_, _) => request.RuntimeSelectionChanged();
        return combo;
    }

    private static Grid LaunchSettingsToolbar(
        LaunchSettingsPanelRequest request,
        out WpfTextBox searchBox,
        out WpfButton advancedButton)
    {
        const double toolbarControlHeight = 30;
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        searchBox = new WpfTextBox
        {
            Height = toolbarControlHeight,
            MinHeight = toolbarControlHeight,
            MinWidth = 150,
            Margin = new Thickness(0, 0, 6, 0),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            ToolTip = Loc.T("Tooltip.LaunchSettingsSearch")
        };
        searchBox.TextChanged += (_, _) => request.LaunchSettingsSearchChanged();
        Grid.SetColumn(searchBox, 0);
        grid.Children.Add(searchBox);

        var showAdvanced = request.ShowAdvancedLaunchSettings;
        var toggleButton = new WpfButton
        {
            Content = AdvancedButtonText(showAdvanced),
            Height = toolbarControlHeight,
            MinHeight = toolbarControlHeight,
            MinWidth = 126,
            Margin = new Thickness(0),
            ToolTip = Loc.T("Tooltip.AdvancedSettings")
        };
        ToolTipService.SetShowOnDisabled(toggleButton, true);
        toggleButton.Click += (_, _) =>
        {
            showAdvanced = !showAdvanced;
            toggleButton.Content = AdvancedButtonText(showAdvanced);
            request.AdvancedSettingsChanged(showAdvanced);
        };
        advancedButton = toggleButton;
        Grid.SetColumn(advancedButton, 1);
        grid.Children.Add(advancedButton);

        return grid;
    }

    private static string AdvancedButtonText(bool showAdvanced)
        => showAdvanced ? Loc.T("Launch.HideAdvanced") : Loc.T("Launch.ShowAdvanced");

    private static WrapPanel ActionButtons(LaunchSettingsPanelRequest request, out WpfButton saveForModelButton)
    {
        var actions = Bar();
        saveForModelButton = Button(Loc.T("Launch.SaveForModelButton"), request.SaveForModelAsync);
        actions.Children.Add(saveForModelButton);
        actions.Children.Add(Button(Loc.T("Launch.SaveAsDefaultButton"), request.SaveDefaultsAsync));
        actions.Children.Add(Button(Loc.T("Launch.ResetDefaultsButton"), () =>
        {
            request.ResetDefaults();
            return Task.CompletedTask;
        }));
        return actions;
    }

    private static Grid SaveAsNewRow(LaunchSettingsPanelRequest request, out WpfTextBox nameBox, out WpfButton saveButton)
    {
        var saveAsNewGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        saveAsNewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(98) });
        saveAsNewGrid.ColumnDefinitions.Add(new ColumnDefinition());
        saveAsNewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        saveAsNewGrid.Children.Add(new TextBlock
        {
            Text = Loc.T("Launch.SaveAsNewLabel"),
            Foreground = (WpfBrush)WpfApplication.Current.Resources["TextMuted"],
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 2),
            ToolTip = Loc.T("Tooltip.SaveAsNewLabel")
        });
        nameBox = new WpfTextBox
        {
            MinHeight = 29,
            Margin = new Thickness(0, 0, 6, 2),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            ToolTip = Loc.T("Tooltip.SaveAsNewNameBox")
        };
        nameBox.TextChanged += (_, _) => request.SaveAsNewNameChanged();
        Grid.SetColumn(nameBox, 1);
        saveAsNewGrid.Children.Add(nameBox);
        saveButton = Button(Loc.T("Launch.SaveAsNewButton"), request.SaveAsNewAsync);
        saveButton.ToolTip = Loc.T("Tooltip.SaveAsNewButton");
        ToolTipService.SetShowOnDisabled(saveButton, true);
        Grid.SetColumn(saveButton, 2);
        saveAsNewGrid.Children.Add(saveButton);
        return saveAsNewGrid;
    }

    private static WpfTextBox LaunchTextBox(int value) => LaunchTextBox(value.ToString(CultureInfo.InvariantCulture));

    private static WpfTextBox LaunchTextBox(double value) => LaunchTextBox(value.ToString("0.###", CultureInfo.InvariantCulture));

    private static WpfTextBox LaunchTextBox(string value) => new()
    {
        Text = value,
        MinHeight = 29,
        MinWidth = 72,
        Margin = new Thickness(0, 0, 4, 2),
        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
    };

    private static WpfComboBox LaunchCombo(params string[] values) => LaunchCombo((IEnumerable<string>)values);

    private static WpfComboBox LaunchCombo(IEnumerable<string> values) => new()
    {
        ItemsSource = values.ToArray(),
        SelectedIndex = 0,
        MinHeight = 27,
        MinWidth = 76,
        Margin = new Thickness(0, 0, 6, 4),
        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
    };
}
