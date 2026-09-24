using System.Diagnostics;
using System.Text.Json;
using QirimType.Utils;

namespace QirimType.Configuration;

public class SettingsManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsFilePath;
    private AppSettings _currentSettings;

    public event Action<AppSettings>? SettingsChanged;

    public AppSettings Settings => _currentSettings;

    public SettingsManager()
    {
        string appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QirimType");

        Directory.CreateDirectory(appDataFolder);
        _settingsFilePath = Path.Combine(appDataFolder, "settings.json");

        _currentSettings = LoadSettings();

        // Sync autostart registry state
        _currentSettings.StartWithWindows = AutostartManager.IsAutostartEnabled();
    }

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null && loaded.Mappings != null && loaded.Mappings.Count > 0)
                {
                    // Ensure all 7 Crimean Tatar symbols exist
                    var defaultMappings = AppSettings.GetDefaultMappings();
                    foreach (var def in defaultMappings)
                    {
                        if (!loaded.Mappings.Any(m => m.Symbol == def.Symbol))
                        {
                            loaded.Mappings.Add(def);
                        }
                    }
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load settings from {_settingsFilePath}: {ex.Message}");
        }

        var defaultSettings = new AppSettings();
        SaveSettings(defaultSettings);
        return defaultSettings;
    }

    public void SaveSettings(AppSettings newSettings)
    {
        try
        {
            _currentSettings = newSettings;

            // Update autostart registry
            AutostartManager.SetAutostart(newSettings.StartWithWindows);

            string json = JsonSerializer.Serialize(_currentSettings, JsonOptions);
            File.WriteAllText(_settingsFilePath, json);

            SettingsChanged?.Invoke(_currentSettings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public void SetEnabled(bool enabled)
    {
        _currentSettings.IsEnabled = enabled;
        SaveSettings(_currentSettings);
    }

    public void SetStartWithWindows(bool startWithWindows)
    {
        _currentSettings.StartWithWindows = startWithWindows;
        SaveSettings(_currentSettings);
    }

    public void ResetToDefaults()
    {
        var defaults = new AppSettings
        {
            IsEnabled = _currentSettings.IsEnabled,
            StartWithWindows = _currentSettings.StartWithWindows,
            Mappings = AppSettings.GetDefaultMappings()
        };
        SaveSettings(defaults);
    }
}
