using System.Windows;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;
using KeyboardTester.UI.Services;
using Res = KeyboardTester.UI.Resources;

namespace KeyboardTester.UI.Views;

/// <summary>
/// Диалог настроек: пороги диагностики и тема оформления.
/// Настройки сохраняются в %AppData%/KeyboardTester/settings.json через <see cref="SettingsService"/>.
/// </summary>
public partial class SettingsDialog : Window
{
    private readonly SettingsService _settingsService;
    private readonly IThemeService _themeService;

    /// <summary>
    /// Создаёт диалог настроек и заполняет поля текущими значениями.
    /// </summary>
    public SettingsDialog(SettingsService settingsService, IThemeService themeService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

        InitializeComponent();

        DebounceSettings debounce = settingsService.Current.Debounce;
        CriticalThresholdBox.Text = debounce.CriticalThresholdMs.ToString();
        WarningThresholdBox.Text = debounce.WarningThresholdMs.ToString();
        MildThresholdBox.Text = debounce.MildThresholdMs.ToString();
        StuckThresholdBox.Text = debounce.StuckKeyThresholdMs.ToString();
        MaxEventsBox.Text = debounce.MaxEventsPerKey.ToString();
        ThemeCombo.SelectedIndex = (int)settingsService.Current.Theme;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParsePositive(CriticalThresholdBox, out int critical) ||
            !TryParsePositive(WarningThresholdBox, out int warning) ||
            !TryParsePositive(MildThresholdBox, out int mild) ||
            !TryParsePositive(StuckThresholdBox, out int stuck) ||
            !TryParsePositive(MaxEventsBox, out int maxEvents))
        {
            MessageBox.Show(
                this,
                Res.Strings.InvalidSettingsPositive,
                Res.Strings.InvalidSettingsTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (critical >= warning || warning >= mild)
        {
            MessageBox.Show(
                this,
                Res.Strings.InvalidSettingsThresholdOrder,
                Res.Strings.InvalidSettingsTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var debounce = new DebounceSettings(critical, warning, mild, stuck, maxEvents);
        var theme = (AppTheme)Math.Clamp(ThemeCombo.SelectedIndex, 0, 2);

        _settingsService.Save(debounce, theme);
        _themeService.SetTheme(theme);

        DialogResult = true;
        Close();
    }

    private static bool TryParsePositive(System.Windows.Controls.TextBox box, out int value)
    {
        return int.TryParse(box.Text, out value) && value > 0;
    }
}
