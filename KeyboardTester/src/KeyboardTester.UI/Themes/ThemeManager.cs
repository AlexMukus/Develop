using System.Windows;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Interfaces;
using Microsoft.Win32;

namespace KeyboardTester.UI.Themes;

/// <summary>
/// Управление темами оформления: подмена словаря ресурсов приложения
/// и определение системной темы Windows через реестр.
/// </summary>
public sealed class ThemeManager : IThemeService
{
    private const string DarkThemeUri = "/KeyboardTester.UI;component/Themes/Dark.xaml";
    private const string LightThemeUri = "/KeyboardTester.UI;component/Themes/Light.xaml";

    private bool _isApplied;

    /// <inheritdoc />
    public event EventHandler? ThemeChanged;

    /// <inheritdoc />
    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    /// <inheritdoc />
    public void SetTheme(AppTheme theme)
    {
        if (_isApplied && CurrentTheme == theme)
        {
            return;
        }

        CurrentTheme = theme;
        ApplyTheme(theme);
        _isApplied = true;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public AppTheme GetSystemTheme()
    {
        // HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme
        // 0 = тёмная, 1 = светлая.
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            object? value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 1 ? AppTheme.Light : AppTheme.Dark;
        }
        catch (Exception)
        {
            return AppTheme.Dark;
        }
    }

    private void ApplyTheme(AppTheme theme)
    {
        AppTheme actualTheme = theme == AppTheme.System ? GetSystemTheme() : theme;
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(actualTheme == AppTheme.Dark ? DarkThemeUri : LightThemeUri, UriKind.Relative),
        };

        System.Windows.Application application = System.Windows.Application.Current;
        ResourceDictionary? oldDictionary = application.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Themes/", StringComparison.OrdinalIgnoreCase) == true);
        if (oldDictionary != null)
        {
            application.Resources.MergedDictionaries.Remove(oldDictionary);
        }

        application.Resources.MergedDictionaries.Add(dictionary);
    }
}
