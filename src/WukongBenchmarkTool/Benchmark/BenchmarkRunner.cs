using System.Diagnostics;
using WukongBenchmarkTool.Steam;

namespace WukongBenchmarkTool.Benchmark;

/// <summary>
/// Запускает один проход бенчмарка через Steam, проводит через меню и дожидается завершения.
/// "Завершение" определяется эвристически (CSV перестал расти / процесс вышел / общий таймаут),
/// т.к. официального сигнала "прогон закончен" инструмент не документирует — см. план калибровки.
/// </summary>
public sealed class BenchmarkRunner(SteamInstallation installation, Action<string> log)
{
    private const int CsvCaptureFrameCeiling = 200_000; // с запасом на ~55 минут при 60 fps
    private static readonly TimeSpan ProcessAppearTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan CsvStabilityWindow = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan OverallRunTimeout = TimeSpan.FromMinutes(20);

    public async Task<BenchmarkResult> RunPassAsync(
        BenchmarkProfile profile, IReadOnlyList<CalibrationStep> navigationSteps, CancellationToken ct)
    {
        var existingPids = Process.GetProcessesByName(installation.ProcessName).Select(p => p.Id).ToHashSet();
        var launchArgs =
            $"-applaunch {SteamLocator.BenchmarkToolAppId} " +
            $"-ResX={profile.ResolutionX} -ResY={profile.ResolutionY} " +
            $"-csvCaptureFrames={CsvCaptureFrameCeiling} -csvGpuStats";

        log($"Запускаю Steam: steam.exe {launchArgs}");
        var launchedAtUtc = DateTime.UtcNow;
        using (Process.Start(new ProcessStartInfo(installation.SteamExePath, launchArgs) { UseShellExecute = true }))
        {
            // steam.exe сам по себе — короткоживущий лаунчер, реальный процесс игры находим отдельно.
        }

        var gameProcess = await WaitForGameProcessAsync(existingPids, ct);
        if (gameProcess is null)
        {
            throw new BenchmarkRunException(
                $"Процесс {installation.ProcessName} не появился за {ProcessAppearTimeout.TotalSeconds:0} секунд после steam -applaunch.");
        }

        log($"Процесс найден: PID {gameProcess.Id}.");

        var navigator = new MenuNavigator(log);
        await navigator.RunAsync(gameProcess, navigationSteps, ct);

        log("Навигация по меню завершена, ожидаю окончания прогона бенчмарка...");
        await WaitForBenchmarkCompletionAsync(gameProcess, launchedAtUtc, ct);

        await EnsureProcessExitedAsync(gameProcess);

        var csvPath = CsvResultParser.FindLatestCsv(installation.ProfilingCsvDir, launchedAtUtc)
            ?? throw new BenchmarkRunException(
                $"Не нашёл CSV с результатами в {installation.ProfilingCsvDir}. " +
                "Проверьте, что -csvCaptureFrames действительно поддерживается этой сборкой, " +
                "и что in-game консоль/флаги профилирования не заблокированы в Shipping-сборке.");

        log($"Разбираю результаты: {csvPath}");
        return CsvResultParser.Parse(csvPath);
    }

    private async Task<Process?> WaitForGameProcessAsync(HashSet<int> existingPids, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + ProcessAppearTimeout;
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var candidate = Process.GetProcessesByName(installation.ProcessName)
                .FirstOrDefault(p => !existingPids.Contains(p.Id));
            if (candidate is not null)
            {
                return candidate;
            }

            await Task.Delay(1000, ct);
        }

        return null;
    }

    private async Task WaitForBenchmarkCompletionAsync(Process gameProcess, DateTime launchedAtUtc, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + OverallRunTimeout;
        long lastSeenLength = -1;
        DateTime lastGrowthAt = DateTime.UtcNow;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            gameProcess.Refresh();
            if (gameProcess.HasExited)
            {
                log("Процесс завершился сам — считаю прогон оконченным.");
                return;
            }

            var csvPath = CsvResultParser.FindLatestCsv(installation.ProfilingCsvDir, launchedAtUtc);
            if (csvPath is not null)
            {
                var length = new FileInfo(csvPath).Length;
                if (length != lastSeenLength)
                {
                    lastSeenLength = length;
                    lastGrowthAt = DateTime.UtcNow;
                }
                else if (length > 0 && DateTime.UtcNow - lastGrowthAt >= CsvStabilityWindow)
                {
                    log($"CSV не растёт уже {CsvStabilityWindow.TotalSeconds:0} секунд — считаю прогон оконченным.");
                    return;
                }
            }

            await Task.Delay(2000, ct);
        }

        log($"Достигнут общий таймаут ожидания ({OverallRunTimeout.TotalMinutes:0} мин) — продолжаю принудительно.");
    }

    private static async Task EnsureProcessExitedAsync(Process gameProcess)
    {
        gameProcess.Refresh();
        if (gameProcess.HasExited)
        {
            return;
        }

        gameProcess.CloseMainWindow();
        try
        {
            await gameProcess.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token);
        }
        catch (OperationCanceledException)
        {
            gameProcess.Kill(entireProcessTree: true);
        }
    }
}

public sealed class BenchmarkRunException(string message) : Exception(message);
