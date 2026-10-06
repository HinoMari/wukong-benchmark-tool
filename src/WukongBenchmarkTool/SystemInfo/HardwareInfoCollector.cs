using System.Management;

namespace WukongBenchmarkTool.SystemInfo;

/// <summary>
/// Собирает характеристики ПК через WMI. Выбирает первый видеоадаптер с ненулевой
/// выделенной VRAM как "основной" GPU — это надёжнее, чем брать первую запись
/// Win32_VideoController, т.к. там нередко первым идёт виртуальный/интегрированный адаптер.
/// </summary>
public static class HardwareInfoCollector
{
    public static HardwareInfo Collect()
    {
        var cpu = QueryFirst("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
        var gpu = QueryBestGpu();
        var ramBytes = SumRam();
        var os = QueryFirst("SELECT Caption FROM Win32_OperatingSystem");

        return new HardwareInfo(
            CpuName: (cpu?["Name"] as string ?? "неизвестно").Trim(),
            CpuCores: ToInt(cpu?["NumberOfCores"]),
            CpuLogicalProcessors: ToInt(cpu?["NumberOfLogicalProcessors"]),
            GpuName: (gpu?["Name"] as string ?? "неизвестно").Trim(),
            GpuVramBytes: ToLong(gpu?["AdapterRAM"]),
            TotalRamBytes: ramBytes,
            OsName: (os?["Caption"] as string ?? "неизвестно").Trim(),
            DisplayWidth: ToInt(gpu?["CurrentHorizontalResolution"]),
            DisplayHeight: ToInt(gpu?["CurrentVerticalResolution"]));
    }

    private static ManagementBaseObject? QueryFirst(string query)
    {
        using var searcher = new ManagementObjectSearcher(query);
        foreach (ManagementBaseObject item in searcher.Get())
        {
            return item;
        }

        return null;
    }

    private static ManagementBaseObject? QueryBestGpu()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, AdapterRAM, CurrentHorizontalResolution, CurrentVerticalResolution FROM Win32_VideoController");

        ManagementBaseObject? best = null;
        long bestVram = -1;
        foreach (ManagementBaseObject item in searcher.Get())
        {
            var vram = ToLong(item["AdapterRAM"]);
            if (vram > bestVram)
            {
                bestVram = vram;
                best = item;
            }
        }

        return best;
    }

    private static long SumRam()
    {
        using var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory");
        long total = 0;
        foreach (ManagementBaseObject item in searcher.Get())
        {
            total += ToLong(item["Capacity"]);
        }

        return total;
    }

    private static int ToInt(object? value) => value is null ? 0 : Convert.ToInt32(value);

    private static long ToLong(object? value) => value is null ? 0L : Convert.ToInt64(value);
}
