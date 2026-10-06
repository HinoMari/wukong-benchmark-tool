namespace WukongBenchmarkTool.Steam;

public sealed record SteamInstallation(
    string SteamExePath,
    string LibraryPath,
    string AppInstallDir,
    string GameExePath,
    string GameUserSettingsIniPath)
{
    /// <summary>GameUserSettings.ini лежит в .../Saved/Config/&lt;Windows|WindowsNoEditor&gt;/ — поднимаемся на 3 уровня.</summary>
    public string SavedDir => Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(GameUserSettingsIniPath)!)!)!;

    public string ProfilingCsvDir => Path.Combine(SavedDir, "Profiling", "CSV");

    public string LogFilePath => Path.Combine(SavedDir, "Logs", Path.GetFileNameWithoutExtension(GameExePath).Replace("-Win64-Shipping", "") + ".log");

    public string ProcessName => Path.GetFileNameWithoutExtension(GameExePath);
}
