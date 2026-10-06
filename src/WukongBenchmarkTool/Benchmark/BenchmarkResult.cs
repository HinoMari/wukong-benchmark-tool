namespace WukongBenchmarkTool.Benchmark;

public sealed record BenchmarkResult(
    double AverageFps,
    double OnePercentLowFps,
    double MinFps,
    double MaxFps,
    double? AverageGpuFps,
    int FrameCount,
    string SourceCsvPath);
