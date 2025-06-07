using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CounterStrike2GSI;
using CounterStrike2GSI.EventMessages;
using TextCopy;
using WindowsInput;
using WindowsInput.Native;
// ReSharper disable InconsistentNaming

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 'required' 修饰符或声明为可以为 null。

namespace ImLag
{
    [SuppressMessage("Interoperability", "CA1416:验证平台兼容性")]
    internal static partial class Program
    {
        private const string Version = "1.0.7"; 
        private const string Author = "Eicy";
        private const string Name = "ImLag";
        private const string UpdateLog = "修复 InputSimulator 权限问题，增加回退机制。";
        
        private static GameStateListener? _gsl;
        private static ChatMessageManager _chatManager;
        private static ConfigManager _configManager;
        private static readonly InputSimulator _inputSimulator = new();
        private static bool _useInputSimulator = true;

        private static void Main()
        {
            Console.Title = $"{Name} v{Version} by {Author}";
            
            if (!IsRunningAsAdministrator())
            {
                Console.WriteLine("警告：程序不是以管理员身份运行。");
                Console.WriteLine("如果遇到按键发送问题，请尝试以管理员身份重新运行程序。");
                Console.WriteLine("按任意键继续...");
                Console.ReadKey();
                Console.Clear();
            }
            
            _configManager = new ConfigManager();
            _configManager.LoadConfig();
            
            if (string.IsNullOrWhiteSpace(_configManager.Config.UserPlayerName))
            {
                SetupPlayerName();
            }

            _chatManager = new ChatMessageManager();
            _chatManager.LoadMessages();

            _gsl = new GameStateListener(4000);

            if (!_gsl.GenerateGSIConfigFile("ImLag"))
            {
                Console.WriteLine("无法生成GSI配置文件。");
            }

            _gsl.PlayerDied += OnPlayerDied;

            if (!_gsl.Start())
            {
                Console.WriteLine("GameStateListener启动失败。请尝试以管理员身份运行程序。");
                Console.ReadLine();
                Environment.Exit(0);
            }

            Console.WriteLine($"=== {Name} v{Version} by {Author} ===");
            Console.WriteLine("正在监听CS2游戏事件...");
            Console.WriteLine($"当前用户玩家名: {_configManager.Config.UserPlayerName}");
            Console.WriteLine($"当前监听模式: {(_configManager.Config.OnlySelfDeath ? "仅监听自己死亡" : "监听所有玩家死亡")}");
            Console.WriteLine($"窗口检测: {(_configManager.Config.SkipWindowCheck ? "已禁用" : "已启用")}");
            Console.WriteLine($"强制发送模式: {(_configManager.Config.ForceMode ? "已启用" : "已禁用")}");
            Console.WriteLine($"按键延迟: {_configManager.Config.KeyDelay}毫秒");
            Console.WriteLine($"按键模拟方式: {(_useInputSimulator ? "InputSimulator" : "原生 Win32 API")}");
            Console.WriteLine();
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
                        Console.WriteLine("当前消息列表：");
                        _chatManager.DisplayMessages();
                        break;
                    case ConsoleKey.C:
                        ChangeChatKey();
                        break;
                    case ConsoleKey.P:
                        ChangePlayerName();
                        break;
                    case ConsoleKey.M:
                        ToggleMonitorMode();
                        break;
                    case ConsoleKey.W:
                        ToggleWindowCheck();
                        break;
                    case ConsoleKey.F:
                        ToggleForceMode();
                        break;
                    case ConsoleKey.K:
                        ChangeKeyDelay();
                        break;
                    case ConsoleKey.V:
                        ShowVersionInfo();
                        break;
                    case ConsoleKey.I:
                        ToggleInputMethod();
                        break;
                    default:
                        Console.WriteLine("你在干什莫？");
                        Init();
                        break;
                }
            } while (keyInfo.Key != ConsoleKey.Escape);

            _configManager.SaveConfig();
            _chatManager.SaveMessages();
            Console.WriteLine("程序已退出。");
        }

        private static void ToggleInputMethod()
        {
            _useInputSimulator = !_useInputSimulator;
            Console.WriteLine($"\n按键模拟方式已切换为: {(_useInputSimulator ? "InputSimulator" : "原生 Win32 API")}");
            
            if (!_useInputSimulator)
            {
                Console.WriteLine("已切换到原生 Win32 API 模式，这可能更兼容某些系统配置。");
            }
            else
            {
                Console.WriteLine("已切换到 InputSimulator 模式，这通常更稳定。");
                Console.WriteLine("如果遇到权限错误，请以管理员身份运行程序。");
            }
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
                Console.WriteLine("警告：强制模式会多次尝试发送消息，可能导致重复发送！");
                Console.WriteLine("建议：仅在消息经常发送失败时启用此选项。");
            }
            else
            {
                Console.WriteLine("已恢复正常发送模式。");
            }
        }

        private static void ChangeKeyDelay()
        {
            Console.WriteLine($"\n当前按键延迟: {_configManager.Config.KeyDelay}毫秒");
            Console.WriteLine("请输入新的按键延迟时间 (30-500毫秒):");
            
            if (int.TryParse(Console.ReadLine(), out var delay) && delay is >= 30 and <= 500)
            {
                _configManager.Config.KeyDelay = delay;
                _configManager.SaveConfig();
                Console.WriteLine($"按键延迟已更新为: {delay}毫秒");
            }
            else
            {
                Console.WriteLine("无效输入，延迟时间应该在30-500毫秒之间。");
            }
        }

        private static void ToggleMonitorMode()
        {
            _configManager.Config.OnlySelfDeath = !_configManager.Config.OnlySelfDeath;
            _configManager.SaveConfig();
            
            Console.WriteLine($"\n监听模式已切换为: {(_configManager.Config.OnlySelfDeath ? "仅监听自己死亡" : "监听所有玩家死亡")}");
            Console.WriteLine($"现在{(_configManager.Config.OnlySelfDeath ? "只有你死亡时" : "任何玩家死亡时")}都会发送消息。");
        }
        
        private static void ToggleWindowCheck()
        {
            _configManager.Config.SkipWindowCheck = !_configManager.Config.SkipWindowCheck;
            _configManager.SaveConfig();
            
            Console.WriteLine($"\n窗口检测已{(_configManager.Config.SkipWindowCheck ? "禁用" : "启用")}");
            if (_configManager.Config.SkipWindowCheck)
            {
                Console.WriteLine("警告：禁用窗口检测后，即使CS2不是活动窗口也会发送消息！");
                Console.WriteLine("建议：仅在使用5E对战平台且窗口标题检测失败时启用此选项。");
            }
            else
            {
                Console.WriteLine("已恢复窗口检测，只有CS2窗口活动时才会发送消息。");
            }
        }

        private static void ShowVersionInfo()
        {
            Console.WriteLine($"\n=== {Name} v{Version} ===");
            Console.WriteLine($"作者: {Author}");
            Console.WriteLine("GitHub: https://github.com/cneicy/ImLag");
            Console.WriteLine($"{UpdateLog}");
        }

        private static void SetupPlayerName()
        {
            Console.WriteLine("=== 首次启动设置 ===");
            Console.WriteLine("请输入你的CS2游戏内玩家名:");
            Console.WriteLine("注意：必须与游戏内显示的玩家名完全一致（包括大小写）");
            Console.WriteLine();

            while (true)
            {
                Console.Write("请输入你的玩家名: ");
                var playerName = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(playerName))
                {
                    _configManager.Config.UserPlayerName = playerName.Trim();
                    _configManager.SaveConfig();
                    Console.WriteLine($"玩家名设置成功: {playerName}");
                    Console.WriteLine();
                    break;
                }

                Console.WriteLine("玩家名不能为空，请重新输入。");
            }
        }

        private static void ChangePlayerName()
        {
            Console.WriteLine($"\n当前玩家名: {_configManager.Config.UserPlayerName}");
            Console.WriteLine("请输入新的玩家名:");
            
            Console.Write("请输入新的玩家名（直接按Enter取消）: ");
            var playerName = Console.ReadLine();

            if (string.IsNullOrEmpty(playerName))
            {
                Console.WriteLine("已取消修改。");
                return;
            }

            _configManager.Config.UserPlayerName = playerName.Trim();
            _configManager.SaveConfig();
            Console.WriteLine($"玩家名已更新: {playerName}");
        }

        private static void Init()
        {
            var chatKeyDescription = _configManager.Config.ChatKey switch
            {
                "y" => "全局聊天",
                "u" => "队内聊天", 
                "enter" => "回车键",
                _ => "自定义"
            };

            Console.WriteLine($"当前聊天按键: {_configManager.Config.ChatKey} ({chatKeyDescription})");
            Console.WriteLine($"当前监听模式: {(_configManager.Config.OnlySelfDeath ? "仅监听自己死亡" : "监听所有玩家死亡")}");
            Console.WriteLine($"窗口检测: {(_configManager.Config.SkipWindowCheck ? "已禁用" : "已启用")}");
            Console.WriteLine($"强制发送模式: {(_configManager.Config.ForceMode ? "已启用" : "已禁用")}");
            Console.WriteLine($"按键延迟: {_configManager.Config.KeyDelay}毫秒");
            Console.WriteLine($"按键模拟方式: {(_useInputSimulator ? "InputSimulator" : "原生 Win32 API")}");
            Console.WriteLine("当前死亡消息列表：");
            _chatManager.DisplayMessages();
            Console.WriteLine("控制按键说明：");
            Console.WriteLine("ESC - 退出程序");
            Console.WriteLine("A - 添加新消息");
            Console.WriteLine("D - 删除消息");
            Console.WriteLine("L - 显示当前消息列表");
            Console.WriteLine("C - 更改聊天按键设置");
            Console.WriteLine("P - 更改玩家名设置");
            Console.WriteLine("M - 切换监听模式");
            Console.WriteLine("W - 切换窗口检测(5E平台或其他情况请尝试关闭窗口检测)");
            Console.WriteLine("F - 切换强制发送模式(消息发送不稳定时尝试)");
            Console.WriteLine("K - 调整按键延迟设置");
            Console.WriteLine("V - 显示版本信息");
            Console.WriteLine("I - 切换按键模拟方式");
        }

        private static void OnPlayerDied(PlayerDied gameEvent)
        {
            Console.WriteLine($"检测到玩家死亡: {gameEvent.Player.Name}");
            
            if (_configManager.Config.OnlySelfDeath && gameEvent.Player.Name != _configManager.Config.UserPlayerName)
            {
                Console.WriteLine("当前为仅监听自己死亡模式，跳过发送消息。");
                return;
            }
            
            Console.WriteLine(gameEvent.Player.Name == _configManager.Config.UserPlayerName
                ? "检测到你死了！"
                : $"检测到队友 {gameEvent.Player.Name} 死亡！");

            var randomMessage = _chatManager.GetRandomMessage();
            if (!string.IsNullOrEmpty(randomMessage))
            {
                SendChatMessage(randomMessage);
                Console.WriteLine($"已发送聊天消息: {randomMessage}");
            }
            else
            {
                Console.WriteLine("消息列表为空，请添加消息。");
            }
        }

        private static void SendChatMessage(string message)
        {
            try
            {
                if (!_configManager.Config.SkipWindowCheck && !IsCS2Active())
                {
                    Console.WriteLine("CS2不是当前活动窗口，跳过发送消息。");
                    return;
                }

                if (_configManager.Config.SkipWindowCheck)
                {
                    Console.WriteLine("已跳过窗口检测，强制发送消息。");
                }

                Console.WriteLine($"准备发送消息: {message}");
                Console.WriteLine($"使用按键模拟方式: {(_useInputSimulator ? "InputSimulator" : "原生 Win32 API")}");

                // 将消息复制到剪贴板
                ClipboardService.SetText(message);
                
                // 释放可能按着的按键
                ReleaseAllKeys();
                
                // 等待一段时间确保游戏已准备好接收输入
                Thread.Sleep(300);

                // 根据配置的聊天按键打开聊天
                if (_configManager.Config.ForceMode)
                {
                    // 强制模式下，多次尝试打开聊天框
                    for (var i = 0; i < 3; i++)
                    {
                        OpenChatBox();
                        Thread.Sleep(_configManager.Config.KeyDelay);
                    }
                }
                else
                {
                    // 正常模式
                    OpenChatBox();
                }
                
                // 增加等待聊天框打开的时间
                Thread.Sleep(_configManager.Config.KeyDelay * 2);
                
                // 清除输入框内容 (Ctrl+A + Delete)
                ClearChatInput();
                Thread.Sleep(_configManager.Config.KeyDelay);

                // 尝试粘贴操作，失败时重试
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
                            Console.WriteLine($"粘贴失败，正在重试 ({retry + 1}/3)...");
                            Console.WriteLine($"错误详情: {ex.Message}");
                            
                            // 如果 InputSimulator 失败，尝试切换到原生 API
                            if (_useInputSimulator && ex.Message.Contains("not sent successfully"))
                            {
                                Console.WriteLine("InputSimulator 遇到权限问题，自动切换到原生 Win32 API...");
                                _useInputSimulator = false;
                            }
                            
                            Thread.Sleep(_configManager.Config.KeyDelay);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                
                Thread.Sleep(_configManager.Config.KeyDelay);
                
                SendEnterKey();

                Console.WriteLine("消息发送完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送聊天消息时出错: {ex.Message}");
                
                if (ex.Message.Contains("not sent successfully"))
                {
                    Console.WriteLine("\n解决建议：");
                    Console.WriteLine("1. 以管理员身份重新运行此程序");
                    Console.WriteLine("2. 按 I 键切换到原生 Win32 API 模式");
                    Console.WriteLine("3. 确保CS2游戏没有以更高权限运行");
                    _useInputSimulator = false; // 自动切换到原生模式
                    Console.WriteLine("已自动切换到原生 Win32 API 模式。");
                }
            }
        }

        private static void OpenChatBox()
        {
            if (_useInputSimulator)
            {
                try
                {
                    if (_configManager.Config.ChatKey == "enter")
                    {
                        _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    }
                    else
                    {
                        var chatKey = _configManager.Config.ChatKey.ToLower()[0];
                        var virtualKey = GetVirtualKeyFromChar(chatKey);
                        if (virtualKey != VirtualKeyCode.NONAME)
                        {
                            _inputSimulator.Keyboard.KeyPress(virtualKey);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"InputSimulator 发送按键失败: {ex.Message}");
                    _useInputSimulator = false;
                    OpenChatBox(); // 递归调用，使用原生 API
                }
            }
            else
            {
                // 使用原生 Win32 API
                if (_configManager.Config.ChatKey == "enter")
                {
                    SendKeyNative(0x0D); // Enter
                }
                else
                {
                    var chatKey = _configManager.Config.ChatKey.ToLower()[0];
                    var keyCode = (byte)VkKeyScan(chatKey);
                    SendKeyNative(keyCode);
                }
            }
        }

        private static void ClearChatInput()
        {
            if (_useInputSimulator)
            {
                try
                {
                    // Ctrl+A 选择全部
                    _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A);
                    Thread.Sleep(_configManager.Config.KeyDelay / 2);
                    
                    // Delete 删除选中内容
                    _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.DELETE);
                    Thread.Sleep(_configManager.Config.KeyDelay / 2);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"InputSimulator 清除输入失败: {ex.Message}");
                    _useInputSimulator = false;
                    ClearChatInput(); // 递归调用，使用原生 API
                }
            }
            else
            {
                // 使用原生 Win32 API
                SelectAllTextNative();
                Thread.Sleep(_configManager.Config.KeyDelay / 2);
                SendKeyNative(0x2E); // Delete
                Thread.Sleep(_configManager.Config.KeyDelay / 2);
            }
        }

        private static void PasteFromClipboard()
        {
            if (_useInputSimulator)
            {
                // Ctrl+V 粘贴
                _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
            }
            else
            {
                // 使用原生 Win32 API
                PasteFromClipboardNative();
            }
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
                    Console.WriteLine($"InputSimulator 发送回车失败: {ex.Message}");
                    SendKeyNative(0x0D); // 回退到原生 API
                }
            }
            else
            {
                SendKeyNative(0x0D); // Enter
            }
        }

        private static void ReleaseAllKeys()
        {
            if (_useInputSimulator)
            {
                try
                {
                    var keysToRelease = new[]
                    {
                        VirtualKeyCode.VK_W, VirtualKeyCode.VK_A, VirtualKeyCode.VK_S, VirtualKeyCode.VK_D,
                        VirtualKeyCode.SPACE, VirtualKeyCode.SHIFT, VirtualKeyCode.CONTROL, 
                        VirtualKeyCode.MENU, VirtualKeyCode.LBUTTON
                    };
                    
                    foreach (var key in keysToRelease)
                    {
                        _inputSimulator.Keyboard.KeyUp(key);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"InputSimulator 释放按键失败: {ex.Message}");
                    _useInputSimulator = false;
                    ReleaseAllKeysNative(); // 回退到原生 API
                }
            }
            else
            {
                ReleaseAllKeysNative();
            }
            
            Thread.Sleep(_configManager.Config.KeyDelay);
        }
        
        private static void ReleaseAllKeysNative()
        {
            byte[] keysToRelease =
            [
                0x57, // W
                0x41, // A
                0x53, // S
                0x44, // D
                0x20, // 空格
                0x10, // Shift
                0x11, // Ctrl
                0x12, // Alt
                0x01  // 鼠标左键
            ];
            
            foreach (var key in keysToRelease)
            {
                keybd_event(key, 0, KeyeventfKeyup, UIntPtr.Zero);
            }
        }

        private static void SelectAllTextNative()
        {
            // Press Ctrl
            keybd_event(0x11, 0, 0, UIntPtr.Zero);
            Thread.Sleep(_configManager.Config.KeyDelay / 2);

            // Press A
            keybd_event(0x41, 0, 0, UIntPtr.Zero);
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
            keybd_event(0x41, 0, KeyeventfKeyup, UIntPtr.Zero); // Release A

            Thread.Sleep(_configManager.Config.KeyDelay / 2);

            // Release Ctrl
            keybd_event(0x11, 0, KeyeventfKeyup, UIntPtr.Zero);
        }

        private static void PasteFromClipboardNative()
        {
            keybd_event(0x11, 0, 0, UIntPtr.Zero); // Ctrl按下
            Thread.Sleep(50);

            keybd_event(0x56, 0, 0, UIntPtr.Zero); // V按下
            Thread.Sleep(20);
            keybd_event(0x56, 0, KeyeventfKeyup, UIntPtr.Zero); // V释放

            Thread.Sleep(50);
            keybd_event(0x11, 0, KeyeventfKeyup, UIntPtr.Zero); // Ctrl释放
        }

        private static void SendKeyNative(byte keyCode)
        {
            keybd_event(keyCode, 0, 0, UIntPtr.Zero); // 按下
            Thread.Sleep(20);
            keybd_event(keyCode, 0, KeyeventfKeyup, UIntPtr.Zero); // 释放
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
                _ => VirtualKeyCode.NONAME
            };
        }

        private static void AddNewMessage()
        {
            Console.WriteLine("\n请输入新的死亡消息 (按Enter确认):");
            var newMessage = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(newMessage))
            {
                _chatManager.AddMessage(newMessage);
                _chatManager.SaveMessages();
                Console.WriteLine($"已添加消息: {newMessage}");
            }
            else
            {
                Console.WriteLine("消息不能为空。");
            }
        }

        private static void DeleteMessage()
        {
            Console.WriteLine("\n当前消息列表：");
            _chatManager.DisplayMessages();

            Console.WriteLine("请输入要删除的消息编号:");
            if (int.TryParse(Console.ReadLine(), out var index))
            {
                if (_chatManager.RemoveMessage(index - 1))
                {
                    _chatManager.SaveMessages();
                    Console.WriteLine("消息已删除。");
                }
                else
                {
                    Console.WriteLine("无效的消息编号。");
                }
            }
            else
            {
                Console.WriteLine("请输入有效的数字。");
            }
        }

        private static void ChangeChatKey()
        {
            Console.WriteLine("\n当前聊天按键设置：");
            Console.WriteLine($"聊天按键: {_configManager.Config.ChatKey}");
            Console.WriteLine();
            Console.WriteLine("请选择聊天按键：");
            Console.WriteLine("1. Y - 全局聊天");
            Console.WriteLine("2. U - 队内聊天");
            Console.WriteLine("3. Enter - 回车键");
            Console.WriteLine("4. 自定义按键");
            Console.WriteLine("请输入选择 (1-4):");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    _configManager.Config.ChatKey = "y";
                    Console.WriteLine("已设置为全局聊天 (Y键)");
                    break;
                case "2":
                    _configManager.Config.ChatKey = "u";
                    Console.WriteLine("已设置为队内聊天 (U键)");
                    break;
                case "3":
                    _configManager.Config.ChatKey = "enter";
                    Console.WriteLine("已设置为回车键");
                    break;
                case "4":
                    Console.WriteLine("请输入自定义按键 (单个字符):");
                    var customKey = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(customKey) && customKey.Length == 1)
                    {
                        _configManager.Config.ChatKey = customKey.ToLower();
                        Console.WriteLine($"已设置为自定义按键: {customKey.ToUpper()}");
                    }
                    else
                    {
                        Console.WriteLine("无效输入。");
                        return;
                    }
                    break;
                default:
                    Console.WriteLine("无效选择。");
                    return;
            }

            _configManager.SaveConfig();
            Console.WriteLine("聊天按键设置已保存。");
        }
        
        [LibraryImport("user32.dll")]
        private static partial void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [LibraryImport("user32.dll")]
        private static partial IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        private const uint KeyeventfKeyup = 0x0002;

        private static bool IsCS2Active()
        {
            var foregroundWindow = GetForegroundWindow();
            var windowText = new System.Text.StringBuilder(256);
            GetWindowText(foregroundWindow, windowText, 256);
            if (windowText.ToString() == "反恐精英：全球攻势")
            {
                return true;
            }
            var title = windowText.ToString().ToLower();

            return title.Contains("counter-strike") || title.Contains("cs2") || title.Contains("csgo");
        }
    }
}