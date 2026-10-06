using System.Diagnostics;
using WukongBenchmarkTool.Native;

namespace WukongBenchmarkTool.Benchmark;

/// <summary>
/// Проводит запущенный процесс Benchmark Tool через меню по заранее записанным шагам
/// (см. CalibrationStep) — клики переводятся из долей окна в абсолютные экранные координаты
/// на основе текущего положения и размера окна игры.
/// </summary>
public sealed class MenuNavigator(Action<string> log)
{
    public async Task RunAsync(Process process, IReadOnlyList<CalibrationStep> steps, CancellationToken ct)
    {
        var windowHandle = await WindowHelper.WaitForMainWindowAsync(process, TimeSpan.FromMinutes(2));
        if (windowHandle == IntPtr.Zero)
        {
            log("Не дождался главного окна Benchmark Tool — пропускаю автонавигацию по меню.");
            return;
        }

        WindowHelper.Focus(windowHandle);

        foreach (var step in steps)
        {
            ct.ThrowIfCancellationRequested();
            if (process.HasExited)
            {
                log("Процесс завершился раньше, чем закончились шаги навигации — останавливаюсь.");
                return;
            }

            log($"Навигация: {step.Description}");
            switch (step.Kind)
            {
                case CalibrationStepKind.Wait:
                    await Task.Delay(step.WaitMs, ct);
                    break;

                case CalibrationStepKind.Key:
                    InputSimulator.PressKey(step.KeyCode);
                    break;

                case CalibrationStepKind.Click:
                    var bounds = WindowHelper.GetWindowBounds(windowHandle);
                    if (bounds is null)
                    {
                        log("Не удалось получить границы окна — клик пропущен.");
                        break;
                    }

                    var (x, y, w, h) = bounds.Value;
                    InputSimulator.ClickAt(x + (int)(step.ClickXFraction * w), y + (int)(step.ClickYFraction * h));
                    break;
            }
        }
    }
}
