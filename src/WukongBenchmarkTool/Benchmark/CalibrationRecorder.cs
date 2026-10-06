using System.Diagnostics;
using WukongBenchmarkTool.Native;

namespace WukongBenchmarkTool.Benchmark;

/// <summary>
/// Интерактивная запись шагов навигации: запускает Benchmark Tool, пока пользователь руками
/// проходит экраны до старта прогона, опрашивает глобальное состояние левой кнопки мыши и
/// нескольких "навигационных" клавиш (Enter/Escape/Space/стрелки/Tab) и копит их как
/// CalibrationStep-и с паузами между ними. Завершается по клавише Q в консоли.
/// Это ответ на пункт задания "способ запуска... определить самостоятельно" в условиях,
/// когда сам Benchmark Tool недоступен для разработки вслепую — калибровка выполняется
/// один раз человеком, а дальше прогон идёт полностью автоматически по записанным шагам.
/// </summary>
public sealed class CalibrationRecorder(Action<string> log)
{
    private static readonly (ushort Vk, string Name)[] WatchedKeys =
    [
        (0x0D, "Enter"),
        (0x1B, "Escape"),
        (0x20, "Space"),
        (0x09, "Tab"),
        (0x25, "Left"),
        (0x27, "Right"),
        (0x26, "Up"),
        (0x28, "Down"),
    ];

    public async Task<List<CalibrationStep>> RecordAsync(Process process)
    {
        var windowHandle = await WindowHelper.WaitForMainWindowAsync(process, TimeSpan.FromMinutes(3));
        if (windowHandle == IntPtr.Zero)
        {
            log("Не дождался окна Benchmark Tool.");
            return [];
        }

        log("Запись начата. Пройдите меню до старта прогона вручную (клики и Enter/Escape/" +
            "Space/стрелки/Tab отслеживаются). Нажмите Q в этом окне консоли, чтобы закончить.");

        var steps = new List<CalibrationStep>();
        var lastEventAt = DateTime.UtcNow;
        var wasMouseDown = false;
        var wasKeyDown = new bool[WatchedKeys.Length];

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Q)
                {
                    break;
                }
            }

            var isMouseDown = InputSimulator.IsLeftMouseButtonDown();
            if (isMouseDown && !wasMouseDown)
            {
                RecordWait(steps, ref lastEventAt);
                var (cx, cy) = InputSimulator.GetCursorPosition();
                var bounds = WindowHelper.GetWindowBounds(windowHandle);
                if (bounds is { } b && b.Width > 0 && b.Height > 0)
                {
                    var fx = (cx - b.X) / (double)b.Width;
                    var fy = (cy - b.Y) / (double)b.Height;
                    steps.Add(new CalibrationStep { Kind = CalibrationStepKind.Click, ClickXFraction = fx, ClickYFraction = fy });
                    log($"Записан клик: ({fx:0.000}, {fy:0.000})");
                }
            }

            wasMouseDown = isMouseDown;

            for (var i = 0; i < WatchedKeys.Length; i++)
            {
                var isDown = InputSimulator.IsKeyDown(WatchedKeys[i].Vk);
                if (isDown && !wasKeyDown[i])
                {
                    RecordWait(steps, ref lastEventAt);
                    steps.Add(new CalibrationStep { Kind = CalibrationStepKind.Key, KeyCode = WatchedKeys[i].Vk });
                    log($"Записана клавиша: {WatchedKeys[i].Name}");
                }

                wasKeyDown[i] = isDown;
            }

            await Task.Delay(50);
        }

        log($"Запись окончена: {steps.Count} шаг(ов).");
        return steps;
    }

    private static void RecordWait(List<CalibrationStep> steps, ref DateTime lastEventAt)
    {
        var now = DateTime.UtcNow;
        var elapsed = (int)(now - lastEventAt).TotalMilliseconds;
        if (elapsed > 100)
        {
            steps.Add(new CalibrationStep { Kind = CalibrationStepKind.Wait, WaitMs = elapsed });
        }

        lastEventAt = now;
    }
}
