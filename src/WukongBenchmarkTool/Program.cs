using System.Diagnostics;
using WukongBenchmarkTool.Benchmark;
using WukongBenchmarkTool.Report;
using WukongBenchmarkTool.Steam;
using WukongBenchmarkTool.SystemInfo;

var calibrationPath = Path.Combine(AppContext.BaseDirectory, "calibration.json");

void Log(string message) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");

if (args.Length > 0 && args[0].Equals("calibrate", StringComparison.OrdinalIgnoreCase))
{
    return await RunCalibrationAsync();
}

return await RunBenchmarkAsync();

async Task<int> RunCalibrationAsync()
{
    try
    {
        var installation = SteamLocator.Locate();
        Log($"Benchmark Tool найден: {installation.GameExePath}");

        var existingPids = Process.GetProcessesByName(installation.ProcessName).Select(p => p.Id).ToHashSet();
        Process.Start(new ProcessStartInfo(
            installation.SteamExePath, $"-applaunch {SteamLocator.BenchmarkToolAppId}")
        { UseShellExecute = true });

        Process? gameProcess = null;
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (DateTime.UtcNow < deadline && gameProcess is null)
        {
            gameProcess = Process.GetProcessesByName(installation.ProcessName)
                .FirstOrDefault(p => !existingPids.Contains(p.Id));
            if (gameProcess is null)
            {
                await Task.Delay(1000);
            }
        }

        if (gameProcess is null)
        {
            Log("Процесс Benchmark Tool не появился — калибровка отменена.");
            return 1;
        }

        var recorder = new CalibrationRecorder(Log);
        var steps = await recorder.RecordAsync(gameProcess);
        if (steps.Count == 0)
        {
            Log("Шагов не записано — файл калибровки не изменён.");
            return 1;
        }

        CalibrationFile.Save(calibrationPath, steps);
        Log($"Сохранено в {calibrationPath}. Запустите обычный прогон (без аргумента calibrate).");
        return 0;
    }
    catch (Exception ex)
    {
        Log($"Ошибка калибровки: {ex.Message}");
        return 1;
    }
}

async Task<int> RunBenchmarkAsync()
{
    Console.WriteLine("=== Black Myth: Wukong Benchmark Tool — автопрогон CPU/GPU ===");
    Console.WriteLine();

    Log("Собираю характеристики ПК...");
    var hardware = HardwareInfoCollector.Collect();
    Console.WriteLine(hardware.Format());
    Console.WriteLine();

    SteamInstallation installation;
    try
    {
        installation = SteamLocator.Locate();
    }
    catch (SteamLocatorException ex)
    {
        Log($"Не удалось найти Benchmark Tool: {ex.Message}");
        return 1;
    }

    Log($"Benchmark Tool: {installation.GameExePath}");
    Log($"Настройки: {installation.GameUserSettingsIniPath}");

    var steps = CalibrationFile.LoadOrDefault(calibrationPath);
    if (!File.Exists(calibrationPath))
    {
        Log("calibration.json не найден — использую плейсхолдер-шаги по умолчанию (почти " +
            "наверняка потребуют правки). Запустите `wukong-bench calibrate`, чтобы записать " +
            "реальные шаги один раз вручную.");
    }

    var iniWriter = new IniSettingsWriter(installation.GameUserSettingsIniPath);
    var backupPath = iniWriter.CreateBackup();
    Log($"Бэкап настроек сохранён: {backupPath}");

    var runner = new BenchmarkRunner(installation, Log);
    using var cts = new CancellationTokenSource();

    try
    {
        var cpuProfile = BenchmarkProfiles.CpuBound();
        Log($"Применяю профиль «{cpuProfile.Name}»...");
        iniWriter.Apply(cpuProfile);
        var cpuResult = await runner.RunPassAsync(cpuProfile, steps, cts.Token);
        Log($"CPU-тест готов: средний FPS {cpuResult.AverageFps:0.0}, 1% low {cpuResult.OnePercentLowFps:0.0}.");

        var gpuProfile = BenchmarkProfiles.GpuBound(hardware);
        Log($"Применяю профиль «{gpuProfile.Name}»...");
        iniWriter.Apply(gpuProfile);
        var gpuResult = await runner.RunPassAsync(gpuProfile, steps, cts.Token);
        Log($"GPU-тест готов: средний FPS {gpuResult.AverageFps:0.0}, 1% low {gpuResult.OnePercentLowFps:0.0}.");

        var reportPath = ReportWriter.Write(
            Path.Combine(AppContext.BaseDirectory, "reports"), hardware, cpuProfile, cpuResult, gpuProfile, gpuResult);
        Log($"Отчёт сохранён: {reportPath}");
        return 0;
    }
    catch (Exception ex)
    {
        Log($"Прогон остановлен из-за ошибки: {ex.Message}");
        return 1;
    }
    finally
    {
        iniWriter.Restore(backupPath);
        Log("Исходные настройки GameUserSettings.ini восстановлены.");
    }
}
