using System.Text.Json;
using System.Text.Encodings.Web;

// ReSharper disable InconsistentNaming

namespace ImLag;

public class ConfigManager
{
    public KeyConfig Config { get; private set; } = new();
    private const string ConfigFile = "Config.json";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

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
                    if (Config.TotalCfgFiles <= 0) Config.TotalCfgFiles = 5;
                    Config.CS2Path ??= string.Empty;

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
            KeyDelay = 100,
            BindKeys = ["k", "p", "l", "m"],
            UseCfgMode = false,
            TotalCfgFiles = 5,
            CS2Path = string.Empty,
            KeySimulationMethod = 3,
        };
        Console.WriteLine("已加载默认配置。");
    }

    public void SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config, _jsonOptions);
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
        public string UserPlayerName { get; set; } = string.Empty; // 用于GSI模式下的"仅自己死亡"
        public bool OnlySelfDeath { get; set; } = true; // GSI模式选项
        public bool SkipWindowCheck { get; set; } // GSI模式选项
        public bool ForceMode { get; set; } // GSI模式选项
        public int KeyDelay { get; set; } = 100; // GSI模式选项
        public bool UseCfgMode { get; set; } // True表示使用CFG模式，False表示GSI聊天模式
        public int TotalCfgFiles { get; set; } = 5; // CFG模式下生成的CFG文件数量

        public List<string> BindKeys { get; set; } = ["p", "k", "l", "m"];
        public string? CS2Path { get; set; } = string.Empty; // CS2游戏根目录路径
        public int KeySimulationMethod { get; set; } = 3;
        public bool UseTraditionalChinese { get; set; } = false; // false = 简体中文, true = 繁体中文
    }
}