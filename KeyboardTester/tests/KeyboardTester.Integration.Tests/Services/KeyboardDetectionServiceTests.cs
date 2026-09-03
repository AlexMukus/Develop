using FluentAssertions;
using KeyboardTester.Application.Services;
using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;
using KeyboardTester.Infrastructure.Layouts;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KeyboardTester.Integration.Tests.Services;

/// <summary>
/// Тесты конечного автомата <see cref="KeyboardDetectionService"/>:
/// переходы состояний, фильтр по DevicePath, «Нет numpad», отмена, Confirm.
/// </summary>
public class KeyboardDetectionServiceTests
{
    private readonly KeyboardDetectionService _service = new(
        new LayoutHeuristics(),
        NullLogger<KeyboardDetectionService>.Instance);

    private static InputDevice CreateDevice(string path = @"\\?\hid#vid_1234&pid_5678#0", uint vid = 0x1234, uint pid = 0x5678)
    {
        return new InputDevice(path, "Test Keyboard", "Test", vid, pid, KeyboardConnectionType.Wired);
    }

    private static RawKeyEventArgs CreateKey(uint scanCode, string devicePath)
    {
        return new RawKeyEventArgs
        {
            VirtualKeyCode = 0,
            ScanCode = scanCode,
            KeyName = $"SC{scanCode:X}",
            TimestampMicroseconds = 0,
            DevicePath = devicePath,
        };
    }

    [Fact]
    public void Start_InitialState_WaitingNumpadEnter()
    {
        _service.Start(CreateDevice());

        _service.State.Should().Be(KeyboardDetectionState.WaitingNumpadEnter);
        _service.IsActive.Should().BeTrue();
        _service.Target.Should().NotBeNull();
    }

    [Fact]
    public void HandleKeyPress_NumpadEnter_TransitionsToWaitingLeftShift()
    {
        InputDevice device = CreateDevice();
        _service.Start(device);

        _service.HandleKeyPress(CreateKey(0xE01C, device.DevicePath));

        _service.State.Should().Be(KeyboardDetectionState.WaitingLeftShift);
    }

    [Fact]
    public void HandleKeyPress_LeftShiftNeighborAfterNumpad_TransitionsToProposal()
    {
        InputDevice device = CreateDevice();
        _service.Start(device);
        _service.HandleKeyPress(CreateKey(0xE01C, device.DevicePath));

        _service.HandleKeyPress(CreateKey(0x2C, device.DevicePath));

        _service.State.Should().Be(KeyboardDetectionState.Proposal);
        _service.SuggestedLayout.Should().Be(KeyboardLayout.Ansi104);
    }

    [Fact]
    public void HandleKeyPress_IsoNeighborAfterNumpad_ProposesIso105()
    {
        InputDevice device = CreateDevice();
        _service.Start(device);
        _service.HandleKeyPress(CreateKey(0xE01C, device.DevicePath));

        _service.HandleKeyPress(CreateKey(0x56, device.DevicePath));

        _service.SuggestedLayout.Should().Be(KeyboardLayout.Iso105);
    }

    [Fact]
    public void HandleKeyPress_ForeignDevicePath_Ignored()
    {
        InputDevice device = CreateDevice();
        _service.Start(device);

        _service.HandleKeyPress(CreateKey(0xE01C, @"\\?\hid#other_device"));

        _service.State.Should().Be(KeyboardDetectionState.WaitingNumpadEnter, "нажатия чужих устройств игнорируются");
    }

    [Fact]
    public void HandleKeyPress_NonMarkerKey_Ignored()
    {
        InputDevice device = CreateDevice();
        _service.Start(device);

        _service.HandleKeyPress(CreateKey(0x1E, device.DevicePath)); // A
        _service.HandleKeyPress(CreateKey(0x39, device.DevicePath)); // Space

        _service.State.Should().Be(KeyboardDetectionState.WaitingNumpadEnter);
    }

    [Fact]
    public void MarkNumpadAbsent_TransitionsToWaitingLeftShift()
    {
        _service.Start(CreateDevice());

        _service.MarkNumpadAbsent();

        _service.State.Should().Be(KeyboardDetectionState.WaitingLeftShift);
    }

    [Fact]
    public void MarkNumpadAbsent_ThenAnsiNeighbor_ProposalWithoutSuggestion()
    {
        InputDevice device = CreateDevice();
        _service.Start(device);
        _service.MarkNumpadAbsent();

        _service.HandleKeyPress(CreateKey(0x2C, device.DevicePath));

        _service.State.Should().Be(KeyboardDetectionState.Proposal);
        _service.SuggestedLayout.Should().BeNull("без numpad форм-фактор неоднозначен — ручной выбор");
    }

    [Fact]
    public void Cancel_ReturnsToIdle()
    {
        _service.Start(CreateDevice());
        _service.HandleKeyPress(CreateKey(0xE01C, CreateDevice().DevicePath));

        _service.Cancel();

        _service.State.Should().Be(KeyboardDetectionState.Idle);
        _service.IsActive.Should().BeFalse();
        _service.Target.Should().BeNull();
    }

    [Fact]
    public void Cancel_FromIdle_NoStateChangeEvent()
    {
        bool fired = false;
        _service.StateChanged += (_, _) => fired = true;

        _service.Cancel();

        fired.Should().BeFalse("отмена неактивной детекции — no-op");
    }

    [Fact]
    public void Confirm_ReturnsToIdleAndClearsTarget()
    {
        _service.Start(CreateDevice());

        _service.Confirm(KeyboardLayout.Tkl);

        _service.State.Should().Be(KeyboardDetectionState.Idle);
        _service.Target.Should().BeNull();
        _service.SuggestedLayout.Should().BeNull();
    }

    [Fact]
    public void StateChanged_FiredOnEveryTransition()
    {
        int fired = 0;
        _service.StateChanged += (_, _) => fired++;
        InputDevice device = CreateDevice();

        _service.Start(device);                          // → WaitingNumpadEnter
        _service.HandleKeyPress(CreateKey(0xE01C, device.DevicePath)); // → WaitingLeftShift
        _service.HandleKeyPress(CreateKey(0x56, device.DevicePath));   // → Proposal
        _service.Confirm(KeyboardLayout.Iso105);         // → Idle

        fired.Should().Be(4);
    }

    [Fact]
    public void Start_RestartResetsMarkers()
    {
        InputDevice device = CreateDevice();
        _service.Start(device);
        _service.HandleKeyPress(CreateKey(0xE01C, device.DevicePath));
        _service.Cancel();

        _service.Start(device);

        _service.State.Should().Be(KeyboardDetectionState.WaitingNumpadEnter, "маркеры сброшены при перезапуске");
    }
}
