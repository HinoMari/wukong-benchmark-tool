using Microsoft.Win32;

namespace WukongBenchmarkTool.Steam;

/// <summary>
/// Находит установку Steam и "Black Myth: Wukong Benchmark Tool" (AppID 3132990) на диске,
/// не полагаясь на единственный жёстко зашитый путь — библиотека игр может быть на любом
/// диске, поэтому путь ищется через реестр Steam + libraryfolders.vdf + appmanifest.
/// </summary>
public static class SteamLocator
{
    public const string BenchmarkToolAppId = "3132990";

    public static SteamInstallation Locate()
    {
        var steamPath = FindSteamInstallPath()
            ?? throw new SteamLocatorException("Не удалось найти установку Steam в реестре.");

        var steamExe = Path.Combine(steamPath, "steam.exe");
        if (!File.Exists(steamExe))
        {
            throw new SteamLocatorException($"steam.exe не найден по пути {steamExe}.");
        }

        var libraryPaths = ReadLibraryFolders(steamPath);
        foreach (var library in libraryPaths)
        {
            var manifestPath = Path.Combine(library, "steamapps", $"appmanifest_{BenchmarkToolAppId}.acf");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            var manifest = VdfNode.Parse(File.ReadAllText(manifestPath));
            var appState = manifest.Child("AppState")
                ?? throw new SteamLocatorException($"Некорректный формат {manifestPath}.");
            var installDir = appState.Child("installdir")?.Value
                ?? throw new SteamLocatorException($"В {manifestPath} нет ключа installdir.");

            var appRoot = Path.Combine(library, "steamapps", "common", installDir);
            var gameExe = FindGameExe(appRoot)
                ?? throw new SteamLocatorException(
                    $"Не нашёл b1-Win64-Shipping.exe внутри {appRoot}. " +
                    "Возможно, изменилось имя исполняемого файла — проверьте вручную.");
            var iniPath = FindGameUserSettingsIni(appRoot)
                ?? throw new SteamLocatorException(
                    $"Не нашёл GameUserSettings.ini внутри {appRoot}\\*\\Saved\\Config. " +
                    "Запустите игру один раз вручную, чтобы файл настроек был создан.");

            return new SteamInstallation(steamExe, library, appRoot, gameExe, iniPath);
        }

        throw new SteamLocatorException(
            $"AppID {BenchmarkToolAppId} (Black Myth: Wukong Benchmark Tool) не установлен ни в одной " +
            "из найденных библиотек Steam. Установите его через Steam перед запуском.");
    }

    private static string? FindSteamInstallPath()
    {
        var fromHklm = Registry.GetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;
        if (!string.IsNullOrEmpty(fromHklm) && Directory.Exists(fromHklm))
        {
            return fromHklm;
        }

        var fromHkcu = Registry.GetValue(
            @"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SteamPath", null) as string;
        if (!string.IsNullOrEmpty(fromHkcu) && Directory.Exists(fromHkcu))
        {
            return fromHkcu.Replace('/', '\\');
        }

        return null;
    }

    private static List<string> ReadLibraryFolders(string steamPath)
    {
        var result = new List<string> { steamPath };
        var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdfPath))
        {
            return result;
        }

        var root = VdfNode.Parse(File.ReadAllText(vdfPath));
        var libraryFolders = root.Child("libraryfolders");
        if (libraryFolders is null)
        {
            return result;
        }

        foreach (var (_, entry) in libraryFolders.ChildEntries())
        {
            var path = entry.Child("path")?.Value;
            if (!string.IsNullOrEmpty(path))
            {
                var normalized = path.Replace("\\\\", "\\");
                if (!result.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(normalized);
                }
            }
        }

        return result;
    }

    private static string? FindGameExe(string appRoot)
    {
        if (!Directory.Exists(appRoot))
        {
            return null;
        }

        var expected = Path.Combine(appRoot, "b1", "Binaries", "Win64", "b1-Win64-Shipping.exe");
        if (File.Exists(expected))
        {
            return expected;
        }

        return Directory.EnumerateFiles(appRoot, "*-Win64-Shipping.exe", SearchOption.AllDirectories).FirstOrDefault();
    }

    private static string? FindGameUserSettingsIni(string appRoot)
    {
        if (!Directory.Exists(appRoot))
        {
            return null;
        }

        var expected = Path.Combine(appRoot, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");
        if (File.Exists(expected))
        {
            return expected;
        }

        // UE4-проекты иногда используют "WindowsNoEditor" вместо "Windows" — ищем защищённо.
        return Directory.EnumerateFiles(appRoot, "GameUserSettings.ini", SearchOption.AllDirectories).FirstOrDefault();
    }
}

public sealed class SteamLocatorException(string message) : Exception(message);
