using System.Globalization;
using System.Text;
using WukongBenchmarkTool.Benchmark;
using WukongBenchmarkTool.SystemInfo;

namespace WukongBenchmarkTool.Report;

public static class ReportWriter
{
    public static string Write(
        string outputDir,
        HardwareInfo hardware,
        BenchmarkProfile cpuProfile,
        BenchmarkResult cpuResult,
        BenchmarkProfile gpuProfile,
        BenchmarkResult gpuResult)
    {
        Directory.CreateDirectory(outputDir);
        var path = Path.Combine(outputDir, $"report-{DateTime.Now:yyyyMMdd-HHmmss}.md");

        var sb = new StringBuilder();
        sb.AppendLine("# Результаты Black Myth: Wukong Benchmark Tool");
        sb.AppendLine();
        sb.AppendLine($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("## Характеристики ПК");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine(hardware.Format());
        sb.AppendLine("```");
        sb.AppendLine();

        AppendPass(sb, cpuProfile, cpuResult);
        AppendPass(sb, gpuProfile, gpuResult);

        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static void AppendPass(StringBuilder sb, BenchmarkProfile profile, BenchmarkResult result)
    {
        var inv = CultureInfo.InvariantCulture;
        sb.AppendLine($"## {profile.Name}");
        sb.AppendLine();
        sb.AppendLine("**Настройки и почему так:**");
        sb.AppendLine();
        sb.AppendLine(profile.Rationale);
        sb.AppendLine();
        sb.AppendLine("**Результат:**");
        sb.AppendLine();
        sb.AppendLine($"- Средний FPS: {result.AverageFps.ToString("0.0", inv)}");
        sb.AppendLine($"- 1% low FPS: {result.OnePercentLowFps.ToString("0.0", inv)}");
        sb.AppendLine($"- Мин / Макс FPS: {result.MinFps.ToString("0.0", inv)} / {result.MaxFps.ToString("0.0", inv)}");
        if (result.AverageGpuFps is { } gpuFps)
        {
            sb.AppendLine($"- Средний GPU FPS (время кадра на GPU): {gpuFps.ToString("0.0", inv)}");
        }

        sb.AppendLine($"- Кадров учтено: {result.FrameCount}");
        sb.AppendLine($"- Источник: `{result.SourceCsvPath}`");
        sb.AppendLine();
    }
}
