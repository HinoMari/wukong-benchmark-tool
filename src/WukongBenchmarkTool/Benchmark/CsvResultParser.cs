using System.Globalization;

namespace WukongBenchmarkTool.Benchmark;

/// <summary>
/// Считает FPS-метрики из CSV, который пишет встроенный в Unreal Engine CSV Profiler
/// (флаги запуска -csvCaptureFrames/-csvGpuStats, каталог Saved/Profiling/CSV). Метрики
/// считаются самостоятельно по кадровому времени (FrameTime, мс), а не берутся из
/// недокументированного "экрана результатов" самого Benchmark Tool.
/// </summary>
public static class CsvResultParser
{
    public static string? FindLatestCsv(string profilingCsvDir, DateTime sinceUtc)
    {
        if (!Directory.Exists(profilingCsvDir))
        {
            return null;
        }

        return Directory.EnumerateFiles(profilingCsvDir, "*.csv")
            .Select(f => new FileInfo(f))
            .Where(f => f.LastWriteTimeUtc >= sinceUtc.AddSeconds(-5))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault()?.FullName;
    }

    public static BenchmarkResult Parse(string csvPath)
    {
        using var reader = new StreamReader(csvPath);
        var headerLine = reader.ReadLine() ?? throw new InvalidDataException($"Пустой CSV: {csvPath}");
        var headers = headerLine.Split(',');

        var frameTimeIndex = Array.FindIndex(headers, h => h.Equals("FrameTime", StringComparison.OrdinalIgnoreCase));
        if (frameTimeIndex < 0)
        {
            throw new InvalidDataException($"В {csvPath} нет колонки FrameTime — неожиданный формат CSV Profiler'а.");
        }

        var gpuFrameTimeIndex = Array.FindIndex(headers, h => h.Equals("FrameTime_GPU", StringComparison.OrdinalIgnoreCase));

        var frameTimesMs = new List<double>();
        var gpuFrameTimesMs = new List<double>();

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var cells = line.Split(',');
            if (cells.Length <= frameTimeIndex)
            {
                continue;
            }

            if (double.TryParse(cells[frameTimeIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var ft) && ft > 0)
            {
                frameTimesMs.Add(ft);

                if (gpuFrameTimeIndex >= 0 && cells.Length > gpuFrameTimeIndex &&
                    double.TryParse(cells[gpuFrameTimeIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var gft) && gft > 0)
                {
                    gpuFrameTimesMs.Add(gft);
                }
            }
        }

        if (frameTimesMs.Count == 0)
        {
            throw new InvalidDataException($"В {csvPath} не нашлось ни одного валидного кадра.");
        }

        var avgFrameTime = frameTimesMs.Average();
        var sorted = frameTimesMs.OrderDescending().ToList();
        var onePercentCount = Math.Max(1, sorted.Count / 100);
        var onePercentLowAvgFrameTime = sorted.Take(onePercentCount).Average();

        return new BenchmarkResult(
            AverageFps: 1000.0 / avgFrameTime,
            OnePercentLowFps: 1000.0 / onePercentLowAvgFrameTime,
            MinFps: 1000.0 / sorted[0],
            MaxFps: 1000.0 / sorted[^1],
            AverageGpuFps: gpuFrameTimesMs.Count > 0 ? 1000.0 / gpuFrameTimesMs.Average() : null,
            FrameCount: frameTimesMs.Count,
            SourceCsvPath: csvPath);
    }
}
