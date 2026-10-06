namespace WukongBenchmarkTool.Benchmark;

/// <summary>
/// Набор графических настроек для одного прохода бенчмарка.
/// Значения sg.* — стандартные группы качества Unreal Engine (0 = Low, 3 = Epic/Cinematic).
/// </summary>
public sealed record BenchmarkProfile(
    string Name,
    string Rationale,
    int ResolutionX,
    int ResolutionY,
    FullscreenMode FullscreenMode,
    int ResolutionQualityPercent,
    int ScalabilityLevel,
    int RayTracingLevel)
{
    public const int MinQuality = 0;
    public const int MaxQuality = 3;
}

public enum FullscreenMode
{
    Fullscreen = 0,
    WindowedFullscreen = 1,
    Windowed = 2,
}
