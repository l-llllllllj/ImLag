using System.Collections.Generic;

namespace ImLag
{
    public class LanguageManager
    {
        // 简体中文到繁体中文的映射字典
        private static readonly Dictionary<string, string> SimplifiedToTraditional = new()
        {
            // 基础UI文本
            {"ImLag v", "ImLag v"},
            {"切换到CFG模式", "切換到CFG模式"},
            {"切换到聊天模式", "切換到聊天模式"},
            {"聊天模式设置", "聊天模式設定"},
            {"CFG模式设置", "CFG模式設定"},
            {"消息管理", "消息管理"},
            
            // 聊天模式控件
            {"聊天键: ", "聊天鍵: "},
            {"全局聊天 (Y)", "全局聊天 (Y)"},
            {"队内聊天 (U)", "隊內聊天 (U)"},
            {"回车 (Enter)", "回車 (Enter)"},
            {"自定义 (", "自定義 ("},
            {"仅自己死亡时发送", "僅自己死亡時發送"},
            {"跳过窗口检测", "跳過窗口檢測"},
            {"强制发送模式", "強制發送模式"},
            {"按键延迟: ", "按鍵延遲: "},
            {"毫秒", "毫秒"},
            {"模拟方式: ", "模擬方式: "},
            {"玩家名: ", "玩家名: "},
            {"更新玩家名", "更新玩家名"},
            
            // CFG模式控件
            {"CS2路径: ", "CS2路徑: "},
            {"浏览", "瀏覽"},
            {"自动查找", "自動查找"},
            {"绑定键: ", "綁定鍵: "},
            {"添加", "添加"},
            {"删除选中", "刪除選中"},
            {"CFG文件数量: ", "CFG文件數量: "},
            {"生成CFG文件", "生成CFG文件"},
            
            // 消息管理控件
            {"清空所有", "清空所有"},
            
            // 状态和提示
            {"就绪", "就緒"},
            {"玩家名已更新", "玩家名已更新"},
            {"警告: 玩家名不能为空", "警告: 玩家名不能為空"},
            {"选择CS2安装目录", "選擇CS2安裝目錄"},
            {"CS2路径已找到", "CS2路徑已找到"},
            {"错误: 无法找到CS2路径，请手动指定", "錯誤: 無法找到CS2路徑，請手動指定"},
            {"警告: 请输入有效的单个字母或数字", "警告: 請輸入有效的單個字母或數字"},
            {"CFG文件已成功生成", "CFG文件已成功生成"},
            {"确定要清空所有消息吗？", "確定要清空所有消息嗎？"},
            {"确认", "確認"},
            {"警告: 无法生成GSI配置文件", "警告: 無法生成GSI配置文件"},
            {"警告: GSI监听启动失败，请以管理员身份运行", "警告: GSI監聽啟動失敗，請以管理員身份運行"},
            {"已切换到 ", "已切換到 "},
            {"设置聊天键", "設置聊天鍵"},
            {"请按下您想要设置的聊天键:\n(Y=全局聊天, U=队内聊天, Enter=回车)", "請按下您想要設置的聊天鍵:\n(Y=全局聊天, U=隊內聊天, Enter=回車)"},
            
            // 语言切换按钮
            {"切换到繁体中文", "切換到簡體中文"},
            {"切换到简体中文", "切換到繁體中文"}
        };

        // 根据当前语言设置转换文本
        public static string Translate(string text, bool useTraditionalChinese)
        {
            if (!useTraditionalChinese || string.IsNullOrEmpty(text))
                return text;

            // 检查是否有完全匹配的翻译
            if (SimplifiedToTraditional.TryGetValue(text, out string? translated))
                return translated;

            // 对于复合文本，尝试部分匹配和替换
            foreach (var kvp in SimplifiedToTraditional)
            {
                if (text.Contains(kvp.Key))
                {
                    text = text.Replace(kvp.Key, kvp.Value);
                }
            }

            return text;
        }
    }
}