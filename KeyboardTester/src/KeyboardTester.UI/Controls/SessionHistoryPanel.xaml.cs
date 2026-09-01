using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using KeyboardTester.Application.ViewModels;
using Res = KeyboardTester.UI.Resources;

namespace KeyboardTester.UI.Controls;

/// <summary>
/// Панель истории тестовых сессий (code-behind): подтверждение удаления сессии.
/// </summary>
public partial class SessionHistoryPanel : UserControl
{
    /// <summary>
    /// Создаёт панель истории сессий.
    /// </summary>
    public SessionHistoryPanel()
    {
        InitializeComponent();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.SelectedSession == null)
        {
            return;
        }

        MessageBoxResult result = MessageBox.Show(
            Window.GetWindow(this),
            string.Format(CultureInfo.CurrentCulture, Res.Strings.ConfirmDelete, vm.SelectedSession.DisplayName),
            Res.Strings.ConfirmDeleteTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes && vm.DeleteSessionCommand.CanExecute(null))
        {
            vm.DeleteSessionCommand.Execute(null);
        }
    }
}
