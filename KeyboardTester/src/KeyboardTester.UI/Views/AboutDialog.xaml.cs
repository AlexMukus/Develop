using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace KeyboardTester.UI.Views;

/// <summary>
/// Диалог «О программе»: версия, описание, ссылки на репозиторий и обновления.
/// </summary>
public partial class AboutDialog : Window
{
    private const string RepositoryUrl = "https://github.com/yourusername/KeyboardTester";
    private const string ReleasesUrl = RepositoryUrl + "/releases";

    /// <summary>
    /// Создаёт диалог «О программе».
    /// </summary>
    public AboutDialog()
    {
        InitializeComponent();
        VersionText.Text = $"Версия: {GetVersion()}";
    }

    private void GitHubLink_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl(RepositoryUrl);
    }

    private void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl(ReleasesUrl);
    }

    private static string GetVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "неизвестна";
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            MessageBox.Show(
                $"Не удалось открыть ссылку:\n{url}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
