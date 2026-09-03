using System.Windows;
using KeyboardTester.Application.ViewModels;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.UI.Services;

namespace KeyboardTester.UI.Views;

/// <summary>
/// Главное окно приложения.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly SettingsService _settingsService;
    private readonly IThemeService _themeService;

    /// <summary>
    /// Создаёт главное окно с указанной моделью представления и сервисами.
    /// </summary>
    public MainWindow(MainViewModel viewModel, SettingsService settingsService, IThemeService themeService)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

        InitializeComponent();

        DataContext = _viewModel;

        // Версия подтягивается из атрибутов сборки (Directory.Build.props, v1.1.0).
        VersionText.Text = $"v{AppVersion.Current}";

        _viewModel.OpenSettingsRequested += OnOpenSettingsRequested;
        _viewModel.OpenAboutRequested += OnOpenAboutRequested;
        _viewModel.ProposalRequested += OnProposalRequested;
        Closed += OnClosed;
    }

    private void OnOpenSettingsRequested(object? sender, EventArgs e)
    {
        var dialog = new SettingsDialog(_settingsService, _themeService)
        {
            Owner = this,
        };
        dialog.ShowDialog();
    }

    private void OnOpenAboutRequested(object? sender, EventArgs e)
    {
        var dialog = new AboutDialog
        {
            Owner = this,
        };
        dialog.ShowDialog();
    }

    private void OnProposalRequested(object? sender, EventArgs e)
    {
        // Диалог модальный: результат применяется командами ViewModel
        // (ApplyProposedLayout/CancelProposal), окно лишь хостит его.
        var dialog = new LayoutProposalDialog(_viewModel)
        {
            Owner = this,
        };

        if (dialog.ShowDialog() != true)
        {
            _viewModel.CancelProposalCommand.Execute(null);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.OpenSettingsRequested -= OnOpenSettingsRequested;
        _viewModel.OpenAboutRequested -= OnOpenAboutRequested;
        _viewModel.ProposalRequested -= OnProposalRequested;
        _viewModel.Dispose();
    }
}
