using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;

// ReSharper disable InconsistentNaming

namespace ImLag;

[SuppressMessage("Interoperability", "CA1416:验证平台兼容性")]
public class CfgManager
{
    private readonly ChatMessageManager _chatManager;
    private readonly ConfigManager _configManager;

    public string SteamPath { get; private set; } = string.Empty;
    public string CS2Path => _configManager.Config.CS2Path ?? string.Empty;
    public string CfgPath { get; private set; } = string.Empty;
    public int TotalCfgFiles => _configManager.Config.TotalCfgFiles;
    private readonly Random _random = new();
    public List<string> BindKeys { get; set; }

    public CfgManager(ChatMessageManager chatManager, ConfigManager configManager)
    {
        _chatManager = chatManager;
        _configManager = configManager;

        if (string.IsNullOrEmpty(CS2Path))
        {
            FindCS2Path();
        }
        else
        {
            UpdateCfgPath();
        }

        BindKeys = _configManager.Config.BindKeys;
    }

    public void FindCS2Path()
    {
        try
        {
            using (var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                SteamPath = regKey?.GetValue("SteamPath") as string ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(SteamPath))
            {
                var libraryFoldersPath = Path.Combine(SteamPath, "steamapps", "libraryfolders.vdf");
                List<string> steamLibraries = [SteamPath];

                if (File.Exists(libraryFoldersPath))
                {
                    var lines = File.ReadAllLines(libraryFoldersPath);
                    steamLibraries.AddRange(from line in lines
                        select line.Trim()
                        into trimmedLine
                        where trimmedLine.StartsWith("\"path\"")
                        select trimmedLine.Split('\"')[3]
                        into path
                        where Directory.Exists(path)
                        select path);
                }

                string[] possibleRelativePaths =
                {
                    Path.Combine("steamapps", "common", "Counter-Strike Global Offensive"),
                    Path.Combine("steamapps", "common", "Counter-Strike 2")
                };

                foreach (var libPath in steamLibraries.Distinct())
                {
                    foreach (var relativePath in possibleRelativePaths)
                    {
                        var potentialCs2Path = Path.Combine(libPath, relativePath);
                        if (!Directory.Exists(potentialCs2Path) ||
                            !File.Exists(Path.Combine(potentialCs2Path, "game", "csgo", "pak01_dir.vpk")))
                            continue; // 检查特征文件
                        _configManager.Config.CS2Path = potentialCs2Path;
                        _configManager.SaveConfig();
                        UpdateCfgPath();
                        Console.WriteLine($"自动找到CS2路径: {CS2Path}");
                        Console.WriteLine($"CFG文件将被写入: {CfgPath}");
                        return;
                    }
                }
            }

            Console.WriteLine("未能自动找到CS2路径。请使用菜单选项 (S) 手动设置。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"自动查找CS2路径时出错: {ex.Message}");
        }
    }

    private void UpdateCfgPath()
    {
        if (string.IsNullOrEmpty(CS2Path)) return;
        CfgPath = Path.Combine(CS2Path, "game", "csgo", "cfg");

        if (Directory.Exists(CfgPath)) return;
        try
        {
            Directory.CreateDirectory(CfgPath);
            Console.WriteLine($"已创建CFG目录: {CfgPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建CFG目录 '{CfgPath}' 失败: {ex.Message}");
        }
    }

    public void SetCS2Path(string path)
    {
        if (Directory.Exists(path) && File.Exists(Path.Combine(path, "game", "csgo", "pak01_dir.vpk"))) // 简单验证
        {
            _configManager.Config.CS2Path = path;
            _configManager.SaveConfig();
            UpdateCfgPath();
            Console.WriteLine($"CS2路径已设置为: {CS2Path}");
            Console.WriteLine($"CFG文件将被写入: {CfgPath}");
        }
        else
        {
            Console.WriteLine("无效的CS2路径，目录不存在或不是有效的CS2安装目录。");
        }
    }

    public void AddBindKey(string key)
    {
        if (!string.IsNullOrWhiteSpace(key) && key.Length == 1 && char.IsLetterOrDigit(key[0]))
        {
            var normalizedKey = key.ToLower();
            if (!BindKeys.Contains(normalizedKey))
            {
                BindKeys.Add(normalizedKey);
                _configManager.SaveConfig();
                Console.WriteLine($"已添加绑定键: {normalizedKey}");
                Console.WriteLine($"当前所有绑定键: {string.Join(", ", BindKeys)}");
            }
            else
            {
                Console.WriteLine("该按键已在绑定列表中。");
            }
        }
        else
        {
            Console.WriteLine("无效的按键，必须是单个字母或数字。");
        }
    }

    public void RemoveBindKey(string key)
    {
        if (BindKeys.Count <= 1)
        {
            Console.WriteLine("至少需要保留一个绑定键。");
            return;
        }

        var normalizedKey = key.ToLower();
        if (BindKeys.Remove(normalizedKey))
        {
            _configManager.SaveConfig();
            Console.WriteLine($"已移除绑定键: {normalizedKey}");
            Console.WriteLine($"当前所有绑定键: {string.Join(", ", BindKeys)}");
        }
        else
        {
            Console.WriteLine("未找到该绑定键。");
        }
    }

    public string GetRandomBindKey()
    {
        return BindKeys[_random.Next(BindKeys.Count)];
    }

    public void SetTotalCfgFiles(int count)
    {
        if (count is >= 1 and <= 200)
        {
            _configManager.Config.TotalCfgFiles = count;
            _configManager.SaveConfig();
            Console.WriteLine($"CFG文件数量已更改为: {TotalCfgFiles}");
        }
        else
        {
            Console.WriteLine("无效的数量，CFG文件数量应在1-200之间。");
        }
    }

    private string EscapeMessageForCfg(string message)
    {
        message = message.Replace("\"", "\"\"");
        message = message.Replace(";", "");
        return message;
    }

    public bool GenerateConfigFiles()
    {
        var messages = _chatManager.GetAllMessages();
        if (messages.Count == 0)
        {
            Console.WriteLine("\n消息列表为空，请先按A添加消息后再生成CFG。");
            return false;
        }

        if (string.IsNullOrEmpty(CS2Path) || !Directory.Exists(CS2Path))
        {
            Console.WriteLine("\nCS2路径无效或未设置。请先按S设置正确的CS2路径。");
            return false;
        }

        if (string.IsNullOrEmpty(CfgPath) || !Directory.Exists(CfgPath))
        {
            UpdateCfgPath();
            if (!Directory.Exists(CfgPath))
            {
                Console.WriteLine($"\nCFG目录 '{CfgPath}' 不存在且无法创建。请检查权限或手动创建。");
                return false;
            }
        }


        try
        {
            Random random = new();
            var shuffledMessages = messages.OrderBy(_ => random.Next()).ToList();
            var actualTotalFiles = Math.Min(TotalCfgFiles, shuffledMessages.Count);

            if (actualTotalFiles < TotalCfgFiles)
            {
                Console.WriteLine(
                    $"注意：消息数量 ({shuffledMessages.Count}) 少于请求的CFG文件数 ({TotalCfgFiles})。将只为每条消息生成一个CFG，共 {actualTotalFiles} 个。");
            }


            for (var i = 0; i < actualTotalFiles; i++)
            {
                var filename = $"imlag_say_{i + 1}.cfg";
                var filePath = Path.Combine(CfgPath, filename);
                var messageToUse = EscapeMessageForCfg(shuffledMessages[i]);

                using (var writer = new StreamWriter(filePath, false))
                {
                    writer.WriteLine($"// ImLag Random Say CFG - File {i + 1}");
                    writer.WriteLine($"// Message: {shuffledMessages[i]}");
                    writer.WriteLine($"say \"{messageToUse}\"");
                }

                Console.WriteLine($"已生成: {filePath}");
            }

            for (var i = actualTotalFiles; i < 200; i++)
            {
                var oldFilename = $"imlag_say_{i + 1}.cfg";
                var oldFilePath = Path.Combine(CfgPath, oldFilename);
                if (!File.Exists(oldFilePath)) continue;
                File.Delete(oldFilePath);
                Console.WriteLine($"已删除旧文件: {oldFilePath}");
            }


            if (actualTotalFiles > 0)
            {
                GenerateSelectorFile(actualTotalFiles);
                Console.WriteLine($"\n成功生成 {actualTotalFiles} 个消息CFG文件和1个选择器文件。");
            }
            else
            {
                Console.WriteLine("\n没有可用的消息来生成CFG文件。");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成CFG文件时出错: {ex.Message}");
            return false;
        }
    }

    private void GenerateSelectorFile(int numberOfMessageFiles)
    {
        if (numberOfMessageFiles == 0) return;

        var selectorFilePath = Path.Combine(CfgPath, "imlag_say_selector.cfg");
        using (var writer = new StreamWriter(selectorFilePath, false))
        {
            writer.WriteLine("// ImLag Random Say Selector CFG");
            writer.WriteLine($"// Cycles through {numberOfMessageFiles} message CFGs.");
            writer.WriteLine();

            for (int i = 1; i <= numberOfMessageFiles; i++)
            {
                int nextFileIndex = i % numberOfMessageFiles + 1;
                writer.WriteLine(
                    $"alias imlag_random_say_{i} \"exec imlag_say_{i}; alias imlag_do_say imlag_random_say_{nextFileIndex}\"");
            }

            writer.WriteLine();
            writer.WriteLine($"alias imlag_do_say imlag_random_say_1");
            writer.WriteLine($"imlag_do_say");
        }

        Console.WriteLine($"已生成选择器文件: {selectorFilePath}");
    }

    public bool UpdateAutoexecFile()
    {
        if (string.IsNullOrEmpty(CfgPath) || !Directory.Exists(CfgPath))
        {
            Console.WriteLine("\nCFG路径无效或未设置，无法更新autoexec.cfg。");
            return false;
        }

        string autoexecFilePath = Path.Combine(CfgPath, "autoexec.cfg");
        string imlagCommentStart = "// --- ImLag Auto-Bind Start ---";
        string imlagCommentEnd = "// --- ImLag Auto-Bind End ---";

        try
        {
            List<string> lines = [];
            bool autoexecExists = File.Exists(autoexecFilePath);

            if (autoexecExists)
            {
                lines.AddRange(File.ReadAllLines(autoexecFilePath));
                int startIndex = -1, endIndex = -1;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Trim() == imlagCommentStart) startIndex = i;
                    if (lines[i].Trim() == imlagCommentEnd && startIndex != -1)
                    {
                        endIndex = i;
                        break;
                    }
                }

                if (startIndex != -1 && endIndex != -1)
                {
                    lines.RemoveRange(startIndex, endIndex - startIndex + 1);
                    Console.WriteLine("已从autoexec.cfg中移除旧的ImLag绑定。");
                }

                lines.RemoveAll(line => line.Contains("exec imlag_say_selector"));
            }
            else
            {
                Console.WriteLine($"autoexec.cfg 文件不存在于 '{CfgPath}'，将创建一个新的。");
                lines.Add("// Counter-Strike 2 Autoexec Configuration File");
                lines.Add("// Generated by ImLag");
                lines.Add("");
            }

            lines.Add("");
            lines.Add(imlagCommentStart);
            lines.Add($"// This block is automatically managed by ImLag v{Program.Version}");
            foreach (var key in BindKeys)
            {
                lines.Add($"bind \"{key}\" \"exec imlag_say_selector\"");
                lines.Add($"echo \"ImLag: '{key}' bound to random message selector.\"");
            }

            lines.Add(imlagCommentEnd);
            lines.Add("");

            lines.RemoveAll(line => line.Trim().ToLower() == "host_writeconfig");
            lines.Add("host_writeconfig");

            File.WriteAllLines(autoexecFilePath, lines);
            Console.WriteLine($"已{(autoexecExists ? "更新" : "创建")} autoexec.cfg 文件并添加/更新绑定。");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"更新/创建 autoexec.cfg 时出错: {ex.Message}");
            return false;
        }
    }

    public void ShowCfgInstructions()
    {
        Console.WriteLine("\n=== CFG模式配置完成/说明 ===");
        if (string.IsNullOrEmpty(CS2Path))
        {
            Console.WriteLine("CS2路径尚未设置，部分功能可能受限。请按S设置。");
            return;
        }

        Console.WriteLine($"CFG文件已生成在: {CfgPath}");
        Console.WriteLine(
            $"1. 包含随机消息的CFG文件: imlag_say_1.cfg ... imlag_say_{Math.Min(TotalCfgFiles, _chatManager.GetAllMessages().Count)}.cfg");
        Console.WriteLine($"2. 选择器CFG文件: imlag_say_selector.cfg");
        Console.WriteLine(
            $"3. autoexec.cfg 中应已添加绑定: bind \"{string.Join(", ", BindKeys)}\" \"exec imlag_say_selector\"");
        Console.WriteLine();
        Console.WriteLine("使用方法:");
        Console.WriteLine($"  - 在CS2游戏中，按下您设置的绑定键 (当前为: '{string.Join(", ", BindKeys)}') 即可发送一条随机消息。");
        Console.WriteLine("  - 每次按下绑定键都会发送不同的消息，循环播放。");
        Console.WriteLine();
        Console.WriteLine("重要提示 - 确保autoexec.cfg被执行:");
        Console.WriteLine("  如果您是首次配置或遇到问题，请确保CS2启动时会执行autoexec.cfg。");
        Console.WriteLine("  方法1: 在Steam中，右键点击CS2 -> '属性...' -> '通用' -> '启动选项'，");
        Console.WriteLine("          在输入框中添加(如果已有其他选项，用空格隔开): +exec autoexec.cfg");
        Console.WriteLine("  方法2: 在游戏控制台中手动输入 `exec autoexec.cfg` 来测试。");
        Console.WriteLine("          如果autoexec.cfg被正确执行，您应该能在控制台看到类似 \"ImLag: 'X' bound...\" 的消息。");
        Console.WriteLine();
    }
}