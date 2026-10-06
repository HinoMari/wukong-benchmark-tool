namespace WukongBenchmarkTool.SystemInfo;

public sealed record HardwareInfo(
    string CpuName,
    int CpuCores,
    int CpuLogicalProcessors,
    string GpuName,
    long GpuVramBytes,
    long TotalRamBytes,
    string OsName,
    int DisplayWidth,
    int DisplayHeight)
{
    public string Format() =>
        $"""
         CPU: {CpuName} ({CpuCores} ядер / {CpuLogicalProcessors} потоков)
         GPU: {GpuName} (VRAM: {GpuVramBytes / 1024 / 1024} МБ)
         RAM: {TotalRamBytes / 1024 / 1024 / 1024} ГБ
         ОС: {OsName}
         Экран: {DisplayWidth}x{DisplayHeight}
         """;
}
