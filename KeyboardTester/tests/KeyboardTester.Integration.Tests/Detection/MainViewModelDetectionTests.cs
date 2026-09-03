using System.IO;
using FluentAssertions;
using KeyboardTester.Application.Services;
using KeyboardTester.Application.ViewModels;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;
using KeyboardTester.Infrastructure.Analysis;
using KeyboardTester.Infrastructure.Layouts;
using KeyboardTester.Infrastructure.Storage;
using KeyboardTester.Integration.Tests.Helpers;
using KeyboardTester.UI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace KeyboardTester.Integration.Tests.Detection;

/// <summary>
/// Интеграционные тесты связки MainViewModel + KeyboardDetectionService
/// через FakeRawInputCapture: подключение неизвестного устройства → баннер,
/// маркерные нажатия → предложение, подтверждение → привязка в сторе,
/// повторное подключение → автоприменение без визарда.
/// </summary>
public class MainViewModelDetectionTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly FakeRawInputCapture _capture = new();
    private readonly SettingsService _layoutStore;
    private readonly MainViewModel _viewModel;

    public MainViewModelDetectionTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "kt-detect-" + Guid.NewGuid().ToString("N"));
        _layoutStore = new SettingsService(_tempDirectory);

        var layoutProvider = new LayoutProvider();
        IStatisticsEngine statisticsEngine = new StatisticsEngine(
            new DebounceAnalyzer(), new DebounceSettings(), layoutProvider);
        var detectionService = new KeyboardDetectionService(
            new LayoutHeuristics(), NullLogger<KeyboardDetectionService>.Instance);
        using var sessionService = new TestSessionService(statisticsEngine, NullLogger<TestSessionService>.Instance);

        _viewModel = new MainViewModel(
            _capture,
            statisticsEngine,
            new GhostingTestEngine(_capture, layoutProvider),
            new SessionHistoryService(_tempDirectory),
            layoutProvider,
            Mock.Of<IThemeService>(),
            sessionService,
            detectionService,
            new KeyboardCatalog(),
            _layoutStore,
            NullLogger<MainViewModel>.Instance);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
        _capture.Dispose();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static InputDevice UnknownDevice => new(
        @"\\?\hid#vid_7777&pid_8888#0",
        "Unknown Mech",
        "Unknown",
        0x7777,
        0x8888,
        KeyboardConnectionType.Wired);

    private static InputDevice KnownDevice => new(
        @"\\?\hid#vid_046d&pid_c338#0",
        "G Pro X",
        "Logitech",
        0x046D,
        0xC338,
        KeyboardConnectionType.Wired);

    private void Press(uint scanCode, string devicePath, long timestamp = 0)
    {
        _capture.Press(scanCode, timestamp, devicePath);
    }

    [Fact]
    public void UnknownDeviceConnected_DetectionBannerShown()
    {
        _capture.RaiseDeviceConnected(UnknownDevice);

        _viewModel.IsDetectionActive.Should().BeTrue("промах каталога и сторa запускает визард");
        _viewModel.DetectionWaitingNumpadEnter.Should().BeTrue();
        _viewModel.SelectedKeyboard.Should().Be(UnknownDevice, "подключённое устройство становится выбранным");
    }

    [Fact]
    public void MarkerPresses_LeadToProposalWithHeuristic()
    {
        _capture.RaiseDeviceConnected(UnknownDevice);

        Press(0xE01C, UnknownDevice.DevicePath); // Numpad Enter
        Press(0x56, UnknownDevice.DevicePath);   // ISO-сосед Shift

        _viewModel.IsProposalRequired.Should().BeTrue();
        _viewModel.SuggestedLayout.Should().Be(KeyboardLayout.Iso105);
        _viewModel.ProposedLayout.Should().Be(KeyboardLayout.Iso105);
        _viewModel.IsDetectionActive.Should().BeTrue("диалог открыт, визард активен");
    }

    [Fact]
    public void ApplyProposedLayout_WithRemember_SavesBinding()
    {
        _capture.RaiseDeviceConnected(UnknownDevice);
        Press(0xE01C, UnknownDevice.DevicePath);
        Press(0x2C, UnknownDevice.DevicePath); // ANSI-сосед
        _viewModel.ProposedLayout = KeyboardLayout.Ansi104;

        _viewModel.ApplyProposedLayoutCommand.Execute(true);

        _viewModel.SelectedLayout.Should().Be(KeyboardLayout.Ansi104);
        _viewModel.IsDetectionActive.Should().BeFalse();
        _layoutStore.GetSavedLayout("VID_7777&PID_8888").Should().Be(KeyboardLayout.Ansi104);
    }

    [Fact]
    public void Reconnect_AfterRememberedBinding_AppliesSilentlyWithoutWizard()
    {
        _layoutStore.SaveLayout("VID_7777&PID_8888", KeyboardLayout.Layout75);

        _capture.RaiseDeviceConnected(UnknownDevice);

        _viewModel.SelectedLayout.Should().Be(KeyboardLayout.Layout75, "сохранённая привязка применяется молча");
        _viewModel.IsDetectionActive.Should().BeFalse("визард не запускается при наличии привязки");
    }

    [Fact]
    public void KnownDeviceConnected_AppliesCatalogLayoutSilently()
    {
        _capture.RaiseDeviceConnected(KnownDevice);

        _viewModel.SelectedLayout.Should().Be(KeyboardLayout.Tkl, "Logitech G Pro X из каталога — TKL");
        _viewModel.IsDetectionActive.Should().BeFalse();
        _viewModel.DetectedKeyboardName.Should().Be("Logitech G Pro X");
        _layoutStore.GetSavedLayout("VID_046D&PID_C338").Should().Be(KeyboardLayout.Tkl, "привязка каталога сохраняется");
    }

    [Fact]
    public void ManualLayoutChange_UpdatesBinding()
    {
        _capture.RaiseDeviceConnected(KnownDevice); // каталог применил TKL

        _viewModel.SelectedLayout = KeyboardLayout.Iso105; // ручная смена

        _layoutStore.GetSavedLayout("VID_046D&PID_C338").Should().Be(KeyboardLayout.Iso105, "ручная смена обновляет привязку");
    }

    [Fact]
    public void CancelDetection_ClosesBannerAndKeepsCurrentLayout()
    {
        _capture.RaiseDeviceConnected(UnknownDevice);

        _viewModel.CancelDetectionCommand.Execute(null);

        _viewModel.IsDetectionActive.Should().BeFalse();
        _viewModel.SelectedLayout.Should().Be(KeyboardLayout.Ansi104, "раскладка не менялась");
        _layoutStore.GetSavedLayout("VID_7777&PID_8888").Should().BeNull("привязка не сохранена при отмене");
    }

    [Fact]
    public void ForeignDeviceKeyPresses_DoNotAdvanceWizard()
    {
        _capture.RaiseDeviceConnected(UnknownDevice);

        Press(0xE01C, @"\\?\hid#vid_9999&pid_aaaa#0"); // чужое устройство

        _viewModel.DetectionWaitingNumpadEnter.Should().BeTrue("нажатия чужих устройств не двигают визард");
    }
}
