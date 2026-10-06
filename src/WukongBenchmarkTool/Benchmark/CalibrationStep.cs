using System.Text.Json;
using System.Text.Json.Serialization;

namespace WukongBenchmarkTool.Benchmark;

public enum CalibrationStepKind
{
    Wait,
    Click,
    Key,
}

/// <summary>
/// Один шаг прохода через меню Benchmark Tool до старта прогона. Клики хранятся в долях
/// экрана (0..1), а не в пикселях, чтобы запись оставалась рабочей при другом разрешении окна.
/// </summary>
public sealed class CalibrationStep
{
    public CalibrationStepKind Kind { get; set; }

    public int WaitMs { get; set; }

    public double ClickXFraction { get; set; }

    public double ClickYFraction { get; set; }

    public ushort KeyCode { get; set; }

    [JsonIgnore]
    public string Description =>
        Kind switch
        {
            CalibrationStepKind.Wait => $"ждать {WaitMs} мс",
            CalibrationStepKind.Click => $"клик в ({ClickXFraction:0.000}, {ClickYFraction:0.000}) от окна",
            CalibrationStepKind.Key => $"нажать код клавиши {KeyCode}",
            _ => "?",
        };
}

/// <summary>
/// Файл с шагами навигации по меню — единственная часть автоматизации, которую нельзя было
/// проверить вживую (инструмент не установлен). Значения-плейсхолдеры ниже заведомо условны:
/// запустите `wukong-bench calibrate`, чтобы один раз пройти меню руками и записать реальные шаги.
/// </summary>
public static class CalibrationFile
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static List<CalibrationStep> LoadOrDefault(string path)
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var steps = JsonSerializer.Deserialize<List<CalibrationStep>>(json);
            if (steps is { Count: > 0 })
            {
                return steps;
            }
        }

        return DefaultPlaceholderSteps();
    }

    public static void Save(string path, List<CalibrationStep> steps)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(steps, JsonOptions));
    }

    /// <summary>
    /// ПЛЕЙСХОЛДЕР: предполагает, что после запуска достаточно один раз нажать Enter, чтобы
    /// попасть с заставки в главное меню, подождать его прогрузки и ещё раз нажать Enter, чтобы
    /// запустить бенчмарк с настройками по умолчанию (обычно это выделенный по умолчанию пункт
    /// меню). Почти наверняка потребует правки через `calibrate` на реальном запуске.
    /// </summary>
    private static List<CalibrationStep> DefaultPlaceholderSteps() =>
        [
            new CalibrationStep { Kind = CalibrationStepKind.Wait, WaitMs = 15000 },
            new CalibrationStep { Kind = CalibrationStepKind.Key, KeyCode = 0x0D }, // Enter — пропуск заставки
            new CalibrationStep { Kind = CalibrationStepKind.Wait, WaitMs = 5000 },
            new CalibrationStep { Kind = CalibrationStepKind.Key, KeyCode = 0x0D }, // Enter — запуск прогона
        ];
}
