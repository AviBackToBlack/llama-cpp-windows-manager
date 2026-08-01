using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LocalLlmConsole.Services;
using WpfApplication = System.Windows.Application;
using WpfBrush = System.Windows.Media.Brush;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

public sealed class LaunchRuntimeOptionsPanel
{
    private const double EditorHeight = 28;
    private readonly WpfTextBox _rawParameters;
    private readonly Func<string, string?> _chooseFile;
    private readonly Func<string, string?> _chooseDirectory;
    private readonly StackPanel _rows = new();
    private readonly TextBlock _status;
    private readonly WpfTextBox _preview;
    private readonly Dictionary<string, RuntimeOptionEditor> _editors = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RuntimeOptionRow> _optionRows = [];
    private string _baseStatus = "Select a runtime to discover its additional launch settings.";
    private string _searchQuery = "";

    public LaunchRuntimeOptionsPanel(
        WpfTextBox rawParameters,
        Func<string, string?> chooseFile,
        Func<string, string?> chooseDirectory)
    {
        _rawParameters = rawParameters ?? throw new ArgumentNullException(nameof(rawParameters));
        _chooseFile = chooseFile ?? throw new ArgumentNullException(nameof(chooseFile));
        _chooseDirectory = chooseDirectory ?? throw new ArgumentNullException(nameof(chooseDirectory));
        _status = new TextBlock
        {
            Text = _baseStatus,
            Foreground = ResourceBrush("TextMuted"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 5)
        };
        _preview = new WpfTextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 48,
            Margin = new Thickness(0, 6, 0, 5),
            ToolTip = "Read-only preview of the exact launch command."
        };

        var import = new WpfButton
        {
            Content = "Import raw parameters",
            Height = EditorHeight,
            MinHeight = EditorHeight,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left
        };
        import.Click += (_, _) => ImportRawParameters();
        var content = new StackPanel();
        content.Children.Add(_status);
        content.Children.Add(_rows);
        content.Children.Add(new TextBlock
        {
            Text = "Command preview",
            Foreground = ResourceBrush("TextMuted"),
            FontSize = 12,
            Margin = new Thickness(0, 5, 0, 0)
        });
        content.Children.Add(_preview);
        content.Children.Add(import);
        Root = new Border
        {
            Background = ResourceBrush("SurfaceRaised"),
            BorderBrush = ResourceBrush("PanelBorderStrong"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 6),
            Child = content
        };
    }

    public FrameworkElement Root { get; }

    public event Action? Changed;

    public void SetLoading()
    {
        Root.Visibility = Visibility.Visible;
        _baseStatus = "Reading supported settings from the selected runtime...";
        _status.Text = _baseStatus;
    }

    public void SetError(string message)
    {
        MaterializeStructuredValues();
        _editors.Clear();
        _optionRows.Clear();
        _rows.Children.Clear();
        _baseStatus = $"Runtime settings could not be discovered: {message}";
        _status.Text = _baseStatus;
        Root.Visibility = Visibility.Visible;
    }

    public void SetOptions(IReadOnlyList<RuntimeLaunchOptionDefinition> options)
    {
        MaterializeStructuredValues();
        _editors.Clear();
        _optionRows.Clear();
        _rows.Children.Clear();
        foreach (var option in options)
        {
            var editor = CreateEditor(option);
            _editors[option.Name] = editor;
            var row = CreateRow(option, editor.Control);
            _optionRows.Add(new RuntimeOptionRow(row, SearchText(option)));
            _rows.Children.Add(row);
        }

        _baseStatus = options.Count == 0
            ? "This runtime exposes no additional safe launch settings beyond the polished settings above."
            : $"{options.Count} additional settings exposed by this runtime. Empty fields inherit the runtime default; advertised defaults are shown in the controls.";
        ImportRawParameters(notify: false);
        ApplyFilter(_searchQuery);
    }

    public void ApplyFilter(string? query)
    {
        _searchQuery = query?.Trim() ?? "";
        var terms = _searchQuery.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var visible = 0;
        foreach (var optionRow in _optionRows)
        {
            var matches = terms.Length == 0
                || terms.All(term => optionRow.SearchText.Contains(term, StringComparison.OrdinalIgnoreCase));
            optionRow.Row.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
            if (matches) visible++;
        }

        Root.Visibility = terms.Length > 0 && _optionRows.Count > 0 && visible == 0
            ? Visibility.Collapsed
            : Visibility.Visible;
        _status.Text = terms.Length == 0
            ? _baseStatus
            : $"{visible} of {_optionRows.Count} additional runtime settings match your search.";
    }

    public string BuildCustomParameters()
    {
        var tokens = CustomLaunchParameterParser.Parse(_rawParameters.Text).ToList();
        foreach (var editor in _editors.Values)
            editor.AppendTokens(tokens);
        RuntimeLaunchOptionPolicy.ValidateCustomArguments(tokens);
        return LaunchArgumentText.Format(tokens);
    }

    public void UpdatePreview(string command) => _preview.Text = command ?? "";

    public void ImportRawParameters(bool notify = true)
    {
        IReadOnlyList<string> tokens;
        try
        {
            tokens = CustomLaunchParameterParser.Parse(_rawParameters.Text);
        }
        catch (Exception ex)
        {
            _status.Text = $"Raw parameters could not be imported: {ex.Message}";
            return;
        }

        foreach (var editor in _editors.Values) editor.Clear();
        var residual = new List<string>();
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            var equalsIndex = token.IndexOf('=');
            var name = equalsIndex > 0 ? token[..equalsIndex] : token;
            var editor = FindEditor(name);
            if (editor is null)
            {
                residual.Add(token);
                continue;
            }

            var inlineValue = equalsIndex > 0 ? token[(equalsIndex + 1)..] : null;
            if (editor.Option.ValueKind == RuntimeLaunchOptionValueKind.Switch)
            {
                editor.Set("on");
                continue;
            }

            if (inlineValue is not null)
                editor.Set(inlineValue);
            else if (index + 1 < tokens.Count && !tokens[index + 1].StartsWith("-", StringComparison.Ordinal))
                editor.Set(tokens[++index]);
            else
                residual.Add(token);
        }

        _rawParameters.Text = LaunchArgumentText.Format(residual);
        if (notify) Changed?.Invoke();
    }

    private RuntimeOptionEditor? FindEditor(string alias)
        => _editors.Values.FirstOrDefault(editor => editor.Option.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase));

    private void MaterializeStructuredValues()
    {
        if (_editors.Count > 0)
            _rawParameters.Text = BuildCustomParameters();
    }

    private RuntimeOptionEditor CreateEditor(RuntimeLaunchOptionDefinition option)
    {
        FrameworkElement valueControl;
        FrameworkElement control;
        if (option.ValueKind == RuntimeLaunchOptionValueKind.Switch)
        {
            var button = SwitchButton(option);
            valueControl = button;
            control = button;
            button.Click += (_, _) =>
            {
                SetSwitchState(button, button.Tag is not true, option);
                Changed?.Invoke();
            };
        }
        else if (option.ValueKind == RuntimeLaunchOptionValueKind.Choice)
        {
            var combo = CrispCompactControl(new WpfComboBox
            {
                ItemsSource = new[] { RuntimeDefaultLabel(option) }.Concat(option.Choices).ToArray(),
                SelectedIndex = 0,
                Height = EditorHeight,
                MinHeight = EditorHeight,
                Margin = new Thickness(0),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalContentAlignment = VerticalAlignment.Center,
                Foreground = ResourceBrush("TextMain")
            });
            valueControl = combo;
            control = combo;
        }
        else
        {
            var textBox = CrispCompactControl(new WpfTextBox
            {
                Height = EditorHeight,
                MinHeight = EditorHeight,
                Margin = new Thickness(0),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalContentAlignment = VerticalAlignment.Center,
                Foreground = ResourceBrush("TextMain")
            });
            var textEditor = TextEditor(textBox, option);
            valueControl = textBox;
            control = option.ValueKind switch
            {
                RuntimeLaunchOptionValueKind.File => PathEditor(textBox, textEditor, _chooseFile),
                RuntimeLaunchOptionValueKind.Directory => PathEditor(textBox, textEditor, _chooseDirectory),
                _ => textEditor
            };
        }

        control.Height = EditorHeight;
        control.MinHeight = EditorHeight;
        control.ToolTip = OptionToolTip(option);
        var editor = new RuntimeOptionEditor(option, control, valueControl);
        if (valueControl is WpfComboBox comboBox) comboBox.SelectionChanged += OnChanged;
        if (valueControl is WpfTextBox text) text.TextChanged += OnChanged;
        return editor;
    }

    private static Grid TextEditor(WpfTextBox textBox, RuntimeLaunchOptionDefinition option)
    {
        var hint = new TextBlock
        {
            Text = RuntimeDefaultLabel(option),
            Foreground = ResourceBrush("TextMain"),
            FontFamily = new WpfFontFamily("Segoe UI"),
            FontSize = 12,
            FontWeight = FontWeights.Normal,
            Margin = new Thickness(9, 0, 7, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            IsHitTestVisible = false
        };
        TextOptions.SetTextHintingMode(hint, TextHintingMode.Fixed);
        textBox.TextChanged += (_, _) =>
            hint.Visibility = string.IsNullOrEmpty(textBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        var host = new Grid { Height = EditorHeight };
        host.Children.Add(textBox);
        host.Children.Add(hint);
        return host;
    }

    private static Grid PathEditor(WpfTextBox textBox, FrameworkElement textEditor, Func<string, string?> choose)
    {
        var grid = new Grid { Height = EditorHeight };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        textEditor.Margin = new Thickness(0, 0, 5, 0);
        grid.Children.Add(textEditor);
        var button = CrispCompactControl(new WpfButton
        {
            Content = "Choose",
            Height = EditorHeight,
            MinHeight = EditorHeight,
            MinWidth = 62,
            Margin = new Thickness(0)
        });
        button.Click += (_, _) =>
        {
            var selected = choose(textBox.Text.Trim());
            if (!string.IsNullOrWhiteSpace(selected)) textBox.Text = selected;
        };
        Grid.SetColumn(button, 1);
        grid.Children.Add(button);
        return grid;
    }

    private void OnChanged(object sender, RoutedEventArgs args) => Changed?.Invoke();

    private static Grid CreateRow(RuntimeLaunchOptionDefinition option, FrameworkElement control)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 2), MinHeight = EditorHeight };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.Children.Add(new TextBlock
        {
            Text = option.Name,
            Foreground = ResourceBrush("TextSoft"),
            FontSize = 11.5,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            ToolTip = OptionToolTip(option)
        });
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
        return grid;
    }

    private static WpfButton SwitchButton(RuntimeLaunchOptionDefinition option)
    {
        var button = CrispCompactControl(new WpfButton
        {
            Height = EditorHeight,
            MinHeight = EditorHeight,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(0),
            Tag = false
        });
        SetSwitchState(button, enabled: false, option);
        return button;
    }

    private static void SetSwitchState(WpfButton button, bool enabled, RuntimeLaunchOptionDefinition option)
    {
        button.Tag = enabled;
        button.Content = enabled ? "Enabled" : RuntimeDefaultLabel(option);
        VisualRole.SetButtonRole(button, enabled ? VisualRole.Primary : "");
    }

    private static string RuntimeDefaultLabel(RuntimeLaunchOptionDefinition option)
        => string.IsNullOrWhiteSpace(option.DefaultValue) ? "Runtime default" : $"Default: {option.DefaultValue}";

    private static string OptionToolTip(RuntimeLaunchOptionDefinition option)
    {
        var description = string.IsNullOrWhiteSpace(option.Description) ? option.Name : option.Description;
        return $"{description}{Environment.NewLine}{RuntimeDefaultLabel(option)}. Leave unchanged to inherit it.";
    }

    private static string SearchText(RuntimeLaunchOptionDefinition option)
        => $"{option.Name} {string.Join(" ", option.Aliases)} {option.ValueHint} {option.Description} {option.DefaultValue}";

    private static T CrispCompactControl<T>(T control) where T : System.Windows.Controls.Control
    {
        control.FontFamily = new WpfFontFamily("Segoe UI");
        control.FontSize = 12;
        control.FontWeight = FontWeights.Normal;
        control.SnapsToDevicePixels = true;
        control.UseLayoutRounding = true;
        TextOptions.SetTextFormattingMode(control, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(control, TextRenderingMode.ClearType);
        TextOptions.SetTextHintingMode(control, TextHintingMode.Fixed);
        return control;
    }

    private static WpfBrush ResourceBrush(string key) => (WpfBrush)WpfApplication.Current.Resources[key];

    private sealed record RuntimeOptionEditor(
        RuntimeLaunchOptionDefinition Option,
        FrameworkElement Control,
        FrameworkElement ValueControl)
    {
        public void AppendTokens(ICollection<string> tokens)
        {
            var value = Value();
            if (string.IsNullOrWhiteSpace(value)) return;
            tokens.Add(Option.Name);
            if (Option.ValueKind != RuntimeLaunchOptionValueKind.Switch) tokens.Add(value);
        }

        public string Value() => ValueControl switch
        {
            WpfButton button => button.Tag is true ? "on" : "",
            WpfComboBox combo => combo.SelectedIndex <= 0 ? "" : combo.SelectedItem?.ToString() ?? "",
            WpfTextBox textBox => textBox.Text.Trim(),
            _ => ""
        };

        public void Set(string value)
        {
            if (ValueControl is WpfButton button) SetSwitchState(button, enabled: true, Option);
            if (ValueControl is WpfTextBox textBox) textBox.Text = value;
            if (ValueControl is WpfComboBox combo)
                combo.SelectedItem = combo.Items.Cast<object>().Skip(1)
                    .FirstOrDefault(item => string.Equals(item.ToString(), value, StringComparison.OrdinalIgnoreCase))
                    ?? combo.Items[0];
        }

        public void Clear()
        {
            if (ValueControl is WpfButton button) SetSwitchState(button, enabled: false, Option);
            if (ValueControl is WpfTextBox textBox) textBox.Clear();
            if (ValueControl is WpfComboBox combo) combo.SelectedIndex = 0;
        }
    }

    private sealed record RuntimeOptionRow(FrameworkElement Row, string SearchText);
}
