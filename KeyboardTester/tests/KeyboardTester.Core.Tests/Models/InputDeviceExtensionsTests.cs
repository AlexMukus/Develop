using FluentAssertions;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;
using Xunit;

namespace KeyboardTester.Core.Tests.Models;

/// <summary>
/// Тесты хелпера ключа привязки «устройство → раскладка» (план v1.2.0).
/// </summary>
public class InputDeviceExtensionsTests
{
    [Fact]
    public void GetLayoutBindingKey_UsbDevice_ReturnsVidPidKey()
    {
        var device = new InputDevice(
            DevicePath: @"\\?\hid#vid_046d&pid_c338#6&1a2b3c4d&0&0000#{884b96c3-56ef-11d1-bc8c-00a0c91405dd}",
            ProductName: "G Pro X",
            Manufacturer: "Logitech",
            VendorId: 0x046D,
            ProductId: 0xC338,
            ConnectionType: KeyboardConnectionType.Wired);

        string key = device.GetLayoutBindingKey();

        key.Should().Be("VID_046D&PID_C338");
    }

    [Fact]
    public void GetLayoutBindingKey_LaptopWithoutVidPid_ReturnsDevicePath()
    {
        var device = new InputDevice(
            DevicePath: @"\\?\ACPI#PNP0303#4&25e0b499&0",
            ProductName: null,
            Manufacturer: null,
            VendorId: 0,
            ProductId: 0,
            ConnectionType: KeyboardConnectionType.Laptop);

        string key = device.GetLayoutBindingKey();

        key.Should().Be(device.DevicePath, "fallback для устройств без VID/PID — полный путь");
    }

    [Fact]
    public void GetLayoutBindingKey_ZeroVendorOnly_ReturnsDevicePath()
    {
        var device = new InputDevice(
            DevicePath: @"\\?\hid#some_device",
            ProductName: null,
            Manufacturer: null,
            VendorId: 0,
            ProductId: 0xC338);

        string key = device.GetLayoutBindingKey();

        key.Should().Be(device.DevicePath, "ключ VID/PID требует обоих ненулевых идентификаторов");
    }

    [Fact]
    public void GetLayoutBindingKey_NullDevice_Throws()
    {
        Action act = () => ((InputDevice)null!).GetLayoutBindingKey();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetLayoutBindingKey_LowercaseHexDigits_FormatsAsFourDigits()
    {
        var device = new InputDevice(
            DevicePath: @"\\?\hid#vid_34ea&pid_0503#0",
            ProductName: null,
            Manufacturer: null,
            VendorId: 0x34EA,
            ProductId: 0x0503);

        string key = device.GetLayoutBindingKey();

        key.Should().Be("VID_34EA&PID_0503");
    }
}
