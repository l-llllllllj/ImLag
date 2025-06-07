using System.Text.Json;

namespace ImLag;

public class ConfigManager
{
    public KeyConfig Config { get; private set; } = new();
    private const string ConfigFile = "Config.json";

    public void LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                var loadedConfig = JsonSerializer.Deserialize<KeyConfig>(json);
                if (loadedConfig != null)
                {
                    Config = loadedConfig;
                    Console.WriteLine("已加载配置文件。");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载配置文件时出错: {ex.Message}");
        }

        LoadDefaultConfig();
        SaveConfig();
    }

    private void LoadDefaultConfig()
    {
        Config = new KeyConfig
        {
            ChatKey = "y",
            UserPlayerName = string.Empty,
            OnlySelfDeath = true,
            SkipWindowCheck = false,
            ForceMode = false,
            KeyDelay = 100
        };
        Console.WriteLine("已加载默认配置。");
    }

    public void SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(ConfigFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存配置文件时出错: {ex.Message}");
        }
    }

    public class KeyConfig
    {
        public string ChatKey { get; set; } = "y";
        public string UserPlayerName { get; set; } = string.Empty;
        public bool OnlySelfDeath { get; set; } = true;
        public bool SkipWindowCheck { get; set; }
        public bool ForceMode { get; set; }
        public int KeyDelay { get; set; } = 100;
    }
}