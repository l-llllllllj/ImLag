using Newtonsoft.Json;

namespace ImLag;

public static class UpdateHandler
{
    private const string JsonFilePath = "Messages.json";
    private const string TxtFilePath = "Messages.txt";

    public static void UpdateFrom2d0d5()
    {
        if (!File.Exists(JsonFilePath)) return;
        try
        {
            Console.WriteLine("正在尝试将语料更新到2.0.6版本匹配格式.");
            var jsonContent = File.ReadAllText(JsonFilePath);
            
            var messages = JsonConvert.DeserializeObject<string[]>(jsonContent);
            
            File.WriteAllLines(TxtFilePath, messages);
            
            Console.WriteLine($"成功将 {JsonFilePath} 转换为 {TxtFilePath}.");
            
            File.Delete(JsonFilePath);
            Console.WriteLine($"{JsonFilePath} 文件已被删除.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("出现错误: " + ex.Message);
        }
    }
}