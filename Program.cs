using System.Runtime.InteropServices;
using CounterStrike2GSI;
using CounterStrike2GSI.EventMessages;
using TextCopy;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 'required' 修饰符或声明为可以为 null。

namespace ImLag
{
    internal static partial class Program
    {
        private static GameStateListener? _gsl;
        private static ChatMessageManager _chatManager;
        private static ConfigManager _configManager;
        private static string Author = "Eicy";
        private static string Version = "1.0.2";
        private static void Main()
        {
            _configManager = new ConfigManager();
            _configManager.LoadConfig();
            Console.Title = $"ImLag - by {Author} Version {Version}";
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

            Console.WriteLine("正在监听CS2游戏事件...");
            Console.WriteLine($"当前用户玩家名: {_configManager.Config.UserPlayerName}");
            Console.WriteLine("只有你死亡时才会发送消息");
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
            Console.WriteLine("当前死亡消息列表：");
            _chatManager.DisplayMessages();
            Console.WriteLine("控制按键说明：");
            Console.WriteLine("ESC - 退出程序");
            Console.WriteLine("A - 添加新消息");
            Console.WriteLine("D - 删除消息");
            Console.WriteLine("L - 显示当前消息列表");
            Console.WriteLine("C - 更改聊天按键设置");
            Console.WriteLine("P - 更改玩家名设置");
        }

        private static void OnPlayerDied(PlayerDied gameEvent)
        {
            Console.WriteLine($"检测到玩家死亡: {gameEvent.Player.Name}");

            // fix只有当死亡的玩家是用户自己时才发送消息
            if (gameEvent.Player.Name != _configManager.Config.UserPlayerName)
            {
                Console.WriteLine("非本人死亡，跳过发送消息。");
                return;
            }

            Console.WriteLine("检测到你死亡了！");
            var randomMessage = _chatManager.GetRandomMessage();
            if (!string.IsNullOrEmpty(randomMessage))
            {
                SendChatMessage(randomMessage);
                Console.WriteLine($"已发送聊天消息: {randomMessage}");
            }
            else
            {
                Console.WriteLine("消息列表为空，请添加一些消息。");
            }
        }

        private static void SendChatMessage(string message)
        {
            try
            {
                if (!IsCS2Active())
                {
                    Console.WriteLine("CS2不是当前活动窗口，跳过发送消息。");
                    return;
                }

                Console.WriteLine($"准备发送消息: {message}");

                // 将消息复制到剪贴板
                ClipboardService.SetText(message);

                // 根据配置的聊天按键打开聊天
                if (_configManager.Config.ChatKey == "enter")
                {
                    // 直接按回车键进入聊天
                    SendKey(0x0D); // Enter键
                }
                else
                {
                    // 使用其他聊天按键
                    var chatKey = _configManager.Config.ChatKey.ToLower()[0];
                    var keyCode = (byte)VkKeyScan(chatKey);
                    SendKey(keyCode);
                }

                Thread.Sleep(150); // 等待聊天框打开

                PasteFromClipboard();
                Thread.Sleep(100); // 等待粘贴完成

                SendKey(0x0D); // Enter键发送消息

                Console.WriteLine("消息发送完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送聊天消息时出错: {ex.Message}");
            }
        }

        private static void PasteFromClipboard()
        {
            keybd_event(0x11, 0, 0, UIntPtr.Zero); // Ctrl按下
            Thread.Sleep(50);

            keybd_event(0x56, 0, 0, UIntPtr.Zero); // V按下
            Thread.Sleep(20);
            keybd_event(0x56, 0, KeyeventfKeyup, UIntPtr.Zero); // V释放

            Thread.Sleep(50);
            keybd_event(0x11, 0, KeyeventfKeyup, UIntPtr.Zero); // Ctrl释放
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

        private static void SendKey(byte keyCode)
        {
            keybd_event(keyCode, 0, 0, UIntPtr.Zero); // 按下
            Thread.Sleep(20);
            keybd_event(keyCode, 0, KeyeventfKeyup, UIntPtr.Zero); // 释放
        }

        // ReSharper disable once InconsistentNaming
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