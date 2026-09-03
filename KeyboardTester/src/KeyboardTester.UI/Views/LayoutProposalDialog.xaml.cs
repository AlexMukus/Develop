using System.Windows;
using KeyboardTester.Application.ViewModels;
using KeyboardTester.Core.Models;
using KeyboardTester.UI.Converters;
using Res = KeyboardTester.UI.Resources;

namespace KeyboardTester.UI.Views;

/// <summary>
/// Диалог предложения раскладки по итогам маркерной эвристики:
/// комбобокс всех раскладок (предварительно выбрана предложенная),
/// чекбокс «Запомнить для этой клавиатуры», ОК/Отмена.
/// </summary>
public partial class LayoutProposalDialog : Window
{
    private readonly MainViewModel _viewModel;

    /// <summary>
    /// Создаёт диалог предложения раскладки для указанной ViewModel.
    /// </summary>
    public LayoutProposalDialog(MainViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();

        LayoutCombo.ItemsSource = _viewModel.AvailableLayouts;
        LayoutCombo.SelectedItem = _viewModel.ProposedLayout;
        RememberCheckBox.IsChecked = _viewModel.RememberDeviceLayout;

        SuggestedText.Text = _viewModel.SuggestedLayout is KeyboardLayout suggested
            ? string.Format(Res.Strings.LayoutProposalSuggested, KeyboardLayoutToDescriptionConverter.Describe(suggested))
            : Res.Strings.LayoutProposalAmbiguous;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (LayoutCombo.SelectedItem is not KeyboardLayout layout)
        {
            return;
        }

        bool remember = RememberCheckBox.IsChecked == true;
        _viewModel.ApplyProposedLayoutCommand.Execute(remember);
        DialogResult = true;
        Close();
    }
}
