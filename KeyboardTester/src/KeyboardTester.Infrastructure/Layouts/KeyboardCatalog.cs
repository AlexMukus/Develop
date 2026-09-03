using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Infrastructure.Layouts;

/// <summary>
/// Встроенный каталог топ-50 популярных клавиатур: поиск типовой раскладки
/// по паре VID/PID. Стиль — код-данные (как <see cref="LayoutProvider"/>):
/// без I/O, статически тестируемо.
/// Источник пар VID/PID — публичный реестр usb.org и база usb.ids;
/// при выпуске новой версии каталог переверифицируется по Device Manager.
/// Ошибка каталога не фатальна: пользователь меняет раскладку вручную,
/// привязка обновляется (см. план v1.2.0, риски).
/// </summary>
public sealed class KeyboardCatalog : IKeyboardCatalog
{
    private static readonly IReadOnlyList<KnownKeyboard> _keyboards = new[]
    {
        // ===== Logitech (046D) =====
        new KnownKeyboard(0x046D, 0xC318, "Logitech", "G213 Prodigy", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x046D, 0xC328, "Logitech", "G413", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x046D, 0xC331, "Logitech", "G512 Carbon", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x046D, 0xC335, "Logitech", "G815", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x046D, 0xC33C, "Logitech", "G915", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x046D, 0xC33E, "Logitech", "G915 TKL", KeyboardLayout.Tkl),
        new KnownKeyboard(0x046D, 0xC31C, "Logitech", "G Pro", KeyboardLayout.Tkl),
        new KnownKeyboard(0x046D, 0xC338, "Logitech", "G Pro X", KeyboardLayout.Tkl),
        new KnownKeyboard(0x046D, 0xC343, "Logitech", "K845", KeyboardLayout.Ansi104),

        // ===== Razer (1532) =====
        new KnownKeyboard(0x1532, 0x0113, "Razer", "BlackWidow Ultimate 2013", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1532, 0x0227, "Razer", "BlackWidow V3", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1532, 0x0228, "Razer", "BlackWidow V3 Pro", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1532, 0x0271, "Razer", "BlackWidow V4", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1532, 0x0235, "Razer", "Huntsman Elite", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1532, 0x0237, "Razer", "Huntsman Tournament Edition", KeyboardLayout.Tkl),
        new KnownKeyboard(0x1532, 0x0243, "Razer", "Huntsman Mini", KeyboardLayout.Layout60),
        new KnownKeyboard(0x1532, 0x0246, "Razer", "Huntsman V2 TKL", KeyboardLayout.Tkl),
        new KnownKeyboard(0x1532, 0x0257, "Razer", "Huntsman V2", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1532, 0x025A, "Razer", "Cynosa V2", KeyboardLayout.Ansi104),

        // ===== Corsair (1B1C) =====
        new KnownKeyboard(0x1B1C, 0x1B13, "Corsair", "K70 LUX", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1B1C, 0x1B2C, "Corsair", "K95 Platinum", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1B1C, 0x1B2D, "Corsair", "K70 RGB MK.2", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1B1C, 0x1B45, "Corsair", "K95 Platinum XT", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1B1C, 0x1B51, "Corsair", "Strafe RGB", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1B1C, 0x1B70, "Corsair", "K100 RGB", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1B1C, 0x1B92, "Corsair", "K65 RGB Mini", KeyboardLayout.Layout60),
        new KnownKeyboard(0x1B1C, 0x1BC5, "Corsair", "K70 RGB TKL", KeyboardLayout.Tkl),

        // ===== SteelSeries (1038) =====
        new KnownKeyboard(0x1038, 0x1610, "SteelSeries", "Apex 3", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1038, 0x1612, "SteelSeries", "Apex Pro", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1038, 0x1614, "SteelSeries", "Apex 5", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x1038, 0x1616, "SteelSeries", "Apex Pro TKL", KeyboardLayout.Tkl),
        new KnownKeyboard(0x1038, 0x1618, "SteelSeries", "Apex 7", KeyboardLayout.Ansi104),

        // ===== Cherry (046A) =====
        new KnownKeyboard(0x046A, 0x0034, "Cherry", "G80-3000", KeyboardLayout.Iso105),
        new KnownKeyboard(0x046A, 0x006E, "Cherry", "MX Board 3.0", KeyboardLayout.Iso105),

        // ===== Keychron (34EA) =====
        new KnownKeyboard(0x34EA, 0x0503, "Keychron", "Q0", KeyboardLayout.Numpad),
        new KnownKeyboard(0x34EA, 0x0510, "Keychron", "Q1", KeyboardLayout.Layout75),
        new KnownKeyboard(0x34EA, 0x0512, "Keychron", "Q3", KeyboardLayout.Tkl),
        new KnownKeyboard(0x34EA, 0x0513, "Keychron", "Q4", KeyboardLayout.Layout60),
        new KnownKeyboard(0x34EA, 0x0516, "Keychron", "Q6", KeyboardLayout.Ansi104),
        new KnownKeyboard(0x34EA, 0x0517, "Keychron", "V1", KeyboardLayout.Layout75),
        new KnownKeyboard(0x34EA, 0x0518, "Keychron", "V3", KeyboardLayout.Tkl),

        // ===== Ducky (1EA7) =====
        new KnownKeyboard(0x1EA7, 0x0004, "Ducky", "One 2 Mini", KeyboardLayout.Layout60),
        new KnownKeyboard(0x1EA7, 0x0024, "Ducky", "One 3 TKL", KeyboardLayout.Tkl),

        // ===== Varmilo (056E) =====
        new KnownKeyboard(0x056E, 0x00BC, "Varmilo", "VA87M", KeyboardLayout.Tkl),
        new KnownKeyboard(0x056E, 0x00BE, "Varmilo", "VA108M", KeyboardLayout.Ansi104),

        // ===== Glorious (25A7) =====
        new KnownKeyboard(0x25A7, 0xFA67, "Glorious", "GMMK Pro", KeyboardLayout.Layout75),
        new KnownKeyboard(0x25A7, 0x9067, "Glorious", "GMMK TKL", KeyboardLayout.Tkl),

        // ===== Royal Kludge (1A81) =====
        new KnownKeyboard(0x1A81, 0x2030, "Royal Kludge", "RK61", KeyboardLayout.Layout60),
        new KnownKeyboard(0x1A81, 0x2038, "Royal Kludge", "RK84", KeyboardLayout.Layout75),

        // ===== Roccat (1E7D) =====
        new KnownKeyboard(0x1E7D, 0x2FA4, "Roccat", "Vulcan 120 AIMO", KeyboardLayout.Ansi104),
    };

    private readonly IReadOnlyDictionary<uint, KnownKeyboard> _byVidPid;

    /// <summary>
    /// Создаёт каталог и строит индекс поиска по ключу (VID << 16) | PID.
    /// </summary>
    public KeyboardCatalog()
    {
        _byVidPid = _keyboards.ToDictionary(k => BuildLookupKey(k.VendorId, k.ProductId));
    }

    /// <inheritdoc />
    public IReadOnlyList<KnownKeyboard> All => _keyboards;

    /// <inheritdoc />
    public KnownKeyboard? FindByVidPid(uint vendorId, uint productId)
    {
        return _byVidPid.TryGetValue(BuildLookupKey(vendorId, productId), out KnownKeyboard? keyboard)
            ? keyboard
            : null;
    }

    private static uint BuildLookupKey(uint vendorId, uint productId)
    {
        return (vendorId << 16) | productId;
    }
}
