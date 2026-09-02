using System.Reflection;
using Res = KeyboardTester.UI.Resources;

namespace KeyboardTester.UI.Services;

/// <summary>
/// Единая точка получения отображаемой версии сборки приложения.
/// SDK автоматически добавляет к AssemblyInformationalVersion суффикс
/// «+<hash>» (SourceRevisionId), поэтому версия обрезается до «чистого»
/// номера, например «1.1.0».
/// </summary>
public static class AppVersion
{
    /// <summary>
    /// Возвращает отображаемую версию сборки без VCS-хеша.
    /// Используется в статус-баре главного окна и в диалоге «О программе».
    /// </summary>
    public static string Current
    {
        get
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string? version = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString();

            if (string.IsNullOrWhiteSpace(version))
            {
                return Res.Strings.VersionUnknown;
            }

            int hashIndex = version.IndexOf('+');
            return hashIndex > 0 ? version[..hashIndex] : version;
        }
    }
}
