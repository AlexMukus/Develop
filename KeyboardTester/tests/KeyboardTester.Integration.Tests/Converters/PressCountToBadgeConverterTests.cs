using System.Globalization;
using FluentAssertions;
using KeyboardTester.UI.Converters;
using Xunit;

namespace KeyboardTester.Integration.Tests.Converters;

/// <summary>
/// Тесты конвертера счётчика нажатий в короткий бейдж на клавише
/// виртуальной клавиатуры (v1.1.0, пункт 5): максимум 4 символа.
/// </summary>
public class PressCountToBadgeConverterTests
{
    private readonly PressCountToBadgeConverter _converter = new();

    [Theory]
    [InlineData(0, "")]
    [InlineData(-5, "")]
    public void Convert_NonPositive_ReturnsEmpty(int count, string expected)
    {
        _converter.Convert(count, typeof(string), null, CultureInfo.CurrentCulture)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "1")]
    [InlineData(42, "42")]
    [InlineData(999, "999")]
    [InlineData(9999, "9999")]
    public void Convert_UpTo9999_ReturnsNumberAsIs(int count, string expected)
    {
        _converter.Convert(count, typeof(string), null, CultureInfo.CurrentCulture)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(10_000, "10k+")]
    [InlineData(15_500, "15k+")]
    [InlineData(999_999, "99k+")]
    [InlineData(123_456_789, "99k+")]
    public void Convert_TenThousandAndMore_ReturnsThousandsBadge(int count, string expected)
    {
        _converter.Convert(count, typeof(string), null, CultureInfo.CurrentCulture)
            .Should().Be(expected);
    }

    [Fact]
    public void Convert_ResultNeverExceedsFourCharacters()
    {
        foreach (int count in new[] { 0, 7, 123, 9999, 10_000, 5_000_000 })
        {
            string badge = (string)_converter.Convert(count, typeof(string), null, CultureInfo.CurrentCulture);
            badge.Length.Should().BeLessThanOrEqualTo(4, $"для счётчика {count}");
        }
    }

    [Fact]
    public void Convert_NotAnInt_ReturnsEmpty()
    {
        _converter.Convert("42", typeof(string), null, CultureInfo.CurrentCulture)
            .Should().Be(string.Empty);
        _converter.Convert(null, typeof(string), null, CultureInfo.CurrentCulture)
            .Should().Be(string.Empty);
    }
}
