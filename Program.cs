using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CounterStrike2GSI;
using CounterStrike2GSI.EventMessages;
using TextCopy;
using WindowsInput;
using WindowsInput.Native;

// ReSharper disable InconsistentNaming

namespace ImLag;

[SuppressMessage("Interoperability", "CA1416:验证平台兼容性")]
internal static partial class Program
{
    public const string Version = "2.0.4";
    private const string Author = "Eicy";
    private const string Name = "ImLag";
    private const string UpdateLog = "优化CS检测模式、添加WinFormSendKeys模拟模式。";

    private static GameStateListener? _gsl;
    private static ChatMessageManager _chatManager;
    private static ConfigManager _configManager;
    private static CfgManager _cfgManager;
    private static readonly InputSimulator _inputSimulator = new();
    private static bool _useInputSimulator;
    private static bool _useSendInput;
    private static bool _useWinFormSendKeys;

    [STAThread]
    private static void Main()
    {
        Console.Title = $"{Name} v{Version} by {Author}";

        if (!IsRunningAsAdministrator())
        {
            Console.WriteLine("警告：请以管理员身份运行以确保按键发送和CFG写入正常。");
            Console.WriteLine("按任意键继续...");
            Console.ReadKey();
            Console.Clear();
        }

        _configManager = new ConfigManager();
        _configManager.LoadConfig();

        _chatManager = new ChatMessageManager();
        _chatManager.LoadMessages();

        _cfgManager = new CfgManager(_chatManager, _configManager);

        _useInputSimulator = _configManager.Config.KeySimulationMethod == 1;
        _useSendInput = _configManager.Config.KeySimulationMethod == 2;
        _useWinFormSendKeys = _configManager.Config.KeySimulationMethod == 3;
        

        if (string.IsNullOrWhiteSpace(_configManager.Config.UserPlayerName))
        {
            SetupPlayerName();
        }

        _gsl = new GameStateListener(4000);
        if (!_gsl.GenerateGSIConfigFile("ImLag"))
        {
            Console.WriteLine("无法生成GSI配置文件。");
        }

        _gsl.PlayerDied += OnPlayerDied;

        Console.WriteLine(!_gsl.Start() ? "GSI启动失败，请以管理员身份运行。" : "正在监听CS2游戏事件 (GSI)...");

        Console.WriteLine($"=== {Name} v{Version} by {Author} ===");
        Init();
        ConsoleKeyInfo keyInfo;
        do
        {
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(100);
            }

            keyInfo = Console.ReadKey(true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.A:
                    AddNewMessage();
                    break;
                case ConsoleKey.D:
                    DeleteMessage();
                    break;
                case ConsoleKey.L:
                    Console.WriteLine("\n消息列表：");
                    _chatManager.DisplayMessages();
                    break;
                case ConsoleKey.C:
                    if (!_configManager.Config.UseCfgMode)
                        ChangeChatKey();
                    else
                        Console.WriteLine("\nCFG模式无需配置聊天按键，请用绑定键。");
                    break;
                case ConsoleKey.P:
                    ChangePlayerName();
                    break;
                case ConsoleKey.M:
                    if (!_configManager.Config.UseCfgMode)
                        ToggleMonitorMode();
                    else
                        Console.WriteLine("\nCFG模式下监听模式无效。");
                    break;
                case ConsoleKey.W:
                    if (!_configManager.Config.UseCfgMode)
                        ToggleWindowCheck();
                    else
                        Console.WriteLine("\nCFG模式下窗口检测无效。");
                    break;
                case ConsoleKey.F:
                    if (!_configManager.Config.UseCfgMode)
                        ToggleForceMode();
                    else
                        Console.WriteLine("\nCFG模式无需强制发送模式。");
                    break;
                case ConsoleKey.K:
                    if (!_configManager.Config.UseCfgMode)
                        ChangeKeyDelay();
                    else
                        Console.WriteLine("\nCFG模式下按键延迟无效。");
                    break;
                case ConsoleKey.V:
                    ShowVersionInfo();
                    break;
                case ConsoleKey.I:
                    ToggleInputMethod();
                    break;
                case ConsoleKey.T:
                    ToggleOperationalMode();
                    break;
                case ConsoleKey.B:
                    if (_configManager.Config.UseCfgMode)
                        ChangeBindKey();
                    else
                        Console.WriteLine("\n请先切换到CFG模式 (T)。");
                    break;
                case ConsoleKey.S:
                    if (_configManager.Config.UseCfgMode)
                        SetCS2Path();
                    else
                        Console.WriteLine("\n请先切换到CFG模式 (T)。");
                    break;
                case ConsoleKey.G:
                    if (_configManager.Config.UseCfgMode)
                        GenerateCfgFiles();
                    else
                        Console.WriteLine("\n请先切换到CFG模式 (T)。");
                    break;
                default:
                    Console.WriteLine("\n无效按键，请查看下方提示。");
                    Init();
                    break;
            }
        } while (keyInfo.Key != ConsoleKey.Escape);
        _configManager.SaveConfig();
        _chatManager.SaveMessages();
        Console.WriteLine("程序已退出。");
    }
    

    private static void ToggleOperationalMode()
    {
        _configManager.Config.UseCfgMode = !_configManager.Config.UseCfgMode;
        _configManager.SaveConfig();
        Console.Clear();
        Console.WriteLine($"\n已切换到 {(_configManager.Config.UseCfgMode ? "CFG模式" : "聊天模式")}");

        if (_configManager.Config.UseCfgMode)
        {
            Console.WriteLine("CFG模式：通过游戏内按键发送消息，需生成CFG文件 (G)。");
            if (string.IsNullOrEmpty(_configManager.Config.CS2Path))
            {
                Console.WriteLine("\nCS2路径未设置，尝试自动查找...");
                _cfgManager.FindCS2Path();
            }

            if (_gsl == null)
            {
                _gsl = new GameStateListener(4000);
                if (!_gsl.GenerateGSIConfigFile("ImLag"))
                {
                    Console.WriteLine("无法生成GSI配置文件。");
                }

                _gsl.PlayerDied += OnPlayerDied;
            }

            if (!_gsl.Running)
            {
                Console.WriteLine(_gsl.Start() ? "GSI监听启动。" : "GSI启动失败，请以管理员身份运行。");
            }

            _cfgManager.ShowCfgInstructions();
        }
        else
        {
            if (_gsl == null)
            {
                _gsl = new GameStateListener(4000);
                if (!_gsl.GenerateGSIConfigFile("ImLag"))
                {
                    Console.WriteLine("无法生成GSI配置文件。");
                }

                _gsl.PlayerDied += OnPlayerDied;
            }

            if (!_gsl.Running)
            {
                Console.WriteLine(_gsl.Start() ? "GSI监听启动。" : "GSI启动失败，请以管理员身份运行。");
            }

            Console.WriteLine("聊天模式：GSI检测死亡，自动发送消息。");
        }

        Init();
    }

    private static void ChangeBindKey()
    {
        while (true)
        {
            Console.WriteLine($"\n绑定键: {string.Join(", ", _cfgManager.BindKeys)}");
            Console.WriteLine("1. 添加绑定键");
            Console.WriteLine("2. 删除绑定键");
            Console.WriteLine("3. 返回");
            Console.Write("选择 (1-3): ");

            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    Console.Write("输入绑定键 (字母/数字): ");
                    var input = Console.ReadLine()?.Trim().ToLower() ?? "";
                    _cfgManager.AddBindKey(input);
                    break;
                case "2":
                    if (_cfgManager.BindKeys.Count <= 1)
                    {
                        Console.WriteLine("需保留至少一个绑定键。");
                        break;
                    }

                    Console.Write("输入要删除的绑定键: ");
                    var keyToRemove = Console.ReadLine()?.Trim().ToLower() ?? "";
                    _cfgManager.RemoveBindKey(keyToRemove);
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("无效选择。");
                    break;
            }
        }
    }

    private static void SetCS2Path()
    {
        Console.WriteLine($"\nCS2路径: {(_cfgManager.CS2Path != "" ? _cfgManager.CS2Path : "未设置")}");
        Console.Write(
            @"输入CS2根目录路径 (如: C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive): ");
        var path = Console.ReadLine()?.Trim() ?? "";
        _cfgManager.SetCS2Path(path);
    }

    private static void UpdateCfgCount()
    {
        _cfgManager.SetTotalCfgFiles(_chatManager.Messages.Count);
    }

    private static void GenerateCfgFiles()
    {
        Console.WriteLine("\n=== 生成CFG文件 ===");
        if (!_cfgManager.GenerateConfigFiles()) return;
        Console.WriteLine("CFG文件已生成。");
        Console.WriteLine("是否更新 autoexec.cfg 以应用绑定? (y/n)");
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.Y)
        {
            if (_cfgManager.UpdateAutoexecFile())
            {
                _cfgManager.ShowCfgInstructions();
            }
        }
        else
        {
            Console.WriteLine("\n已跳过更新 autoexec.cfg。");
            Console.WriteLine("请手动添加以下内容到 autoexec.cfg:");
            Console.WriteLine($"bind \"{string.Join(", ", _cfgManager.BindKeys)}\" \"exec random_say_selector\"");
            Console.WriteLine($"路径: {_cfgManager.CfgPath}\\autoexec.cfg");
        }
    }

    private static void ToggleInputMethod()
    {
        var currentMethod = _configManager.Config.KeySimulationMethod;
        var nextMethod = (currentMethod + 1) % 4;

        _configManager.Config.KeySimulationMethod = nextMethod;
        _configManager.SaveConfig();
        
        _useInputSimulator = nextMethod == 1;
        _useSendInput = nextMethod == 2;
        _useWinFormSendKeys = nextMethod == 3;

        Console.WriteLine(nextMethod switch
        {
            0 => "\n模拟方式切换为: keybd_event",
            1 => "\n模拟方式切换为: InputSimulator",
            2 => "\n模拟方式切换为: SendInput",
            3 => "\n模拟方式切换为: WinForm SendKeys",
            _ => "\n模拟方式切换为: keybd_event"
        });
        
        if (_useInputSimulator)
            Console.WriteLine("InputSimulator 模式，可能需管理员权限。");
        else if (_useSendInput)
            Console.WriteLine("SendInput 模式，更现代且可靠。");
        else if (_useWinFormSendKeys)
            Console.WriteLine("WinForm SendKeys 模式，依赖活动窗口。");
        else
            Console.WriteLine("keybd_event 模式，兼容性较好。");
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static void ToggleForceMode()
    {
        _configManager.Config.ForceMode = !_configManager.Config.ForceMode;
        _configManager.SaveConfig();

        Console.WriteLine($"\n强制发送模式已{(_configManager.Config.ForceMode ? "启用" : "禁用")}");
        if (_configManager.Config.ForceMode)
        {
            Console.WriteLine("警告：强制模式可能导致重复发送，仅在发送失败时启用。");
        }
    }

    private static void ChangeKeyDelay()
    {
        Console.WriteLine($"\n当前按键延迟: {_configManager.Config.KeyDelay}ms");
        Console.Write("输入新延迟 (30-500ms): ");

        if (int.TryParse(Console.ReadLine(), out var delay) && delay is >= 30 and <= 500)
        {
            _configManager.Config.KeyDelay = delay;
            _configManager.SaveConfig();
            Console.WriteLine($"延迟更新为: {delay}ms");
        }
        else
        {
            Console.WriteLine("无效输入，需在30-500ms之间。");
        }
    }

    private static void ToggleMonitorMode()
    {
        _configManager.Config.OnlySelfDeath = !_configManager.Config.OnlySelfDeath;
        _configManager.SaveConfig();

        Console.WriteLine($"\n监听模式切换为: {(_configManager.Config.OnlySelfDeath ? "仅自己死亡" : "所有玩家死亡")}");
    }

    private static void ToggleWindowCheck()
    {
        _configManager.Config.SkipWindowCheck = !_configManager.Config.SkipWindowCheck;
        _configManager.SaveConfig();

        Console.WriteLine($"\n窗口检测已{(_configManager.Config.SkipWindowCheck ? "禁用" : "启用")}");
        if (_configManager.Config.SkipWindowCheck)
        {
            Console.WriteLine("警告：禁用窗口检测可能在非CS2窗口发送消息。");
        }
    }

    private static void ShowVersionInfo()
    {
        Console.WriteLine($"\n=== {Name} v{Version} ===");
        Console.WriteLine($"作者: {Author}");
        Console.WriteLine("GitHub: https://github.com/cneicy/ImLag");
        Console.WriteLine($"更新: {UpdateLog}");
    }

    private static void SetupPlayerName()
    {
        Console.WriteLine("\n=== 首次设置 ===");
        Console.WriteLine("请输入CS2游戏内玩家名（需完全一致）：");

        while (true)
        {
            Console.Write("玩家名: ");
            var playerName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(playerName))
            {
                Console.WriteLine("玩家名不能为空。");
                continue;
            }

            _configManager.Config.UserPlayerName = playerName.Trim();
            _configManager.SaveConfig();
            Console.WriteLine($"玩家名设置: {playerName}");
            break;
        }
    }

    private static void ChangePlayerName()
    {
        Console.WriteLine(
            $"\n当前玩家名: {(_configManager.Config.UserPlayerName == "" ? "未设置" : _configManager.Config.UserPlayerName)}");
        Console.Write("输入新玩家名 (Enter取消, '-'清空): ");
        var playerName = Console.ReadLine();

        if (string.IsNullOrEmpty(playerName))
        {
            Console.WriteLine("已取消。");
            return;
        }

        if (playerName == "-")
        {
            _configManager.Config.UserPlayerName = string.Empty;
            Console.WriteLine("玩家名已清空。");
        }
        else
        {
            _configManager.Config.UserPlayerName = playerName.Trim();
            Console.WriteLine($"玩家名更新: {playerName}");
        }

        _configManager.SaveConfig();
    }

    private static void Init()
    {
        Console.Clear();
        Console.WriteLine($"=== ImLag v{Version} ===");
        Console.WriteLine();

        if (_configManager.Config.UseCfgMode)
        {
            Console.WriteLine("**CFG模式** (按键发送消息)");
            Console.WriteLine($"绑定键: {string.Join(", ", _cfgManager.BindKeys)}");
            Console.WriteLine($"CFG数量: {_cfgManager.TotalCfgFiles}");
            Console.WriteLine($"CS2路径: {(_cfgManager.CS2Path != "" ? _cfgManager.CS2Path : "未设置 (S)")}");
            Console.WriteLine($"消息数: {_chatManager.Messages.Count}");
            var simulationMethod = _useInputSimulator ? "InputSimulator" :
                _useSendInput ? "SendInput" :
                _useWinFormSendKeys ? "WinForm SendKeys" : "keybd_event";
            Console.WriteLine($"模拟: {simulationMethod}");
            UpdateCfgCount();
            Console.WriteLine("\n操作: T-切换聊天模式 | A-加消息 | D-删消息 | L-列消息");
            Console.WriteLine("      B-改绑定键 | S-设CS2路径 | G-生成CFG");
        }
        else
        {
            var chatKeyDescription = _configManager.Config.ChatKey switch
            {
                "y" => "全局",
                "u" => "队内",
                "enter" => "回车",
                _ => $"自定义 ({_configManager.Config.ChatKey})"
            };
            Console.WriteLine("**聊天模式** (GSI自动发送)");
            Console.WriteLine($"聊天键: {chatKeyDescription}");
            Console.WriteLine(
                $"监听: {(_configManager.Config.OnlySelfDeath ? "仅自己" : "所有")} (玩家: {_configManager.Config.UserPlayerName})");
            Console.WriteLine($"窗口检测: {(_configManager.Config.SkipWindowCheck ? "禁用" : "启用")}");
            Console.WriteLine($"强制发送: {(_configManager.Config.ForceMode ? "启用" : "禁用")}");
            Console.WriteLine($"延迟: {_configManager.Config.KeyDelay}ms");
            var simulationMethod = _useInputSimulator ? "InputSimulator" :
                _useSendInput ? "SendInput" :
                _useWinFormSendKeys ? "WinForm SendKeys" : "keybd_event";
            Console.WriteLine($"模拟: {simulationMethod}");
            Console.WriteLine($"消息数: {_chatManager.Messages.Count}");
            Console.WriteLine("\n操作: T-切换CFG模式 | A-加消息 | D-删消息 | L-列消息 | C-改聊天键");
            Console.WriteLine("      P-改玩家名 | M-切换监听 | W-切换窗口检测 | F-强制发送 | K-改延迟 | I-切换模拟");
        }

        Console.WriteLine("\n通用: V-版本 | ESC-退出");
        Console.Write("选择操作: ");
    }

    private static void OnPlayerDied(PlayerDied gameEvent)
    {
        Console.WriteLine($"\n[GSI] 玩家死亡: {gameEvent.Player.Name}");

        if (_configManager.Config.OnlySelfDeath && gameEvent.Player.Name != _configManager.Config.UserPlayerName)
        {
            Console.WriteLine("[GSI] 仅监听自己死亡，跳过。");
            return;
        }

        if (_configManager.Config.OnlySelfDeath && string.IsNullOrEmpty(_configManager.Config.UserPlayerName))
        {
            Console.WriteLine("[GSI] 未设置玩家名，监听所有死亡。");
        }

        Console.WriteLine(gameEvent.Player.Name == _configManager.Config.UserPlayerName ||
                          string.IsNullOrEmpty(_configManager.Config.UserPlayerName)
            ? "[GSI] 你死了！"
            : $"[GSI] 队友 {gameEvent.Player.Name} 死亡！");

        if (!_configManager.Config.SkipWindowCheck && !IsCS2Active())
        {
            Console.WriteLine("[GSI] CS2非活动窗口，跳过。");
            return;
        }

        if (_configManager.Config.SkipWindowCheck)
        {
            Console.WriteLine("[GSI] 跳过窗口检测，发送消息。");
        }

        if (_configManager.Config.UseCfgMode)
        {
            var randomKey = _cfgManager.GetRandomBindKey();
            SimulateBindKey(randomKey);
            Console.WriteLine($"[GSI] 模拟按键: {randomKey}");
        }
        else
        {
            var randomMessage = _chatManager.GetRandomMessage();
            if (!string.IsNullOrEmpty(randomMessage))
            {
                SendChatMessage(randomMessage);
                Console.WriteLine($"[GSI] 发送消息: {randomMessage}");
            }
            else
            {
                Console.WriteLine("[GSI] 消息列表为空。");
            }
        }
    }

    private static void SimulateBindKey(string bindKey)
    {
        if (string.IsNullOrEmpty(bindKey)) return;
        if (_useInputSimulator)
        {
            var vk = GetVirtualKeyFromChar(bindKey[0]);
            if (vk != VirtualKeyCode.NONAME)
                _inputSimulator.Keyboard.KeyPress(vk);
        }
        else if (_useSendInput)
        {
            SendKeySendInput((byte)VkKeyScan(bindKey[0]));
        }
        else if (_useWinFormSendKeys)
        {
            SendKeys.SendWait(bindKey);
        }
        else
        {
            SendKeyNative((byte)VkKeyScan(bindKey[0]));
        }
    }

    private static void SendChatMessage(string message)
    {
        try
        {
            Console.WriteLine($"[GSI] 准备发送: {message}");
            Console.WriteLine(
                $"[GSI] 模拟方式: {(_useInputSimulator ? "InputSimulator" : _useSendInput ? "SendInput" : _useWinFormSendKeys ? "WinFormSendKeys" : "keybd_event")}");

            ClipboardService.SetText(message);
            ReleaseAllKeys();
            Thread.Sleep(300);

            if (_configManager.Config.ForceMode)
            {
                for (var i = 0; i < 3; i++)
                {
                    OpenChatBox();
                    Thread.Sleep(_configManager.Config.KeyDelay);
                }
            }
            else
            {
                OpenChatBox();
            }

            Thread.Sleep(_configManager.Config.KeyDelay * 2);
            ClearChatInput();
            Thread.Sleep(_configManager.Config.KeyDelay);

            for (var retry = 0; retry < 3; retry++)
            {
                try
                {
                    PasteFromClipboard();
                    break;
                }
                catch (Exception ex)
                {
                    if (retry < 2)
                    {
                        Console.WriteLine($"[GSI] 粘贴失败，重试 ({retry + 1}/3)... 错误: {ex.Message}");
                        if (_useInputSimulator && ex.Message.Contains("not sent successfully"))
                        {
                            Console.WriteLine("[GSI] InputSimulator权限问题，切换到SendInput...");
                            _useInputSimulator = false;
                            _useSendInput = true;
                        }
                        else if (_useSendInput && ex.Message.Contains("not sent successfully"))
                        {
                            Console.WriteLine("[GSI] SendInput权限问题，切换到keybd_event...");
                            _useSendInput = false;
                        }

                        Thread.Sleep(_configManager.Config.KeyDelay);
                    }
                    else throw;
                }
            }

            Thread.Sleep(_configManager.Config.KeyDelay);
            SendEnterKey();
            Console.WriteLine("[GSI] 发送完成。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GSI] 发送出错: {ex.Message}");
            if (ex.Message.Contains("not sent successfully"))
            {
                Console.WriteLine("\n[GSI] 解决建议：");
                Console.WriteLine("1. 以管理员身份运行。");
                Console.WriteLine("2. 按I切换模拟方式。");
                Console.WriteLine("3. 确保CS2无更高权限。");
                if (_useInputSimulator)
                {
                    _useInputSimulator = false;
                    _useSendInput = true;
                    Console.WriteLine("[GSI] 切换到SendInput。");
                }
                else if (_useSendInput)
                {
                    _useSendInput = false;
                    Console.WriteLine("[GSI] 切换到keybd_event。");
                }
            }
        }
    }

    private static void OpenChatBox()
    {
        if (_useInputSimulator)
        {
            try
            {
                if (_configManager.Config.ChatKey == "enter") _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                else
                {
                    var virtualKey = GetVirtualKeyFromChar(_configManager.Config.ChatKey.ToLower()[0]);
                    if (virtualKey != VirtualKeyCode.NONAME) _inputSimulator.Keyboard.KeyPress(virtualKey);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GSI] InputSimulator失败: {ex.Message}, 尝试SendInput...");
                _useInputSimulator = false;
                _useSendInput = true;
                OpenChatBox();
            }
        }
        else if (_useSendInput)
        {
            if (_configManager.Config.ChatKey == "enter") SendKeySendInput(0x0D);
            else SendKeySendInput((byte)VkKeyScan(_configManager.Config.ChatKey.ToLower()[0]));
        }
        else if (_useWinFormSendKeys)
        {
            SendKeys.SendWait(_configManager.Config.ChatKey == "enter" ? "{ENTER}" : _configManager.Config.ChatKey);
        }
        else
        {
            if (_configManager.Config.ChatKey == "enter") SendKeyNative(0x0D);
            else SendKeyNative((byte)VkKeyScan(_configManager.Config.ChatKey.ToLower()[0]));
        }
    }

    private static void ClearChatInput()
    {
        if (_useInputSimulator)
        {
            try
            {
                _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A);
                Thread.Sleep(_configManager.Config.KeyDelay / 2);
                _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.DELETE);
                Thread.Sleep(_configManager.Config.KeyDelay / 2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GSI] InputSimulator清除失败: {ex.Message}, 尝试SendInput...");
                _useInputSimulator = false;
                _useSendInput = true;
                ClearChatInput();
            }
        }
        else if (_useSendInput)
        {
            SelectAllTextSendInput();
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
            SendKeySendInput(0x2E);
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
        }
        else if (_useWinFormSendKeys)
        {
            SendKeys.SendWait("^a");
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
            SendKeys.SendWait("{DEL}");
        }
        else
        {
            SelectAllTextNative();
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
            SendKeyNative(0x2E);
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
        }
    }

    private static void PasteFromClipboard()
    {
        if (_useInputSimulator) _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
        else if (_useSendInput) PasteFromClipboardSendInput();
        else if (_useWinFormSendKeys)
        {
            SendKeys.SendWait("^v");
        }
        else PasteFromClipboardNative();
    }

    private static void SendEnterKey()
    {
        if (_useInputSimulator)
        {
            try
            {
                _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GSI] InputSimulator回车失败: {ex.Message}, 尝试SendInput...");
                _useInputSimulator = false;
                _useSendInput = true;
                SendKeySendInput(0x0D);
            }
        }
        else if (_useSendInput)
        {
            SendKeySendInput(0x0D);
        }
        else if (_useWinFormSendKeys)
        {
            SendKeys.SendWait("{ENTER}");
        }
        else
        {
            SendKeyNative(0x0D);
        }
    }

    private static void ReleaseAllKeys()
    {
        if (_useWinFormSendKeys) return;
        if (_useInputSimulator)
        {
            try
            {
                VirtualKeyCode[] keysToRelease =
                [
                    VirtualKeyCode.VK_W, VirtualKeyCode.VK_A, VirtualKeyCode.VK_S, VirtualKeyCode.VK_D,
                    VirtualKeyCode.SPACE, VirtualKeyCode.LSHIFT, VirtualKeyCode.RSHIFT,
                    VirtualKeyCode.LCONTROL, VirtualKeyCode.RCONTROL, VirtualKeyCode.LMENU, VirtualKeyCode.RMENU,
                    VirtualKeyCode.LBUTTON, VirtualKeyCode.RBUTTON
                ];
                foreach (var key in keysToRelease) _inputSimulator.Keyboard.KeyUp(key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GSI] InputSimulator释放失败: {ex.Message}, 尝试SendInput...");
                _useInputSimulator = false;
                _useSendInput = true;
                ReleaseAllKeysSendInput();
            }
        }
        else if (_useSendInput)
        {
            ReleaseAllKeysSendInput();
        }
        else
        {
            ReleaseAllKeysNative();
        }

        Thread.Sleep(_configManager.Config.KeyDelay);
    }

    private static void ReleaseAllKeysNative()
    {
        byte[] keysToRelease = [0x57, 0x41, 0x53, 0x44, 0x20, 0x10, 0x11, 0x12, 0x01, 0x02];
        foreach (var key in keysToRelease) keybd_event(key, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    private static void SelectAllTextNative()
    {
        keybd_event(0x11, 0, 0, UIntPtr.Zero);
        Thread.Sleep(_configManager.Config.KeyDelay / 2);
        keybd_event(0x41, 0, 0, UIntPtr.Zero);
        Thread.Sleep(_configManager.Config.KeyDelay / 2);
        keybd_event(0x41, 0, KeyeventfKeyup, UIntPtr.Zero);
        Thread.Sleep(_configManager.Config.KeyDelay / 2);
        keybd_event(0x11, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    private static void PasteFromClipboardNative()
    {
        keybd_event(0x11, 0, 0, UIntPtr.Zero);
        Thread.Sleep(50);
        keybd_event(0x56, 0, 0, UIntPtr.Zero);
        Thread.Sleep(20);
        keybd_event(0x56, 0, KeyeventfKeyup, UIntPtr.Zero);
        Thread.Sleep(50);
        keybd_event(0x11, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    private static void SendKeyNative(byte keyCode)
    {
        keybd_event(keyCode, 0, 0, UIntPtr.Zero);
        Thread.Sleep(20);
        keybd_event(keyCode, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_SCANCODE = 0x0008;

    private static void SendKeySendInput(byte keyCode)
    {
        INPUT[] inputs = new INPUT[2];

        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = keyCode,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = keyCode,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        if (result != inputs.Length)
        {
            throw new Exception($"SendInput failed with error code: {Marshal.GetLastWin32Error()}");
        }
        Thread.Sleep(20);
    }

    private static void ReleaseAllKeysSendInput()
    {
        byte[] keysToRelease = [0x57, 0x41, 0x53, 0x44, 0x20, 0x10, 0x11, 0x12, 0x01, 0x02];
        foreach (var key in keysToRelease)
        {
            INPUT[] inputs = new INPUT[1]
            {
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new INPUTUNION
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = key,
                            wScan = 0,
                            dwFlags = KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                }
            };
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }

    private static void SelectAllTextSendInput()
    {
        INPUT[] inputs = new INPUT[4];

        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x11,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x41,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[2] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x41,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[3] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x11,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        Thread.Sleep(_configManager.Config.KeyDelay / 2);
    }

    private static void PasteFromClipboardSendInput()
    {
        INPUT[] inputs = new INPUT[4];

        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x11,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x56,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[2] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x56,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[3] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x11,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        Thread.Sleep(50);
    }

    private static VirtualKeyCode GetVirtualKeyFromChar(char character)
    {
        return character switch
        {
            'a' => VirtualKeyCode.VK_A, 'b' => VirtualKeyCode.VK_B, 'c' => VirtualKeyCode.VK_C,
            'd' => VirtualKeyCode.VK_D, 'e' => VirtualKeyCode.VK_E, 'f' => VirtualKeyCode.VK_F,
            'g' => VirtualKeyCode.VK_G, 'h' => VirtualKeyCode.VK_H, 'i' => VirtualKeyCode.VK_I,
            'j' => VirtualKeyCode.VK_J, 'k' => VirtualKeyCode.VK_K, 'l' => VirtualKeyCode.VK_L,
            'm' => VirtualKeyCode.VK_M, 'n' => VirtualKeyCode.VK_N, 'o' => VirtualKeyCode.VK_O,
            'p' => VirtualKeyCode.VK_P, 'q' => VirtualKeyCode.VK_Q, 'r' => VirtualKeyCode.VK_R,
            's' => VirtualKeyCode.VK_S, 't' => VirtualKeyCode.VK_T, 'u' => VirtualKeyCode.VK_U,
            'v' => VirtualKeyCode.VK_V, 'w' => VirtualKeyCode.VK_W, 'x' => VirtualKeyCode.VK_X,
            'y' => VirtualKeyCode.VK_Y, 'z' => VirtualKeyCode.VK_Z,
            '0' => VirtualKeyCode.VK_0, '1' => VirtualKeyCode.VK_1, '2' => VirtualKeyCode.VK_2,
            '3' => VirtualKeyCode.VK_3, '4' => VirtualKeyCode.VK_4, '5' => VirtualKeyCode.VK_5,
            '6' => VirtualKeyCode.VK_6, '7' => VirtualKeyCode.VK_7, '8' => VirtualKeyCode.VK_8,
            '9' => VirtualKeyCode.VK_9,
            _ => VirtualKeyCode.NONAME
        };
    }

    private static void AddNewMessage()
    {
        Console.Write("\n输入新死亡消息 (Enter确认): ");
        var newMessage = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(newMessage))
        {
            _chatManager.AddMessage(newMessage);
            _chatManager.SaveMessages();
            Console.WriteLine($"已添加: {newMessage}");
        }
        else
        {
            Console.WriteLine("消息不能为空。");
        }

        UpdateCfgCount();
    }

    private static void DeleteMessage()
    {
        Console.WriteLine("\n消息列表：");
        _chatManager.DisplayMessages();
        if (_chatManager.GetAllMessages().Count == 0) return;

        Console.Write("输入要删除的消息编号: ");
        if (int.TryParse(Console.ReadLine(), out var index))
        {
            if (_chatManager.RemoveMessage(index - 1))
            {
                _chatManager.SaveMessages();
                Console.WriteLine("消息已删除。");
            }
            else
            {
                Console.WriteLine("无效编号。");
            }
        }
        else
        {
            Console.WriteLine("请输入数字。");
        }

        UpdateCfgCount();
    }

    private static void ChangeChatKey()
    {
        Console.WriteLine($"\n当前聊天键: {_configManager.Config.ChatKey}");
        Console.WriteLine("1. Y-全局 | 2. U-队内 | 3. Enter | 4. 自定义");
        Console.Write("选择 (1-4): ");

        var choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                _configManager.Config.ChatKey = "y";
                Console.WriteLine("设为全局聊天 (Y)");
                break;
            case "2":
                _configManager.Config.ChatKey = "u";
                Console.WriteLine("设为队内聊天 (U)");
                break;
            case "3":
                _configManager.Config.ChatKey = "enter";
                Console.WriteLine("设为回车键");
                break;
            case "4":
                Console.Write("输入自定义键 (单字符): ");
                var customKey = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(customKey) && customKey.Length == 1 &&
                    char.IsLetterOrDigit(customKey[0]))
                {
                    _configManager.Config.ChatKey = customKey.ToLower();
                    Console.WriteLine($"设为: {customKey.ToUpper()}");
                }
                else
                {
                    Console.WriteLine("无效输入，需单字母/数字。");
                    return;
                }

                break;
            default:
                Console.WriteLine("无效选择。");
                return;
        }

        _configManager.SaveConfig();
        Console.WriteLine("聊天键已保存。");
    }

    [LibraryImport("user32.dll")]
    private static partial void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern short VkKeyScan(char ch);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    private const uint KeyeventfKeyup = 0x0002;

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial uint GetWindowThreadProcessId(nint hWnd, out int lpdwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        int dwProcessId);

    [LibraryImport("psapi.dll", SetLastError = true)]
    private static unsafe partial uint GetModuleFileNameExW(nint hProcess, nint hModule, char* lpFilename, uint nSize);

    [LibraryImport("kernel32.dll")]
    private static partial uint CloseHandle(nint hObject);

    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_VM_READ = 0x0010;

    private static unsafe bool IsCS2Active()
    {
        if (GetWindowThreadProcessId(GetForegroundWindow(), out var pid) == 0)
        {
            return false;
        }

        var hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
        if (hProc == 0)
        {
            return false;
        }

        var sProcPath = stackalloc char[32767];
        if (GetModuleFileNameExW(hProc, nint.Zero, sProcPath, 32767) == 0)
        {
            return false;
        }

        _ = CloseHandle(hProc);
        return Path.GetFileName(new string(sProcPath)).Equals("cs2.exe", StringComparison.InvariantCultureIgnoreCase);
    }
}