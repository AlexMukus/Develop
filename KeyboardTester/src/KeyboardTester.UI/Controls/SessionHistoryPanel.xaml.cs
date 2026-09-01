using System.Windows;
using System.Windows.Controls;
using KeyboardTester.Application.ViewModels;

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
            $"Удалить сессию «{vm.SelectedSession.DisplayName}»?",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes && vm.DeleteSessionCommand.CanExecute(null))
        {
            vm.DeleteSessionCommand.Execute(null);
        }
    }
}
