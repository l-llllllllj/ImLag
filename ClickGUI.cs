using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;
using System.Drawing;
using System.Threading.Tasks;
using CounterStrike2GSI;
using CounterStrike2GSI.EventMessages;
using WindowsInput;
using WindowsInput.Native;
using TextCopy;
using System.Text;
using System.Runtime.InteropServices;

namespace ImLag
{
    public class ClickGUI : Form
    {
        private ConfigManager _configManager;
        private ChatMessageManager _chatManager;
        private CfgManager _cfgManager;
        private GameStateListener _gsl;
        
        // 模式切换按钮
        private Button _modeSwitchButton;
        
        // 语言切换按钮
        private Button _languageSwitchButton;
        
        // 聊天模式控件
        private GroupBox _chatModeGroupBox;
        private Label _chatKeyLabel;
        private Button _changeChatKeyButton;
        private CheckBox _onlySelfDeathCheckBox;
        private CheckBox _skipWindowCheckCheckBox;
        private CheckBox _forceModeCheckBox;
        private Label _keyDelayLabel;
        private NumericUpDown _keyDelayNumericUpDown;
        private Label _simulationMethodLabel;
        private ComboBox _simulationMethodComboBox;
        private Label _playerNameLabel;
        private TextBox _playerNameTextBox;
        private Button _updatePlayerNameButton;
        
        // CFG模式控件
        private GroupBox _cfgModeGroupBox;
        private Label _cs2PathLabel;
        private TextBox _cs2PathTextBox;
        private Button _browseCs2PathButton;
        private Button _findCs2PathButton;
        private Label _bindKeysLabel;
        private ListBox _bindKeysListBox;
        private TextBox _bindKeyInput;
        private Button _addBindKeyButton;
        private Button _removeBindKeyButton;
        private Label _totalCfgFilesLabel;
        private NumericUpDown _totalCfgFilesNumericUpDown;
        private Button _generateCfgFilesButton;
        
        // 消息管理控件
        private GroupBox _messageManagerGroupBox;
        private ListBox _messagesListBox;
        private TextBox _newMessageTextBox;
        private Button _addMessageButton;
        private Button _removeMessageButton;
        private Button _clearAllMessagesButton;
        
        // 状态显示
        private Label _statusLabel;
        
        public ClickGUI()
        {
            InitializeComponents();
            LoadData();
            UpdateUI();
        }
        
        private void InitializeComponents()
        {
            // 基本窗口设置
            this.Text = $"ImLag v{Program.Version}";
            this.Size = new Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            
            // 模式切换按钮
            _modeSwitchButton = new Button
            {
                Text = "切换到CFG模式",
                Location = new Point(20, 20),
                Size = new Size(150, 30)
            };
            _modeSwitchButton.Click += ModeSwitchButton_Click;
            
            // 语言切换按钮
            _languageSwitchButton = new Button
            {
                Text = "切换到繁体中文",
                Location = new Point(630, 20),
                Size = new Size(150, 30)
            };
            _languageSwitchButton.Click += LanguageSwitchButton_Click;
            
            // 聊天模式组
            _chatModeGroupBox = new GroupBox
            {
                Text = "聊天模式设置",
                Location = new Point(20, 60),
                Size = new Size(360, 250),
                Visible = true
            };
            
            _chatKeyLabel = new Label
            {
                Text = "聊天键:",
                Location = new Point(20, 30),
                Size = new Size(100, 20)
            };
            
            _changeChatKeyButton = new Button
            {
                Text = "修改",
                Location = new Point(240, 28),
                Size = new Size(80, 24)
            };
            _changeChatKeyButton.Click += ChangeChatKeyButton_Click;
            
            _onlySelfDeathCheckBox = new CheckBox
            {
                Text = "仅自己死亡时发送消息",
                Location = new Point(20, 60),
                Size = new Size(200, 20)
            };
            _onlySelfDeathCheckBox.CheckedChanged += OnlySelfDeathCheckBox_CheckedChanged;
            
            _skipWindowCheckCheckBox = new CheckBox
            {
                Text = "跳过窗口检测",
                Location = new Point(20, 90),
                Size = new Size(200, 20)
            };
            _skipWindowCheckCheckBox.CheckedChanged += SkipWindowCheckCheckBox_CheckedChanged;
            
            _forceModeCheckBox = new CheckBox
            {
                Text = "强制发送模式",
                Location = new Point(20, 120),
                Size = new Size(200, 20)
            };
            _forceModeCheckBox.CheckedChanged += ForceModeCheckBox_CheckedChanged;
            
            _keyDelayLabel = new Label
            {
                Text = "按键延迟 (ms):",
                Location = new Point(20, 150),
                Size = new Size(100, 20)
            };
            
            _keyDelayNumericUpDown = new NumericUpDown
            {
                Minimum = 30,
                Maximum = 500,
                Value = 100,
                Location = new Point(130, 150),
                Size = new Size(80, 20)
            };
            _keyDelayNumericUpDown.ValueChanged += KeyDelayNumericUpDown_ValueChanged;
            
            _simulationMethodLabel = new Label
            {
                Text = "按键模拟方式:",
                Location = new Point(20, 180),
                Size = new Size(100, 20)
            };
            
            _simulationMethodComboBox = new ComboBox
            {
                Location = new Point(130, 180),
                Size = new Size(190, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _simulationMethodComboBox.Items.AddRange(new object[]
            {
                "keybd_event",
                "InputSimulator",
                "SendInput",
                "WinForm SendKeys"
            });
            _simulationMethodComboBox.SelectedIndexChanged += SimulationMethodComboBox_SelectedIndexChanged;
            
            _playerNameLabel = new Label
            {
                Text = "游戏内玩家名:",
                Location = new Point(20, 210),
                Size = new Size(100, 20)
            };
            
            _playerNameTextBox = new TextBox
            {
                Location = new Point(130, 210),
                Size = new Size(120, 20)
            };
            
            _updatePlayerNameButton = new Button
            {
                Text = "更新",
                Location = new Point(260, 208),
                Size = new Size(60, 24)
            };
            _updatePlayerNameButton.Click += UpdatePlayerNameButton_Click;
            
            _chatModeGroupBox.Controls.AddRange(new Control[]
            {
                _chatKeyLabel, _changeChatKeyButton, _onlySelfDeathCheckBox, 
                _skipWindowCheckCheckBox, _forceModeCheckBox, _keyDelayLabel,
                _keyDelayNumericUpDown, _simulationMethodLabel, _simulationMethodComboBox,
                _playerNameLabel, _playerNameTextBox, _updatePlayerNameButton
            });
            
            // CFG模式组
            _cfgModeGroupBox = new GroupBox
            {
                Text = "CFG模式设置",
                Location = new Point(20, 60),
                Size = new Size(360, 250),
                Visible = false
            };
            
            _cs2PathLabel = new Label
            {
                Text = "CS2路径:",
                Location = new Point(20, 30),
                Size = new Size(100, 20)
            };
            
            _cs2PathTextBox = new TextBox
            {
                Location = new Point(20, 55),
                Size = new Size(240, 20)
            };
            
            _browseCs2PathButton = new Button
            {
                Text = "浏览",
                Location = new Point(270, 53),
                Size = new Size(60, 24)
            };
            _browseCs2PathButton.Click += BrowseCs2PathButton_Click;
            
            _findCs2PathButton = new Button
            {
                Text = "自动查找",
                Location = new Point(20, 85),
                Size = new Size(100, 24)
            };
            _findCs2PathButton.Click += FindCs2PathButton_Click;
            
            _bindKeysLabel = new Label
            {
                Text = "绑定键:",
                Location = new Point(20, 120),
                Size = new Size(100, 20)
            };
            
            _bindKeysListBox = new ListBox
            {
                Location = new Point(20, 145),
                Size = new Size(150, 80)
            };
            
            _bindKeyInput = new TextBox
            {
                Location = new Point(180, 145),
                Size = new Size(40, 20),
                MaxLength = 1
            };
            
            _addBindKeyButton = new Button
            {
                Text = "添加",
                Location = new Point(230, 143),
                Size = new Size(60, 24)
            };
            _addBindKeyButton.Click += AddBindKeyButton_Click;
            
            _removeBindKeyButton = new Button
            {
                Text = "删除",
                Location = new Point(180, 175),
                Size = new Size(60, 24)
            };
            _removeBindKeyButton.Click += RemoveBindKeyButton_Click;
            
            _totalCfgFilesLabel = new Label
            {
                Text = "CFG文件数量:",
                Location = new Point(180, 210),
                Size = new Size(100, 20)
            };
            
            _totalCfgFilesNumericUpDown = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 1000,
                Value = 5,
                Location = new Point(270, 210),
                Size = new Size(60, 20)
            };
            _totalCfgFilesNumericUpDown.ValueChanged += TotalCfgFilesNumericUpDown_ValueChanged;
            
            _generateCfgFilesButton = new Button
            {
                Text = "生成CFG文件",
                Location = new Point(180, 240),
                Size = new Size(150, 24),
                BackColor = Color.LightGreen
            };
            _generateCfgFilesButton.Click += GenerateCfgFilesButton_Click;
            
            _cfgModeGroupBox.Controls.AddRange(new Control[]
            {
                _cs2PathLabel, _cs2PathTextBox, _browseCs2PathButton, _findCs2PathButton,
                _bindKeysLabel, _bindKeysListBox, _bindKeyInput, _addBindKeyButton,
                _removeBindKeyButton, _totalCfgFilesLabel, _totalCfgFilesNumericUpDown,
                _generateCfgFilesButton
            });
            
            // 消息管理组
            _messageManagerGroupBox = new GroupBox
            {
                Text = "消息管理",
                Location = new Point(400, 60),
                Size = new Size(360, 450)
            };
            
            _messagesListBox = new ListBox
            {
                Location = new Point(20, 30),
                Size = new Size(320, 350)
            };
            
            _newMessageTextBox = new TextBox
            {
                Location = new Point(20, 390),
                Size = new Size(220, 20)
            };
            
            _addMessageButton = new Button
            {
                Text = "添加",
                Location = new Point(250, 388),
                Size = new Size(90, 24),
                BackColor = Color.LightBlue
            };
            _addMessageButton.Click += AddMessageButton_Click;
            
            _removeMessageButton = new Button
            {
                Text = "删除选中",
                Location = new Point(20, 420),
                Size = new Size(100, 24)
            };
            _removeMessageButton.Click += RemoveMessageButton_Click;
            
            _clearAllMessagesButton = new Button
            {
                Text = "清空所有",
                Location = new Point(240, 420),
                Size = new Size(100, 24),
                BackColor = Color.LightCoral
            };
            _clearAllMessagesButton.Click += ClearAllMessagesButton_Click;
            
            _messageManagerGroupBox.Controls.AddRange(new Control[]
            {
                _messagesListBox, _newMessageTextBox, _addMessageButton,
                _removeMessageButton, _clearAllMessagesButton
            });
            
            // 状态标签
            _statusLabel = new Label
            {
                Text = "就绪",
                Location = new Point(20, 530),
                Size = new Size(740, 20),
                Font = new Font(DefaultFont, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            
            // 添加所有控件到窗口
            this.Controls.AddRange(new Control[]
            {
                _modeSwitchButton, _languageSwitchButton, _chatModeGroupBox, _cfgModeGroupBox,
                _messageManagerGroupBox, _statusLabel
            });
        }
        
        private void LoadData()
        {
            _configManager = new ConfigManager();
            _configManager.LoadConfig();
            
            _chatManager = new ChatMessageManager();
            _chatManager.LoadMessages();
            
            _cfgManager = new CfgManager(_chatManager, _configManager);
            
            // 初始化GSI监听器
            _gsl = new GameStateListener(4000);
            if (!_gsl.GenerateGSIConfigFile("ImLag"))
            {
                _statusLabel.Text = "警告: 无法生成GSI配置文件";
            }
            
            _gsl.PlayerDied += (gameEvent) => OnPlayerDied(gameEvent);
            if (!_gsl.Start())
            {
                _statusLabel.Text = "警告: GSI监听启动失败，请以管理员身份运行";
            }
        }
        
        private void UpdateUI()
        {
            bool useTraditional = _configManager.Config.UseTraditionalChinese;
            
            // 更新语言切换按钮文本
            _languageSwitchButton.Text = useTraditional ? 
                LanguageManager.Translate("切换到简体中文", useTraditional) : 
                LanguageManager.Translate("切换到繁体中文", useTraditional);
            
            // 更新模式切换按钮文本
            _modeSwitchButton.Text = _configManager.Config.UseCfgMode ? 
                LanguageManager.Translate("切换到聊天模式", useTraditional) : 
                LanguageManager.Translate("切换到CFG模式", useTraditional);
            
            // 切换模式组的可见性
            _chatModeGroupBox.Visible = !_configManager.Config.UseCfgMode;
            _cfgModeGroupBox.Visible = _configManager.Config.UseCfgMode;
            
            // 更新聊天模式控件
            var chatKeyDescription = _configManager.Config.ChatKey switch
            {
                "y" => LanguageManager.Translate("全局聊天 (Y)", useTraditional),
                "u" => LanguageManager.Translate("队内聊天 (U)", useTraditional),
                "enter" => LanguageManager.Translate("回车 (Enter)", useTraditional),
                _ => $"{LanguageManager.Translate("自定义 (", useTraditional)}{_configManager.Config.ChatKey.ToUpper()})"
            };
            _chatKeyLabel.Text = $"{LanguageManager.Translate("聊天键: ", useTraditional)}{chatKeyDescription}";
            _chatModeGroupBox.Text = LanguageManager.Translate("聊天模式设置", useTraditional);
            _onlySelfDeathCheckBox.Text = LanguageManager.Translate("仅自己死亡时发送", useTraditional);
            _skipWindowCheckCheckBox.Text = LanguageManager.Translate("跳过窗口检测", useTraditional);
            _forceModeCheckBox.Text = LanguageManager.Translate("强制发送模式", useTraditional);
            _keyDelayLabel.Text = LanguageManager.Translate("按键延迟: ", useTraditional);
            _simulationMethodLabel.Text = LanguageManager.Translate("模拟方式: ", useTraditional);
            _playerNameLabel.Text = LanguageManager.Translate("玩家名: ", useTraditional);
            _updatePlayerNameButton.Text = LanguageManager.Translate("更新玩家名", useTraditional);
            
            // 更新CFG模式控件
            _cfgModeGroupBox.Text = LanguageManager.Translate("CFG模式设置", useTraditional);
            _cs2PathLabel.Text = LanguageManager.Translate("CS2路径: ", useTraditional);
            _browseCs2PathButton.Text = LanguageManager.Translate("浏览", useTraditional);
            _findCs2PathButton.Text = LanguageManager.Translate("自动查找", useTraditional);
            _bindKeysLabel.Text = LanguageManager.Translate("绑定键: ", useTraditional);
            _addBindKeyButton.Text = LanguageManager.Translate("添加", useTraditional);
            _removeBindKeyButton.Text = LanguageManager.Translate("删除选中", useTraditional);
            _totalCfgFilesLabel.Text = LanguageManager.Translate("CFG文件数量: ", useTraditional);
            _generateCfgFilesButton.Text = LanguageManager.Translate("生成CFG文件", useTraditional);
            _cs2PathTextBox.Text = _configManager.Config.CS2Path ?? string.Empty;
            
            _bindKeysListBox.Items.Clear();
            foreach (var key in _configManager.Config.BindKeys)
            {
                _bindKeysListBox.Items.Add(key);
            }
            
            _totalCfgFilesNumericUpDown.Value = _configManager.Config.TotalCfgFiles;
            
            // 更新消息管理控件
            _messageManagerGroupBox.Text = LanguageManager.Translate("消息管理", useTraditional);
            _addMessageButton.Text = LanguageManager.Translate("添加", useTraditional);
            _removeMessageButton.Text = LanguageManager.Translate("删除选中", useTraditional);
            _clearAllMessagesButton.Text = LanguageManager.Translate("清空所有", useTraditional);
            
            // 更新消息列表
            UpdateMessagesList();
            
            // 更新状态标签
            _statusLabel.Text = LanguageManager.Translate(_statusLabel.Text, useTraditional);
        }
        
        private void UpdateMessagesList()
        {
            _messagesListBox.Items.Clear();
            foreach (var message in _chatManager.Messages)
            {
                _messagesListBox.Items.Add(message);
            }
        }
        
        // 模式切换按钮事件
        private void ModeSwitchButton_Click(object sender, EventArgs e)
        {
            _configManager.Config.UseCfgMode = !_configManager.Config.UseCfgMode;
            _configManager.SaveConfig();
            UpdateUI();
            
            string mode = _configManager.Config.UseCfgMode ? 
                LanguageManager.Translate("CFG模式", _configManager.Config.UseTraditionalChinese) : 
                LanguageManager.Translate("聊天模式", _configManager.Config.UseTraditionalChinese);
            _statusLabel.Text = $"{LanguageManager.Translate("已切换到 ", _configManager.Config.UseTraditionalChinese)}{mode}";
        }
        
        // 聊天键修改按钮事件
        private void ChangeChatKeyButton_Click(object sender, EventArgs e)
        {
            using (var inputForm = new Form
            {
                Text = LanguageManager.Translate("设置聊天键", _configManager.Config.UseTraditionalChinese),
                Size = new Size(300, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false
            })
            {
                var label = new Label
                {
                    Text = LanguageManager.Translate("请按下您想要设置的聊天键:\n(Y=全局聊天, U=队内聊天, Enter=回车)", _configManager.Config.UseTraditionalChinese),
                    Location = new Point(20, 20),
                    Size = new Size(250, 50),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                inputForm.KeyPress += (s, args) =>
                {
                    string key = args.KeyChar.ToString().ToLower();
                    if (key == "y" || key == "u" || args.KeyChar == (char)Keys.Enter)
                    {
                        if (args.KeyChar == (char)Keys.Enter)
                        {
                            _configManager.Config.ChatKey = "enter";
                        }
                        else
                        {
                            _configManager.Config.ChatKey = key;
                        }
                        _configManager.SaveConfig();
                        UpdateUI();
                        inputForm.Close();
                    }
                };

                inputForm.Controls.Add(label);
                inputForm.ShowDialog(this);
            }
        }
        
        // 仅自己死亡复选框事件
        private void OnlySelfDeathCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _configManager.Config.OnlySelfDeath = _onlySelfDeathCheckBox.Checked;
            _configManager.SaveConfig();
        }
        
        // 跳过窗口检测复选框事件
        private void SkipWindowCheckCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _configManager.Config.SkipWindowCheck = _skipWindowCheckCheckBox.Checked;
            _configManager.SaveConfig();
        }
        
        // 强制发送模式复选框事件
        private void ForceModeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _configManager.Config.ForceMode = _forceModeCheckBox.Checked;
            _configManager.SaveConfig();
        }
        
        // 按键延迟数值框事件
        private void KeyDelayNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            _configManager.Config.KeyDelay = (int)_keyDelayNumericUpDown.Value;
            _configManager.SaveConfig();
        }
        
        // 模拟方式下拉框事件
        private void SimulationMethodComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _configManager.Config.KeySimulationMethod = _simulationMethodComboBox.SelectedIndex;
            _configManager.SaveConfig();
        }
        
        // 更新玩家名按钮事件
        private void UpdatePlayerNameButton_Click(object sender, EventArgs e)
        {
            string newName = _playerNameTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                _configManager.Config.UserPlayerName = newName;
                _configManager.SaveConfig();
                _statusLabel.Text = "玩家名已更新";
            }
            else
            {
                _statusLabel.Text = "警告: 玩家名不能为空";
            }
        }
        
        // 浏览CS2路径按钮事件
        private void BrowseCs2PathButton_Click(object sender, EventArgs e)
        {
            using (var folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.Description = "选择CS2安装目录";
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    _cs2PathTextBox.Text = folderBrowser.SelectedPath;
                    _configManager.Config.CS2Path = folderBrowser.SelectedPath;
                    _configManager.SaveConfig();
                }
            }
        }
        
        // 自动查找CS2路径按钮事件
        private void FindCs2PathButton_Click(object sender, EventArgs e)
        {
            _cfgManager.FindCS2Path();
            if (!string.IsNullOrEmpty(_cfgManager.CS2Path))
            {
                _cs2PathTextBox.Text = _cfgManager.CS2Path;
                _configManager.Config.CS2Path = _cfgManager.CS2Path;
                _configManager.SaveConfig();
                _statusLabel.Text = "CS2路径已找到";
            }
            else
            {
                _statusLabel.Text = "错误: 无法找到CS2路径，请手动指定";
            }
        }
        
        // 添加绑定键按钮事件
        private void AddBindKeyButton_Click(object sender, EventArgs e)
        {
            string key = _bindKeyInput.Text.ToLower().Trim();
            if (key.Length == 1 && char.IsLetterOrDigit(key[0]) && !_configManager.Config.BindKeys.Contains(key))
            {
                _configManager.Config.BindKeys.Add(key);
                _configManager.SaveConfig();
                _bindKeysListBox.Items.Add(key);
                _bindKeyInput.Clear();
            }
            else
            {
                _statusLabel.Text = "警告: 请输入有效的单个字母或数字";
            }
        }
        
        // 删除绑定键按钮事件
        private void RemoveBindKeyButton_Click(object sender, EventArgs e)
        {
            if (_bindKeysListBox.SelectedItem != null)
            {
                string key = _bindKeysListBox.SelectedItem.ToString();
                _configManager.Config.BindKeys.Remove(key);
                _configManager.SaveConfig();
                _bindKeysListBox.Items.Remove(key);
            }
        }
        
        // CFG文件数量数值框事件
        private void TotalCfgFilesNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            _configManager.Config.TotalCfgFiles = (int)_totalCfgFilesNumericUpDown.Value;
            _configManager.SaveConfig();
        }
        
        // 生成CFG文件按钮事件
        private void GenerateCfgFilesButton_Click(object sender, EventArgs e)
        {
            try
            {
                _cfgManager.GenerateConfigFiles();
                _statusLabel.Text = "CFG文件已成功生成";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"错误: {ex.Message}";
            }
        }
        
        // 添加消息按钮事件
        private void AddMessageButton_Click(object sender, EventArgs e)
        {
            string message = _newMessageTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                _chatManager.AddMessage(message);
                UpdateMessagesList();
                _newMessageTextBox.Clear();
            }
        }
        
        // 删除消息按钮事件
        private void RemoveMessageButton_Click(object sender, EventArgs e)
        {
            if (_messagesListBox.SelectedIndex >= 0)
            {
                _chatManager.RemoveMessage(_messagesListBox.SelectedIndex);
                UpdateMessagesList();
            }
        }
        
        // 清空所有消息按钮事件
        private void ClearAllMessagesButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                LanguageManager.Translate("确定要清空所有消息吗？", _configManager.Config.UseTraditionalChinese), 
                LanguageManager.Translate("确认", _configManager.Config.UseTraditionalChinese), 
                MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                // 清空所有消息
                _chatManager.Messages.Clear();
                _chatManager.SaveMessages();
                UpdateMessagesList();
            }
        }
        
        // 语言切换按钮事件
        private void LanguageSwitchButton_Click(object sender, EventArgs e)
        {
            _configManager.Config.UseTraditionalChinese = !_configManager.Config.UseTraditionalChinese;
            _configManager.SaveConfig();
            UpdateUI();
            
            string lang = _configManager.Config.UseTraditionalChinese ? 
                LanguageManager.Translate("繁体中文", true) : 
                LanguageManager.Translate("简体中文", false);
            _statusLabel.Text = $"{LanguageManager.Translate("已切换到 ", _configManager.Config.UseTraditionalChinese)}{lang}";
        }
        
        // 玩家死亡事件处理
        private void OnPlayerDied(PlayerDied e)
        {
            // 检查是否在聊天模式
            if (_configManager.Config.UseCfgMode)
                return;
            
            // 检查是否仅自己死亡时发送
            if (_configManager.Config.OnlySelfDeath && e.Player.Name != _configManager.Config.UserPlayerName)
                return;
            
            // 检查CS2窗口是否激活
            if (!_configManager.Config.SkipWindowCheck && !IsCS2Active())
                return;
            
            // 在UI线程上执行
            this.Invoke(new Action(() =>
            {
                try
                {
                    // 发送随机消息
                    string randomMessage = _chatManager.GetRandomMessage();
                    if (!string.IsNullOrEmpty(randomMessage))
                    {
                        SendChatMessage(randomMessage);
                        _statusLabel.Text = $"已发送消息: {randomMessage}";
                    }
                }
                catch (Exception ex)
                {
                    _statusLabel.Text = $"发送消息失败: {ex.Message}";
                }
            }));
        }
        
        // 发送聊天消息
        private void SendChatMessage(string message)
        {
            // 复制消息到剪贴板
            System.Windows.Forms.Clipboard.SetText(message);
            
            // 根据不同的模拟方式发送
            switch (_configManager.Config.KeySimulationMethod)
            {
                case 0: // keybd_event
                    SendChatMessageKeybdEvent();
                    break;
                case 1: // InputSimulator
                    SendChatMessageInputSimulator();
                    break;
                case 2: // SendInput
                    SendChatMessageSendInput();
                    break;
                case 3: // WinForm SendKeys
                    SendChatMessageSendKeys();
                    break;
            }
        }
        
        // 使用keybd_event发送聊天消息
        private void SendChatMessageKeybdEvent()
        {
            // 打开聊天框
            OpenChatBoxKeybdEvent();
            Thread.Sleep(_configManager.Config.KeyDelay);
            
            // 粘贴内容
            PasteFromClipboardKeybdEvent();
            Thread.Sleep(_configManager.Config.KeyDelay);
            
            // 发送
            SendEnterKeyKeybdEvent();
        }
        
        // 使用InputSimulator发送聊天消息
        private void SendChatMessageInputSimulator()
        {
            try
            {
                var sim = new InputSimulator();
                
                // 打开聊天框
                OpenChatBoxInputSimulator(sim);
                Thread.Sleep(_configManager.Config.KeyDelay);
                
                // 粘贴内容
                PasteFromClipboardInputSimulator(sim);
                Thread.Sleep(_configManager.Config.KeyDelay);
                
                // 发送
                SendEnterKeyInputSimulator(sim);
            }
            catch
            {
                // 如果失败，切换到SendInput
                _configManager.Config.KeySimulationMethod = 2;
                _configManager.SaveConfig();
                SendChatMessageSendInput();
            }
        }
        
        // 使用SendInput发送聊天消息
        private void SendChatMessageSendInput()
        {
            // 打开聊天框
            OpenChatBoxSendInput();
            Thread.Sleep(_configManager.Config.KeyDelay);
            
            // 粘贴内容
            PasteFromClipboardSendInput();
            Thread.Sleep(_configManager.Config.KeyDelay);
            
            // 发送
            SendEnterKeySendInput();
        }
        
        // 使用WinForm SendKeys发送聊天消息
        private void SendChatMessageSendKeys()
        {
            // 打开聊天框
            OpenChatBoxSendKeys();
            Thread.Sleep(_configManager.Config.KeyDelay);
            
            // 粘贴内容
            PasteFromClipboardSendKeys();
            Thread.Sleep(_configManager.Config.KeyDelay);
            
            // 发送
            SendEnterKeySendKeys();
        }
        
        // 以下是各种输入模拟方法的具体实现
        // 这些方法需要从Program.cs中复制并适配到GUI环境
        
        // 检查CS2窗口是否激活
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        private bool IsCS2Active()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();
            
            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString().Contains("Counter-Strike 2") || Buff.ToString().Contains("CS2");
            }
            return false;
        }
        
        // keybd_event相关方法
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        
        private void OpenChatBoxKeybdEvent()
        {
            byte keyCode = _configManager.Config.ChatKey switch
            {
                "y" => 0x59, // Y
                "u" => 0x55, // U
                "enter" => 0x0D, // Enter
                _ => GetVirtualKeyFromChar(_configManager.Config.ChatKey[0])
            };
            
            keybd_event(keyCode, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        
        private void PasteFromClipboardKeybdEvent()
        {
            // Ctrl+V
            keybd_event(0x11, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Ctrl
            Thread.Sleep(50);
            keybd_event(0x56, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // V
            Thread.Sleep(50);
            keybd_event(0x56, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // V
            Thread.Sleep(50);
            keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Ctrl
        }
        
        private void SendEnterKeyKeybdEvent()
        {
            keybd_event(0x0D, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Enter
            Thread.Sleep(50);
            keybd_event(0x0D, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Enter
        }
        
        // InputSimulator相关方法
        private void OpenChatBoxInputSimulator(InputSimulator sim)
        {
            if (_configManager.Config.ChatKey == "y")
                sim.Keyboard.KeyPress(VirtualKeyCode.VK_Y);
            else if (_configManager.Config.ChatKey == "u")
                sim.Keyboard.KeyPress(VirtualKeyCode.VK_U);
            else if (_configManager.Config.ChatKey == "enter")
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            else
            {
                char key = _configManager.Config.ChatKey[0];
                if (char.IsDigit(key))
                    sim.Keyboard.KeyPress((VirtualKeyCode)(0x30 + (key - '0')));
                else
                    sim.Keyboard.KeyPress((VirtualKeyCode)(0x41 + char.ToUpper(key) - 'A'));
            }
        }
        
        private void PasteFromClipboardInputSimulator(InputSimulator sim)
        {
            sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
        }
        
        private void SendEnterKeyInputSimulator(InputSimulator sim)
        {
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
        }
        
        // SendInput相关方法
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }
        
        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
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
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
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
        
        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        
        private void OpenChatBoxSendInput()
        {
            ushort keyCode = _configManager.Config.ChatKey switch
            {
                "y" => 0x59, // Y
                "u" => 0x55, // U
                "enter" => 0x0D, // Enter
                _ => (ushort)GetVirtualKeyFromChar(_configManager.Config.ChatKey[0])
            };
            
            KEYBDINPUT kb = new KEYBDINPUT();
            kb.wVk = keyCode;
            kb.dwFlags = 0;
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki = kb;
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            
            kb.dwFlags = KEYEVENTF_KEYUP;
            inputs[0].U.ki = kb;
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
        
        private void PasteFromClipboardSendInput()
        {
            // Ctrl+V
            // Ctrl Down
            KEYBDINPUT kbCtrlDown = new KEYBDINPUT();
            kbCtrlDown.wVk = 0x11; // Ctrl
            kbCtrlDown.dwFlags = 0;
            INPUT[] inputsCtrlDown = new INPUT[1];
            inputsCtrlDown[0].type = INPUT_KEYBOARD;
            inputsCtrlDown[0].U.ki = kbCtrlDown;
            SendInput(1, inputsCtrlDown, Marshal.SizeOf(typeof(INPUT)));
            
            // V Down
            KEYBDINPUT kbVDown = new KEYBDINPUT();
            kbVDown.wVk = 0x56; // V
            kbVDown.dwFlags = 0;
            INPUT[] inputsVDown = new INPUT[1];
            inputsVDown[0].type = INPUT_KEYBOARD;
            inputsVDown[0].U.ki = kbVDown;
            SendInput(1, inputsVDown, Marshal.SizeOf(typeof(INPUT)));
            
            // V Up
            kbVDown.dwFlags = KEYEVENTF_KEYUP;
            inputsVDown[0].U.ki = kbVDown;
            SendInput(1, inputsVDown, Marshal.SizeOf(typeof(INPUT)));
            
            // Ctrl Up
            kbCtrlDown.dwFlags = KEYEVENTF_KEYUP;
            inputsCtrlDown[0].U.ki = kbCtrlDown;
            SendInput(1, inputsCtrlDown, Marshal.SizeOf(typeof(INPUT)));
        }
        
        private void SendEnterKeySendInput()
        {
            KEYBDINPUT kb = new KEYBDINPUT();
            kb.wVk = 0x0D; // Enter
            kb.dwFlags = 0;
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki = kb;
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            
            kb.dwFlags = KEYEVENTF_KEYUP;
            inputs[0].U.ki = kb;
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
        
        // WinForm SendKeys相关方法
        private void OpenChatBoxSendKeys()
        {
            string key = _configManager.Config.ChatKey switch
            {
                "y" => "Y",
                "u" => "U",
                "enter" => "{ENTER}",
                _ => _configManager.Config.ChatKey.ToUpper()
            };
            SendKeys.SendWait(key);
        }
        
        private void PasteFromClipboardSendKeys()
        {
            SendKeys.SendWait("^v"); // Ctrl+V
        }
        
        private void SendEnterKeySendKeys()
        {
            SendKeys.SendWait("{ENTER}");
        }
        
        // 获取字符对应的虚拟键码
        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);
        
        private byte GetVirtualKeyFromChar(char ch)
        {
            short keyCode = VkKeyScan(ch);
            return (byte)(keyCode & 0xFF);
        }
    }
}