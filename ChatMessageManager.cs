using System.Text.Json;

namespace ImLag;

public class ChatMessageManager
{
    private List<string> _messages = [];
    private const string ConfigFile = "Messages.json";
    private readonly Random _random = new();

    public void LoadMessages()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                var loadedMessages = JsonSerializer.Deserialize<List<string>>(json);
                if (loadedMessages != null)
                {
                    _messages = loadedMessages;
                    Console.WriteLine($"已加载 {_messages.Count} 条死亡消息。");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载消息文件时出错: {ex.Message}");
        }

        LoadDefaultMessages();
        SaveMessages();
    }

    private void LoadDefaultMessages()
    {
        _messages =
        [
            "网卡",
            "手抖",
            "高延迟",
            "鼠标出问题了",
            "瓶颈期",
            "手冻僵了",
            "被阴了",
            "卡输入法了",
            "day0了",
            "掉帧了",
            "手汗手滑",
            "腱鞘炎犯了",
            "吞子弹了",
            "timing侠",
            "唉，资本",
            "刚打瓦回来不适应",
            "灵敏度有问题",
            "谁把我键位改了",
            "感冒了没反应",
            "拆消音器去了",
            "校园网是这样的",
            "状态不行",
            "鼠标撞键盘上了",
            "复健",
            "屏幕太小"
        ];
        Console.WriteLine("已加载默认死亡消息。");
    }

    public void SaveMessages()
    {
        try
        {
            var json = JsonSerializer.Serialize(_messages, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(ConfigFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存消息文件时出错: {ex.Message}");
        }
    }

    public string GetRandomMessage()
    {
        if (_messages.Count == 0)
            return string.Empty;

        var index = _random.Next(_messages.Count);
        return _messages[index];
    }

    public void AddMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message) && !_messages.Contains(message))
        {
            _messages.Add(message);
        }
    }

    public bool RemoveMessage(int index)
    {
        if (index < 0 || index >= _messages.Count) return false;
        _messages.RemoveAt(index);
        return true;
    }

    public void DisplayMessages()
    {
        if (_messages.Count == 0)
        {
            Console.WriteLine("消息列表为空。");
            return;
        }

        for (var i = 0; i < _messages.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {_messages[i]}");
        }
    }
}