using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.Views;
using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public class ThemePreferencesViewModel : PropertyChangedBaseWithValidation
{
    private static readonly AppSettings DefaultSettings = AppSettings.GetDefaultAppSettings();
    private readonly AppSettings appSettings;
    private readonly Logger logger;
    private RadWindow? parentWindow;

    public ThemePreferencesViewModel(AppSettings appSettings, Logger logger)
    {
        this.appSettings = appSettings;
        this.logger = logger;

        Theme = appSettings.Design.AppTheme;
        FontSize = Math.Clamp(appSettings.Design.FontSize, FontSizeOptions[0], FontSizeOptions[^1]);
        ColorSettings =
        [
            new ColorPreferenceViewModel("LightThemeTextColor", appSettings.Design.LightThemeTextColor, DefaultSettings.Design.LightThemeTextColor),
            new ColorPreferenceViewModel("LightThemeTextBoxBackgroundColor", appSettings.Design.LightThemeTextBoxBackgroundColor, DefaultSettings.Design.LightThemeTextBoxBackgroundColor),
            new ColorPreferenceViewModel("LightThemeControlBackgroundColor", appSettings.Design.LightThemeControlBackgroundColor, DefaultSettings.Design.LightThemeControlBackgroundColor),
            new ColorPreferenceViewModel("DarkThemeTextColor", appSettings.Design.DarkThemeTextColor, DefaultSettings.Design.DarkThemeTextColor),
            new ColorPreferenceViewModel("DarkThemeTextBoxBackgroundColor", appSettings.Design.DarkThemeTextBoxBackgroundColor, DefaultSettings.Design.DarkThemeTextBoxBackgroundColor),
            new ColorPreferenceViewModel("DarkThemeControlBackgroundColor", appSettings.Design.DarkThemeControlBackgroundColor, DefaultSettings.Design.DarkThemeControlBackgroundColor)
        ];
        ApplyCommand = new RelayCommand<object>(_ => ApplySettings());
        OkCommand = new RelayCommand<object>(_ =>
        {
            if (ApplySettings())
            {
                parentWindow?.Close();
            }
        });
        CancelCommand = new RelayCommand<object>(_ => parentWindow?.Close());
    }

    public IReadOnlyList<ApplicationTheme> ThemeOptions { get; } = Enum.GetValues<ApplicationTheme>();

    public ApplicationTheme Theme
    {
        get;
        set { field = value; OnPropertyChanged(); }
    }

    // The UI layouts (ribbon, log columns, ...) are tuned for this range; outside it the
    // fixed chrome around the text stops fitting, so the choice is a closed list.
    public IReadOnlyList<int> FontSizeOptions { get; } = Enumerable.Range(10, 11).ToList();

    public int FontSize
    {
        get;
        set { field = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ColorPreferenceViewModel> ColorSettings { get; }

    public string ErrorMessage
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    } = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public RelayCommand<object> ApplyCommand { get; }
    public RelayCommand<object> OkCommand { get; }
    public RelayCommand<object> CancelCommand { get; }

    public void SetParentWindow(RadWindow window)
    {
        parentWindow = window;
    }

    private bool ApplySettings()
    {
        try
        {
            appSettings.Design.AppTheme = Theme;
            appSettings.Design.FontSize = Math.Clamp(FontSize, FontSizeOptions[0], FontSizeOptions[^1]);
            appSettings.Design.LightThemeTextColor = GetColor("LightThemeTextColor");
            appSettings.Design.LightThemeTextBoxBackgroundColor = GetColor("LightThemeTextBoxBackgroundColor");
            appSettings.Design.LightThemeControlBackgroundColor = GetColor("LightThemeControlBackgroundColor");
            appSettings.Design.DarkThemeTextColor = GetColor("DarkThemeTextColor");
            appSettings.Design.DarkThemeTextBoxBackgroundColor = GetColor("DarkThemeTextBoxBackgroundColor");
            appSettings.Design.DarkThemeControlBackgroundColor = GetColor("DarkThemeControlBackgroundColor");

            AppSettings.SaveAppSettingsToFile(AppDataPaths.AppSettingsFile, appSettings);
            ShellWindow.ApplyDesignSettings();
            ErrorMessage = string.Empty;
            logger.Log(LogLevel.Info, "Application preferences saved.");
            return true;
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
            logger.Log(LogLevel.Error, $"Save application preferences failed: {e.Message}", e);
            return false;
        }
    }

    private Color GetColor(string name)
    {
        var colorSetting = ColorSettings.First(setting => setting.Name == name);
        return colorSetting.GetColor();
    }

    private static byte ParseColorComponent(string value, string propertyName)
    {
        if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var component))
        {
            return component;
        }

        throw new InvalidOperationException($"{propertyName} contains invalid color component \"{value}\".");
    }
}

public class ColorPreferenceViewModel : PropertyChangedBase
{
    private readonly Color defaultColor;
    private bool isUpdatingColor;
    private Color selectedColor;

    public ColorPreferenceViewModel(string name, Color color, Color defaultColor)
    {
        Name = name;
        this.defaultColor = defaultColor;
        SetColor(color);
        ResetDefaultCommand = new RelayCommand<object>(_ => SetColor(this.defaultColor));
    }

    public string Name { get; }
    public string DisplayName => Name switch
    {
        "LightThemeTextColor" => "Light Theme Text Color",
        "LightThemeTextBoxBackgroundColor" => "Light Theme Text Box Background Color",
        "LightThemeControlBackgroundColor" => "Light Theme Control Background Color",
        "DarkThemeTextColor" => "Dark Theme Text Color",
        "DarkThemeTextBoxBackgroundColor" => "Dark Theme Text Box Background Color",
        "DarkThemeControlBackgroundColor" => "Dark Theme Control Background Color",
        _ => Name
    };

    public string Red
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            if (!isUpdatingColor)
            {
                RefreshSelectedColorFromComponents();
            }
        }
    } = string.Empty;

    public string Green
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            if (!isUpdatingColor)
            {
                RefreshSelectedColorFromComponents();
            }
        }
    } = string.Empty;

    public string Blue
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            if (!isUpdatingColor)
            {
                RefreshSelectedColorFromComponents();
            }
        }
    } = string.Empty;

    public Color SelectedColor
    {
        get => selectedColor;
        set
        {
            if (selectedColor == value)
            {
                return;
            }

            selectedColor = value;
            isUpdatingColor = true;
            try
            {
                Red = value.R.ToString(CultureInfo.InvariantCulture);
                Green = value.G.ToString(CultureInfo.InvariantCulture);
                Blue = value.B.ToString(CultureInfo.InvariantCulture);
            }
            finally
            {
                isUpdatingColor = false;
            }

            OnPropertyChanged();
        }
    }

    public RelayCommand<object> ResetDefaultCommand { get; }

    public Color GetColor()
    {
        return Color.FromRgb(
            ParseColorComponent(Red, Name),
            ParseColorComponent(Green, Name),
            ParseColorComponent(Blue, Name));
    }

    private void SetColor(Color color)
    {
        SelectedColor = color;
        Red = color.R.ToString(CultureInfo.InvariantCulture);
        Green = color.G.ToString(CultureInfo.InvariantCulture);
        Blue = color.B.ToString(CultureInfo.InvariantCulture);
    }

    private void RefreshSelectedColorFromComponents()
    {
        if (!TryParseColorComponent(Red, out var red)
            || !TryParseColorComponent(Green, out var green)
            || !TryParseColorComponent(Blue, out var blue))
        {
            return;
        }

        var color = Color.FromRgb(red, green, blue);
        if (SelectedColor == color)
        {
            return;
        }

        selectedColor = color;
        OnPropertyChanged(nameof(SelectedColor));
    }

    private static bool TryParseColorComponent(string value, out byte component)
    {
        return byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out component);
    }

    private static byte ParseColorComponent(string value, string propertyName)
    {
        if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var component))
        {
            return component;
        }

        throw new InvalidOperationException($"{propertyName} contains invalid color component \"{value}\".");
    }
}
