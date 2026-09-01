using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace KeyboardTester.Integration.Tests.Helpers;

/// <summary>
/// Выполняет проверку в выделенном STA-потоке.
/// Требуется для <see cref="System.Windows.Interop.HwndSource"/> в RawInputCapture.
/// </summary>
internal static class Sta
{
    /// <summary>
    /// Запускает действие в новом STA-потоке и дожидается завершения,
    /// пробрасывая исключения (включая ошибки ассертов) в тестовый поток.
    /// </summary>
    public static void Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Exception? exception = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                Dispatcher.FromThread(Thread.CurrentThread)?.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
