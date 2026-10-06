using System.Globalization;

namespace WukongBenchmarkTool.Benchmark;

/// <summary>
/// Патчит GameUserSettings.ini под нужный профиль. Секция "/Script/Engine.GameUserSettings" —
/// стандартное имя для всех UE4/5-игр, поэтому зашито напрямую, а не угадывается.
/// Всегда делает бэкап оригинала и умеет его восстановить — чтобы автопрогон не оставлял
/// систему пользователя с изменёнными настройками, если что-то пойдёт не так.
/// </summary>
public sealed class IniSettingsWriter(string iniPath)
{
    private const string SectionName = "/Script/Engine.GameUserSettings";

    public string CreateBackup()
    {
        var backupPath = iniPath + ".bench-backup";
        File.Copy(iniPath, backupPath, overwrite: true);
        return backupPath;
    }

    public void Restore(string backupPath)
    {
        File.Copy(backupPath, iniPath, overwrite: true);
        File.Delete(backupPath);
    }

    public void Apply(BenchmarkProfile profile)
    {
        var lines = File.ReadAllLines(iniPath).ToList();
        var values = BuildKeyValues(profile);

        var sectionStart = lines.FindIndex(l =>
            l.Trim().Equals($"[{SectionName}]", StringComparison.OrdinalIgnoreCase));

        if (sectionStart < 0)
        {
            lines.Add($"[{SectionName}]");
            sectionStart = lines.Count - 1;
        }

        var sectionEnd = lines.FindIndex(sectionStart + 1, l => l.TrimStart().StartsWith('['));
        if (sectionEnd < 0)
        {
            sectionEnd = lines.Count;
        }

        var remaining = new HashSet<string>(values.Keys, StringComparer.OrdinalIgnoreCase);
        for (var i = sectionStart + 1; i < sectionEnd; i++)
        {
            var eq = lines[i].IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var key = lines[i][..eq].Trim();
            if (values.TryGetValue(key, out var newValue))
            {
                lines[i] = $"{key}={newValue}";
                remaining.Remove(key);
            }
        }

        var insertAt = sectionEnd;
        foreach (var key in remaining)
        {
            lines.Insert(insertAt, $"{key}={values[key]}");
            insertAt++;
        }

        File.WriteAllLines(iniPath, lines);
    }

    private static Dictionary<string, string> BuildKeyValues(BenchmarkProfile profile)
    {
        var inv = CultureInfo.InvariantCulture;
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bUseVSync"] = "False",
            ["ResolutionSizeX"] = profile.ResolutionX.ToString(inv),
            ["ResolutionSizeY"] = profile.ResolutionY.ToString(inv),
            ["LastUserConfirmedResolutionSizeX"] = profile.ResolutionX.ToString(inv),
            ["LastUserConfirmedResolutionSizeY"] = profile.ResolutionY.ToString(inv),
            ["FullscreenMode"] = ((int)profile.FullscreenMode).ToString(inv),
            ["LastConfirmedFullscreenMode"] = ((int)profile.FullscreenMode).ToString(inv),
            ["FrameRateLimit"] = "0.000000",
            ["sg.ResolutionQuality"] = profile.ResolutionQualityPercent.ToString(inv),
            ["sg.ViewDistanceQuality"] = profile.ScalabilityLevel.ToString(inv),
            ["sg.AntiAliasingQuality"] = profile.ScalabilityLevel.ToString(inv),
            ["sg.ShadowQuality"] = profile.ScalabilityLevel.ToString(inv),
            ["sg.PostProcessQuality"] = profile.ScalabilityLevel.ToString(inv),
            ["sg.TextureQuality"] = profile.ScalabilityLevel.ToString(inv),
            ["sg.EffectsQuality"] = profile.ScalabilityLevel.ToString(inv),
            ["sg.FoliageQuality"] = profile.ScalabilityLevel.ToString(inv),
            ["sg.ShadingQuality"] = profile.ScalabilityLevel.ToString(inv),
            ["sg.RayTracingQuality"] = profile.RayTracingLevel.ToString(inv),
        };
        return values;
    }
}
