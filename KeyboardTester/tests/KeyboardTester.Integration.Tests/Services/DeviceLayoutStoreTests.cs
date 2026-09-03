using System.IO;
using FluentAssertions;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;
using KeyboardTester.UI.Services;
using Xunit;

namespace KeyboardTester.Integration.Tests.Services;

/// <summary>
/// Тесты хранилища привязок «устройство → раскладка» поверх
/// <see cref="SettingsService"/>: roundtrip, формат ключа,
/// регресс-защита merge при Save(debounce, theme), повреждённый файл.
/// </summary>
public class DeviceLayoutStoreTests : IDisposable
{
    private readonly string _tempDirectory;

    public DeviceLayoutStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "KeyboardTesterTests", Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveLayout_And_GetSavedLayout_Roundtrip()
    {
        var service = new SettingsService(_tempDirectory);

        service.SaveLayout("VID_046D&PID_C339", KeyboardLayout.Iso105);

        service.GetSavedLayout("VID_046D&PID_C339").Should().Be(KeyboardLayout.Iso105);
    }

    [Fact]
    public void SaveLayout_PersistsAcrossServiceInstances()
    {
        var first = new SettingsService(_tempDirectory);
        first.SaveLayout("VID_34EA&PID_0510", KeyboardLayout.Layout75);

        var second = new SettingsService(_tempDirectory);

        second.GetSavedLayout("VID_34EA&PID_0510").Should().Be(KeyboardLayout.Layout75, "привязка читается с диска");
    }

    [Fact]
    public void SaveLayout_OverwritesExistingBinding()
    {
        var service = new SettingsService(_tempDirectory);
        service.SaveLayout("VID_1234&PID_5678", KeyboardLayout.Ansi104);

        service.SaveLayout("VID_1234&PID_5678", KeyboardLayout.Tkl);

        service.GetSavedLayout("VID_1234&PID_5678").Should().Be(KeyboardLayout.Tkl);
    }

    [Fact]
    public void GetSavedLayout_UnknownKey_ReturnsNull()
    {
        var service = new SettingsService(_tempDirectory);

        service.GetSavedLayout("VID_DEAD&PID_BEEF").Should().BeNull();
    }

    [Fact]
    public void SaveLayout_DevicePathFallbackKey_Works()
    {
        var service = new SettingsService(_tempDirectory);
        const string laptopPath = @"\\?\ACPI#PNP0303#4&25e0b499&0";

        service.SaveLayout(laptopPath, KeyboardLayout.Layout60);

        service.GetSavedLayout(laptopPath).Should().Be(KeyboardLayout.Layout60);
    }

    [Fact]
    public void Save_DebounceAndTheme_DoesNotEraseDeviceLayouts()
    {
        // Регресс-защита v1.2.0: SettingsDialog вызывает Save(debounce, theme),
        // который раньше пересоздавал AppSettings и стирал привязки.
        var service = new SettingsService(_tempDirectory);
        service.SaveLayout("VID_046D&PID_C338", KeyboardLayout.Tkl);

        service.Save(new DebounceSettings(60, 40, 25, 900, 600), AppTheme.Dark);

        service.GetSavedLayout("VID_046D&PID_C338").Should().Be(KeyboardLayout.Tkl, "Save не должен стирать DeviceLayouts");
        service.Current.DeviceLayouts.Should().HaveCount(1);
    }

    [Fact]
    public void Load_CorruptedJson_FallsBackToDefaults()
    {
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(Path.Combine(_tempDirectory, "settings.json"), "{ this is not valid json");

        var service = new SettingsService(_tempDirectory);

        service.Current.DeviceLayouts.Should().BeEmpty();
        service.Current.Debounce.CriticalThresholdMs.Should().BeGreaterThan(0);
        service.GetSavedLayout("VID_046D&PID_C338").Should().BeNull();
    }

    [Fact]
    public void SavedFile_ContainsHumanReadableEnumAndKeyFormat()
    {
        var service = new SettingsService(_tempDirectory);
        service.SaveLayout("VID_046D&PID_C339", KeyboardLayout.Iso105);

        string json = File.ReadAllText(service.FilePath);

        json.Should().Contain("\"VID_046D&PID_C339\"");
        json.Should().Contain("\"Iso105\"", "enum сериализуется строкой (JsonStringEnumConverter)");
    }
}
