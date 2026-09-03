namespace KeyboardTester.Core.Models;

/// <summary>
/// Известная модель клавиатуры из встроенного каталога: пара VID/PID,
/// бренд, модель и типовая раскладка.
/// </summary>
/// <param name="VendorId">Идентификатор производителя (USB VID).</param>
/// <param name="ProductId">Идентификатор продукта (USB PID).</param>
/// <param name="Brand">Бренд-производитель (например, «Logitech»).</param>
/// <param name="Model">Модель клавиатуры (например, «G Pro X»).</param>
/// <param name="Layout">Типовая физическая раскладка модели.</param>
public sealed record KnownKeyboard(
    uint VendorId,
    uint ProductId,
    string Brand,
    string Model,
    KeyboardLayout Layout)
{
    /// <summary>
    /// Отображаемое имя: «Бренд Модель».
    /// </summary>
    public string DisplayName => $"{Brand} {Model}";
}
