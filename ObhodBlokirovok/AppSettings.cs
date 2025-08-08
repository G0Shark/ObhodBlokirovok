using System.IO;
using System.Text.Json;

namespace ObhodBlokirovok;

public class AppSettings
{
    // === Настройки ===
    public bool EnableNotifications { get; set; } = true;
    public bool EnableNoClose { get; set; } = true;
    public bool EnableAutoStart { get; set; } = false;

    // === Путь к JSON файлу ===
    private static readonly string SettingsFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    /// <summary>
    /// Загрузка настроек из JSON. Если файл отсутствует — создаётся с настройками по умолчанию.
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                var defaultSettings = new AppSettings();
                defaultSettings.Save(); // создаём файл с дефолтными настройками
                return defaultSettings;
            }

            string json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);

            return settings ?? new AppSettings();
        }
        catch
        {
            // При ошибке — возвращаем настройки по умолчанию
            return new AppSettings();
        }
    }

    /// <summary>
    /// Сохранение текущих настроек в JSON файл.
    /// </summary>
    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            // Обработка ошибок (например, логирование)
            Console.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
        }
    }
}