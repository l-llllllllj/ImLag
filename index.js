const { app, BrowserWindow, ipcMain, dialog, shell } = require('electron');
const path = require('path');
const fs = require('fs-extra');
const { exec } = require('child_process');
const keySender = require('node-key-sender');
const { v4: uuidv4 } = require('uuid');
const { userInfo } = require('os');

// 配置文件路径
const configPath = path.join(app.getPath('userData'), 'config.json');
const messagesPath = path.join(app.getPath('userData'), 'messages.json');

// 确保配置目录存在
function ensureConfigDir() {
  const configDir = path.dirname(configPath);
  if (!fs.existsSync(configDir)) {
    fs.ensureDirSync(configDir);
  }
}

// 默认配置
const defaultConfig = {
  chatKey: 'y',
  userPlayerName: '',
  onlySelfDeath: true,
  skipWindowCheck: false,
  forceMode: false,
  keyDelay: 100,
  useCfgMode: false,
  totalCfgFiles: 5,
  bindKeys: ['k', 'p', 'l', 'm'],
  cs2Path: '',
  keySimulationMethod: 0,
  useTraditionalChinese: false
};

// 默认消息
const defaultMessages = [
  '网卡', '手抖', '高延迟', '鼠标出问题了', '瓶颈期', '手冻僵了', '被阴了',
  '卡输入法了', 'day0了', '掉帧了', '手汗手滑', '腱鞘炎犯了', '吞子弹了',
  'timing侠', '唉，资本', '刚打瓦回来不适应', '灵敏度有问题', '谁把我键位改了',
  '感冒了没反应', '拆消音器去了', '校园网是这样的', '状态不行', '鼠标撞键盘上了',
  '复健', '屏幕太小', '键盘坏了', '显示器延迟高', '对面锁了', '他静音'
];

// 读取配置
function readConfig() {
  ensureConfigDir();
  try {
    if (fs.existsSync(configPath)) {
      const data = fs.readFileSync(configPath, 'utf8');
      return { ...defaultConfig, ...JSON.parse(data) };
    }
  } catch (error) {
    console.error('读取配置文件失败:', error);
  }
  return { ...defaultConfig };
}

// 保存配置
function saveConfig(config) {
  ensureConfigDir();
  try {
    fs.writeFileSync(configPath, JSON.stringify(config, null, 2));
    return { success: true };
  } catch (error) {
    console.error('保存配置文件失败:', error);
    return { success: false, error: error.message };
  }
}

// 读取消息
function readMessages() {
  ensureConfigDir();
  try {
    if (fs.existsSync(messagesPath)) {
      const data = fs.readFileSync(messagesPath, 'utf8');
      return JSON.parse(data);
    }
  } catch (error) {
    console.error('读取消息文件失败:', error);
  }
  return [...defaultMessages];
}

// 保存消息
function saveMessages(messages) {
  ensureConfigDir();
  try {
    fs.writeFileSync(messagesPath, JSON.stringify(messages, null, 2));
    return { success: true };
  } catch (error) {
    console.error('保存消息文件失败:', error);
    return { success: false, error: error.message };
  }
}

// 当前配置
let currentConfig = readConfig();

let mainWindow;

// 检查是否以管理员权限运行
async function checkAdminRights() {
  if (process.platform === 'win32') {
    try {
      // 在Windows上检查管理员权限
      const { execSync } = require('child_process');
      // 尝试执行需要管理员权限的命令
      execSync('net session', { stdio: 'ignore' });
      return true;
    } catch (error) {
      return false;
    }
  }
  return true; // 非Windows平台默认为有管理员权限
}

// 显示非管理员权限提示
function showAdminWarning() {
  const response = dialog.showMessageBoxSync(null, {
    type: 'warning',
    title: '权限不足',
    message: '警告：应用程序未以管理员权限运行',
    detail: '某些功能可能无法正常工作。建议以管理员身份重新启动应用程序。',
    buttons: ['确定', '以管理员身份重新启动'],
    defaultId: 0,
    cancelId: 0
  });
  
  // 无论用户点击哪个按钮，应用程序都会继续启动
  // 只有在用户选择"以管理员身份重新启动"时，才会重新启动应用程序
  if (response === 1) {
    // 以管理员身份重新启动
    if (process.platform === 'win32') {
      const { exec } = require('child_process');
      exec('powershell -Command "Start-Process \'' + process.execPath + '\' -Verb RunAs"');
      app.quit();
    }
  }
  // 如果用户点击"确定"，函数会自然结束，应用程序会继续启动
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 800,
    height: 600,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false,
      enableRemoteModule: true
    },
    resizable: true,
    autoHideMenuBar: true
  });

  mainWindow.loadFile('index.html');
  
  // 开发模式下打开调试工具
  // mainWindow.webContents.openDevTools();

  mainWindow.on('closed', function () {
    mainWindow = null;
  });
}

app.on('ready', async () => {
  // 检查是否以管理员权限运行
  const isAdmin = await checkAdminRights();
  if (!isAdmin) {
    // 非管理员权限，显示警告
    showAdminWarning();
  }
  // 无论是否有管理员权限，都创建窗口
  createWindow();
});

app.on('window-all-closed', function () {
  if (process.platform !== 'darwin') app.quit();
});

app.on('activate', function () {
  if (mainWindow === null) createWindow();
});

// 配置管理相关的IPC处理
ipcMain.on('get-config', (event) => {
  currentConfig = readConfig();
  event.returnValue = { ...currentConfig, messages: readMessages() };
});

ipcMain.on('save-config', (event, config) => {
  // 从配置中分离出消息
  const { messages, ...configWithoutMessages } = config;
  currentConfig = configWithoutMessages;
  const result = saveConfig(configWithoutMessages);
  event.returnValue = result;
});

// 消息管理相关的IPC处理
ipcMain.on('get-messages', (event) => {
  event.returnValue = readMessages();
});

ipcMain.on('save-messages', (event, messages) => {
  const result = saveMessages(messages);
  event.returnValue = result;
});

// CFG模式相关的IPC处理
ipcMain.on('find-cs2-path', (event) => {
  try {
    findCS2Path().then(path => {
      event.reply('cs2-path-found', path);
    }).catch(err => {
      event.reply('cs2-path-found', { error: err.message });
    });
  } catch (error) {
    event.reply('cs2-path-found', { error: error.message });
  }
});

ipcMain.on('browse-cs2-path', (event) => {
  dialog.showOpenDialog(mainWindow, {
    properties: ['openDirectory'],
    title: '选择CS2安装目录'
  }).then(result => {
    if (!result.canceled && result.filePaths.length > 0) {
      event.reply('cs2-path-selected', result.filePaths[0]);
    }
  }).catch(err => {
    event.reply('cs2-path-selected', { error: err.message });
  });
});

ipcMain.on('generate-cfg-files', (event, { cs2Path, bindKeys, totalCfgFiles, messages }) => {
  try {
    generateCfgFiles(cs2Path, bindKeys, totalCfgFiles, messages);
    event.reply('cfg-files-generated', { success: true });
  } catch (error) {
    event.reply('cfg-files-generated', { success: false, error: error.message });
  }
});

// 查找CS2路径的函数
async function findCS2Path() {
  if (process.platform === 'win32') {
    // 在Windows上从注册表查找Steam路径
    return new Promise((resolve, reject) => {
      exec('reg query "HKCU\Software\Valve\Steam" /v SteamPath', (error, stdout, stderr) => {
        if (error) {
          reject(new Error('无法从注册表找到Steam路径'));
          return;
        }
        
        const match = stdout.match(/SteamPath\s+REG_SZ\s+(.+)/);
        if (!match || !match[1]) {
          reject(new Error('无法解析Steam路径'));
          return;
        }
        
        const steamPath = match[1].trim();
        const libraryFoldersPath = path.join(steamPath, 'steamapps', 'libraryfolders.vdf');
        
        if (!fs.existsSync(libraryFoldersPath)) {
          // 只检查默认库路径
          const defaultCs2Path = path.join(steamPath, 'steamapps', 'common', 'Counter-Strike Global Offensive');
          if (fs.existsSync(path.join(defaultCs2Path, 'game', 'csgo', 'pak01_dir.vpk'))) {
            resolve(defaultCs2Path);
          } else {
            reject(new Error('无法找到CS2路径'));
          }
          return;
        }
        
        try {
          const libraryContent = fs.readFileSync(libraryFoldersPath, 'utf8');
          const libraryPaths = [];
          libraryPaths.push(steamPath);
          
          // 简单解析libraryfolders.vdf文件
          const pathMatches = libraryContent.match(/"path"\s+"(.+)"/g) || [];
          pathMatches.forEach(match => {
            const pathMatch = match.match(/"path"\s+"(.+)"/);
            if (pathMatch && pathMatch[1]) {
              libraryPaths.push(pathMatch[1].replace(/\\/g, '\\\\'));
            }
          });
          
          const possibleRelativePaths = [
            path.join('steamapps', 'common', 'Counter-Strike Global Offensive'),
            path.join('steamapps', 'common', 'Counter-Strike 2')
          ];
          
          for (const libPath of libraryPaths) {
            for (const relativePath of possibleRelativePaths) {
              const potentialCs2Path = path.join(libPath, relativePath);
              if (fs.existsSync(path.join(potentialCs2Path, 'game', 'csgo', 'pak01_dir.vpk'))) {
                resolve(potentialCs2Path);
                return;
              }
            }
          }
          
          reject(new Error('无法找到CS2路径'));
        } catch (e) {
          reject(new Error('解析libraryfolders.vdf时出错: ' + e.message));
        }
      });
    });
  } else {
    throw new Error('当前平台不支持自动查找CS2路径');
  }
}

// 生成CFG文件的函数
function generateCfgFiles(cs2Path, bindKeys, totalCfgFiles, messages) {
  if (!cs2Path) {
    throw new Error('CS2路径不能为空');
  }
  
  const cfgPath = path.join(cs2Path, 'game', 'csgo', 'cfg');
  if (!fs.existsSync(cfgPath)) {
    fs.ensureDirSync(cfgPath);
  }
  
  // 清理旧文件
  const oldFiles = fs.readdirSync(cfgPath).filter(file => file.startsWith('imlag_') && file.endsWith('.cfg'));
  oldFiles.forEach(file => fs.unlinkSync(path.join(cfgPath, file)));
  
  // 生成新文件
  for (let i = 1; i <= totalCfgFiles; i++) {
    const randomMessages = [];
    for (let j = 0; j < bindKeys.length && j < messages.length; j++) {
      const randomIndex = Math.floor(Math.random() * messages.length);
      randomMessages.push(messages[randomIndex]);
    }
    
    let cfgContent = '// ImLag CFG File ' + i + '\n';
    
    bindKeys.forEach((key, index) => {
      if (index < randomMessages.length) {
        const message = randomMessages[index];
        cfgContent += `bind "${key}" "say ${message}"\n`;
      }
    });
    
    fs.writeFileSync(path.join(cfgPath, `imlag_${i}.cfg`), cfgContent);
  }
}