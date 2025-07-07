# 本地化系统架构设计

## 日期: 2025-07-07

## 任务概述

设计PongHub游戏的本地化系统架构，为菜单UI系统添加多语言支持。这是Story-7的第一个任务，为后续实现奠定基础。

## 设计目标

1. 创建灵活、可扩展的本地化系统
2. 支持中文和英文两种语言，易于扩展到更多语言
3. 实现高效的资源加载和管理
4. 确保平滑的语言切换体验
5. 最小化对现有UI系统的修改

## 本地化系统架构

### 核心组件

1. **LocalizationManager**：本地化系统的核心管理器
   - 单例模式，全局访问点
   - 管理语言资源的加载和卸载
   - 处理语言切换
   - 提供本地化文本查询接口
   - 发送语言变更事件

2. **ResourceLoader**：资源加载器
   - 负责加载语言配置文件
   - 加载文本资源文件
   - 加载字体资源
   - 支持异步加载

3. **LanguageSettings**：语言设置
   - 管理用户语言偏好
   - 存储语言配置信息
   - 提供默认语言和回退机制

4. **EventSystem**：事件系统
   - 发布语言变更事件
   - 允许UI组件订阅语言变更

### UI组件

1. **LocalizedText**：本地化文本组件
   - 继承自TextMeshProUGUI
   - 通过键值引用本地化文本
   - 自动响应语言变更
   - 支持文本格式化

2. **LocalizedImage**：本地化图像组件
   - 继承自Image
   - 支持不同语言的图像资源
   - 自动响应语言变更

3. **LanguageSelector**：语言选择器
   - 显示可用语言列表
   - 处理语言选择事件
   - 提供语言预览功能

### 数据结构

1. **语言配置文件**：`languages.json`
```json
{
  "languages": [
    {
      "code": "en",
      "name": "English",
      "font": "LiberationSans",
      "fallback": ""
    },
    {
      "code": "zh",
      "name": "中文",
      "font": "NotoSansSC",
      "fallback": "en"
    }
  ],
  "default": "en"
}
```

2. **文本资源文件**：`en.json`, `zh.json`
```json
{
  "menu.title": "Main Menu",
  "settings.language": "Language",
  "settings.audio": "Audio",
  "settings.graphics": "Graphics",
  "game.start": "Start Game",
  "game.exit": "Exit"
}
```

3. **LanguageInfo类**：语言信息
```csharp
[Serializable]
public class LanguageInfo
{
    public string code;      // 语言代码
    public string name;      // 语言名称
    public string font;      // 字体资源名称
    public string fallback;  // 回退语言
}
```

### 资源组织

```
Resources/
├── Localization/
│   ├── Config/
│   │   └── languages.json       // 语言配置
│   ├── Fonts/
│   │   ├── LiberationSans.ttf   // 英文字体
│   │   └── NotoSansSC.ttf       // 中文字体
│   └── Text/
│       ├── en.json              // 英文文本
│       └── zh.json              // 中文文本
```

## 工作流程

### 初始化流程

1. 游戏启动时，初始化LocalizationManager
2. 加载语言配置文件
3. 确定初始语言（用户偏好或系统语言）
4. 加载初始语言资源
5. 通知所有本地化组件更新

### 语言切换流程

1. 用户选择新语言
2. LocalizationManager加载新语言资源
3. 更新当前语言设置
4. 发送语言变更事件
5. 本地化组件接收事件并更新内容
6. 保存用户语言偏好

### 本地化工作流程

1. 导出需要本地化的文本
2. 创建翻译表
3. 翻译文本内容
4. 导入翻译到资源文件
5. 测试本地化效果
6. 发布更新

## 技术实现细节

### 1. 异步资源加载

使用协程实现异步资源加载，避免阻塞主线程：

```csharp
public IEnumerator LoadLanguageResources(string languageCode)
{
    // 加载文本资源
    ResourceRequest textRequest = Resources.LoadAsync<TextAsset>($"Localization/Text/{languageCode}");
    yield return textRequest;

    // 处理文本资源
    TextAsset textAsset = textRequest.asset as TextAsset;
    if (textAsset != null)
    {
        ProcessTextResource(textAsset);
    }

    // 加载字体资源
    string fontName = GetFontNameForLanguage(languageCode);
    ResourceRequest fontRequest = Resources.LoadAsync<Font>($"Localization/Fonts/{fontName}");
    yield return fontRequest;

    // 处理字体资源
    Font font = fontRequest.asset as Font;
    if (font != null)
    {
        m_fontResources[languageCode] = font;
    }

    // 通知加载完成
    OnLanguageResourcesLoaded(languageCode);
}
```

### 2. 语言变更事件系统

使用C#事件系统实现语言变更通知：

```csharp
// 在LocalizationManager中
public event Action<string> OnLanguageChanged;

// 切换语言时
public void SwitchLanguage(string languageCode)
{
    if (languageCode == CurrentLanguage)
        return;

    StartCoroutine(LoadLanguageResources(languageCode));
}

private void OnLanguageResourcesLoaded(string languageCode)
{
    string previousLanguage = CurrentLanguage;
    CurrentLanguage = languageCode;

    // 保存用户偏好
    SaveLanguagePreference(languageCode);

    // 通知所有监听者
    OnLanguageChanged?.Invoke(languageCode);
}
```

### 3. 本地化文本组件

创建自定义组件处理文本本地化：

```csharp
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string m_localizationKey;
    [SerializeField] private string[] m_formatArgs;

    private TextMeshProUGUI m_textComponent;

    private void Awake()
    {
        m_textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            UpdateLocalizedText();
        }
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    private void OnLanguageChanged(string languageCode)
    {
        UpdateLocalizedText();
    }

    public void UpdateLocalizedText()
    {
        if (string.IsNullOrEmpty(m_localizationKey) || m_textComponent == null)
            return;

        string localizedText = LocalizationManager.Instance.GetLocalizedText(m_localizationKey, m_formatArgs);
        m_textComponent.text = localizedText;
    }
}
```

## 下一步计划

1. 创建LocalizationManager脚本
2. 实现基本的资源加载功能
3. 创建语言配置文件结构
4. 设计本地化组件接口

## 注意事项

1. 确保本地化系统与现有UI系统无缝集成
2. 考虑不同语言的文本长度差异，设计自适应UI布局
3. 优化资源加载性能，避免游戏启动延迟
4. 为翻译人员提供清晰的上下文信息