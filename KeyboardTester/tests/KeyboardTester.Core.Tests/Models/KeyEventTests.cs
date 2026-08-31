using FluentAssertions;
using KeyboardTester.Core.Models;
using Xunit;

namespace KeyboardTester.Core.Tests.Models;

public class KeyEventTests
{
    [Fact]
    public void KeyEvent_Initialization_PreservesValues()
    {
        var keyEvent = new KeyEvent(
            Id: Guid.NewGuid(),
            VirtualKeyCode: 0x41,
            ScanCode: 0x1E,
            KeyName: "A",
            TimestampMicroseconds: 123_456_789,
            IsKeyDown: true,
            DevicePath: "HID\\VID_1234&PID_5678");

        keyEvent.VirtualKeyCode.Should().Be(0x41);
        keyEvent.ScanCode.Should().Be(0x1E);
        keyEvent.KeyName.Should().Be("A");
        keyEvent.TimestampMicroseconds.Should().Be(123_456_789);
        keyEvent.IsKeyDown.Should().BeTrue();
        keyEvent.DevicePath.Should().Be("HID\\VID_1234&PID_5678");
    }

    [Fact]
    public void GetDurationMicroseconds_WithReleaseEvent_ReturnsDifference()
    {
        var down = new KeyEvent(Guid.NewGuid(), 0x41, 0x1E, "A", 100, true, null);
        var up = new KeyEvent(Guid.NewGuid(), 0x41, 0x1E, "A", 550, false, null);

        down.GetDurationMicroseconds(up).Should().Be(450);
    }

    [Fact]
    public void GetDurationMicroseconds_WithKeyDownEvent_ThrowsArgumentException()
    {
        var down = new KeyEvent(Guid.NewGuid(), 0x41, 0x1E, "A", 100, true, null);
        var wrong = new KeyEvent(Guid.NewGuid(), 0x41, 0x1E, "A", 200, true, null);

        var act = () => down.GetDurationMicroseconds(wrong);
        act.Should().Throw<ArgumentException>();
    }
}
