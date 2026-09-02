using System.Globalization;
using System.Reflection;
using System.Windows;
using Res = KeyboardTester.UI.Resources;

namespace KeyboardTester.UI.Views;

/// <summary>
/// Диалог «О программе»: версия, описание и сведения о лицензии.
/// </summary>
public partial class AboutDialog : Window
{
    /// <summary>
    /// Создаёт диалог «О программе».
    /// </summary>
    public AboutDialog()
    {
        InitializeComponent();
        VersionText.Text = string.Format(CultureInfo.CurrentCulture, Res.Strings.Version, GetVersion());
    }

    private static string GetVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? Res.Strings.VersionUnknown;
    }
}
