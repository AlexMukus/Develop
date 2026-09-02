using System.Globalization;
using System.Windows;
using KeyboardTester.UI.Services;
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
        VersionText.Text = string.Format(CultureInfo.CurrentCulture, Res.Strings.Version, AppVersion.Current);
    }
}
