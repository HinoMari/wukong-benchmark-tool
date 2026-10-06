using WukongBenchmarkTool.SystemInfo;

namespace WukongBenchmarkTool.Benchmark;

public static class BenchmarkProfiles
{
    /// <summary>
    /// Низкое разрешение и минимальное качество графики снимают нагрузку с GPU (меньше
    /// пикселей, более дешёвые шейдеры/тени/RT), но не снижают CPU-нагрузку — та же логика
    /// игры, ИИ, физика, число draw call'ов и LOD-уровней объектов. Узким местом остаётся CPU.
    /// </summary>
    public static BenchmarkProfile CpuBound() => new(
        Name: "CPU-тест",
        Rationale:
            "1280x720, оконный режим, все sg.* на минимум (0), трассировка лучей выключена, " +
            "апскейлинг выключен (ResolutionQuality=100, т.е. рендер без дополнительного масштабирования), " +
            "VSync и лимит FPS выключены. Снижение разрешения/качества графики уменьшает нагрузку " +
            "на GPU, но не на CPU (логика игры, ИИ, физика и число draw call'ов не зависят от " +
            "разрешения) — поэтому узким местом становится процессор.",
        ResolutionX: 1280,
        ResolutionY: 720,
        FullscreenMode: FullscreenMode.Windowed,
        ResolutionQualityPercent: 100,
        ScalabilityLevel: BenchmarkProfile.MinQuality,
        RayTracingLevel: BenchmarkProfile.MinQuality);

    /// <summary>
    /// Нативное (или близкое к нему) разрешение экрана и максимальное качество графики,
    /// включая трассировку лучей, максимизируют нагрузку на видеокарту при той же CPU-нагрузке —
    /// узким местом становится GPU.
    /// </summary>
    public static BenchmarkProfile GpuBound(HardwareInfo hardware)
    {
        var width = hardware.DisplayWidth > 0 ? hardware.DisplayWidth : 2560;
        var height = hardware.DisplayHeight > 0 ? hardware.DisplayHeight : 1440;

        return new BenchmarkProfile(
            Name: "GPU-тест",
            Rationale:
                $"{width}x{height} (нативное разрешение экрана), полноэкранный режим, все sg.* на " +
                "максимум (3/Epic), трассировка лучей на максимум, апскейлинг выключен " +
                "(ResolutionQuality=100 — рендер в полном разрешении, без DLSS/FSR, чтобы не " +
                "снижать реальную нагрузку на GPU), VSync и лимит FPS выключены. Максимальные " +
                "разрешение и качество графики нагружают именно видеокарту, CPU-нагрузка " +
                "не меняется — узким местом становится GPU.",
            ResolutionX: width,
            ResolutionY: height,
            FullscreenMode: FullscreenMode.Fullscreen,
            ResolutionQualityPercent: 100,
            ScalabilityLevel: BenchmarkProfile.MaxQuality,
            RayTracingLevel: BenchmarkProfile.MaxQuality);
    }
}
