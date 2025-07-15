# MCP Unity 与 Cursor 编辑器结合使用指南

## 什么是 MCP Unity？

MCP Unity 是一个实现了模型上下文协议（Model Context Protocol）的 Unity 编辑器集成工具，它允许 AI 助手（如 Claude、Cursor 等）与您的 Unity 项目进行交互。该工具通过在 Unity 和支持 MCP 协议的 Node.js 服务器之间建立桥梁，使 AI 代理能够在 Unity 编辑器中执行各种操作。

## 核心功能

### 1. IDE 集成 - 包缓存访问

MCP Unity 通过将 Unity 的 `Library/PackedCache` 文件夹添加到工作区来提供与 VSCode 类似的 IDE（Visual Studio Code、Cursor、Windsurf）的自动集成。这个功能可以：

- 改善 Unity 包的代码智能提示
- 启用更好的自动完成和 Unity 包的类型信息
- 帮助 AI 编程助手理解项目的依赖关系

### 2. MCP 服务器工具

以下工具可通过 MCP 操作和查询 Unity 场景和 GameObjects：

- **`execute_menu_item`**: 执行 Unity 菜单项（具有 MenuItem 属性的函数）
- **`select_gameobject`**: 通过路径或实例 ID 在 Unity 层次结构中选择游戏对象
- **`update_gameobject`**: 更新 GameObject 的核心属性（名称、标签、层、激活/静态状态），或在不存在时创建 GameObject
- **`update_component`**: 更新 GameObject 上的组件字段或在 GameObject 不包含该组件时添加它
- **`add_package`**: 在 Unity 包管理器中安装新包
- **`run_tests`**: 使用 Unity Test Runner 运行测试
- **`send_console_log`**: 向 Unity 发送控制台日志
- **`add_asset_to_scene`**: 从 AssetDatabase 将资源添加到 Unity 场景

### 3. MCP 服务器资源

- **`unity://menu-items`**: 检索 Unity 编辑器中所有可用菜单项的列表
- **`unity://scenes-hierarchy`**: 检索当前 Unity 场景层次结构中所有游戏对象的列表
- **`unity://gameobject/{id}`**: 通过实例 ID 或场景层次结构中的对象路径检索特定 GameObject 的详细信息
- **`unity://logs`**: 检索 Unity 控制台的所有日志列表
- **`unity://packages`**: 检索 Unity 包管理器中已安装和可用包的信息
- **`unity://assets`**: 检索 Unity Asset Database 中资源的信息
- **`unity://tests/{testMode}`**: 检索 Unity Test Runner 中测试的信息

## 安装和配置

### 系统要求

- Unity 2022.3 或更高版本
- Node.js 18 或更高版本
- npm 9 或更高版本

### 步骤 1：安装 Unity MCP Server 包

1. 打开 Unity Package Manager（窗口 > Package Manager）
2. 点击左上角的"+"按钮
3. 选择"Add package from git URL..."
4. 输入：`https://github.com/CoderGamester/mcp-unity.git`
5. 点击"Add"

### 步骤 2：安装 Node.js

**Windows 用户：**

1. 访问 Node.js 下载页面
2. 下载 LTS 版本的 Windows 安装程序（.msi）
3. 运行安装程序并按照安装向导操作
4. 在 PowerShell 中运行以下命令验证安装：

   ```powershell
   node --version
   ```

**macOS 用户：**

1. 访问 Node.js 下载页面
2. 下载 LTS 版本的 macOS 安装程序（.pkg）
3. 运行安装程序并按照安装向导操作
4. 或者，如果安装了 Homebrew，可以运行：

   ```bash
   brew install node@18
   ```

5. 在终端中运行以下命令验证安装：

   ```bash
   node --version
   ```

### 步骤 3：配置 Cursor 编辑器

#### 方法 1：使用 Unity 编辑器配置（推荐）

1. 打开 Unity 编辑器
2. 导航到 Tools > MCP Unity > Server Window
3. 点击 Cursor 的"Configure"按钮
4. 确认安装配置弹窗

#### 方法 2：手动配置

打开 Cursor 的 MCP 配置文件，并添加以下配置：

```json
{
  "mcpServers": {
    "mcp-unity": {
      "command": "node",
      "args": ["ABSOLUTE/PATH/TO/mcp-unity/Server~/build/index.js"]
    }
  }
}
```

> **注意**：将 `ABSOLUTE/PATH/TO` 替换为您的 MCP Unity 安装的绝对路径。

## 在 Cursor 中使用 MCP Unity

### 启动 Unity Editor MCP Server

1. 打开 Unity 编辑器
2. 导航到 Tools > MCP Unity > Server Window
3. 点击"Start Server"启动 WebSocket 服务器
4. 打开 Cursor 编辑器，AI 客户端连接到 WebSocket 服务器时会自动显示在绿色框中

### 在 Cursor 中的实际应用

#### 1. 场景对象管理

您可以在 Cursor 中使用自然语言来操作 Unity 场景：

**示例提示词：**

- "创建一个名为'Player'的新空 GameObject"
- "选择场景中的 Main Camera 对象"
- "将 Player 对象的标签设置为'Enemy'并使其不活跃"

#### 2. 组件操作

通过 Cursor 添加和修改组件：

**示例提示词：**

- "给 Player 对象添加一个 Rigidbody 组件并将其质量设置为 5"
- "为所有敌人对象添加 Collider 组件"

#### 3. 包管理

通过 AI 助手管理 Unity 包：

**示例提示词：**

- "添加 TextMeshPro 包到我的项目"
- "列出当前项目中安装的所有包"

#### 4. 测试运行

使用 Cursor 运行 Unity 测试：

**示例提示词：**

- "运行项目中的所有 EditMode 测试"
- "显示最近的测试结果"

#### 5. 资源管理

管理项目资源：

**示例提示词：**

- "将 Player 预制体从项目添加到当前场景"
- "查找项目中的所有纹理资源"

#### 6. 调试和日志

通过 Cursor 查看和发送日志：

**示例提示词：**

- "显示 Unity 控制台中的最近错误消息"
- "向 Unity 编辑器发送一条控制台日志"

## 高级配置

### 自定义 WebSocket 端口

1. 打开 Unity 编辑器
2. 导航到 Tools > MCP Unity > Server Window
3. 将"WebSocket Port"值更改为所需的端口号
4. Unity 会将系统环境变量 UNITY_PORT 设置为新端口号
5. 重启 Node.js 服务器
6. 再次点击"Start Server"重新连接

### 设置超时时间

1. 打开 Unity 编辑器
2. 导航到 Tools > MCP Unity > Server Window
3. 将"Request Timeout (seconds)"值更改为所需的超时秒数
4. Unity 会将系统环境变量 UNITY_REQUEST_TIMEOUT 设置为新的超时值
5. 重启 Node.js 服务器

## 最佳实践

### 1. 工作流程优化

- **自动化重复任务**：使用 AI 助手自动化场景设置、资源导入等重复性工作
- **快速原型制作**：通过自然语言快速创建游戏对象和组件配置
- **测试自动化**：定期运行测试并分析结果

### 2. 代码质量提升

- **智能代码建议**：利用 Cursor 的 AI 功能获得 Unity 特定的代码建议
- **错误分析**：让 AI 助手分析 Unity 控制台错误并提供解决方案
- **性能优化**：询问 AI 关于 Unity 性能优化的最佳实践

### 3. 学习和文档

- **API 探索**：询问 AI 关于 Unity API 的使用方法
- **最佳实践指导**：获取 Unity 开发的最佳实践建议
- **问题解决**：通过 AI 助手快速解决开发中遇到的问题

## 故障排除

### 常见问题

#### 1. 无法连接到 MCP Unity

- 确保 WebSocket 服务器正在运行（检查 Unity 中的 Server Window）
- 从 MCP 客户端发送控制台日志消息以强制重新连接
- 在 Unity 编辑器 MCP Server 窗口中更改端口号

#### 2. MCP Unity 服务器无法启动

- 检查 Unity 控制台中的错误消息
- 确保 Node.js 已正确安装并可在您的 PATH 中访问
- 验证 Server 目录中的所有依赖项都已安装

#### 3. Play Mode 测试连接失败

这个错误发生是因为在切换到 Play Mode 时域重新加载导致桥接连接丢失。
**解决方案**：在 Edit > Project Settings > Editor > "Enter Play Mode Settings" 中关闭 **Reload Domain**。

## 调试和开发

### 使用 MCP Inspector 调试

如果需要调试服务器，可以使用 MCP Inspector：

```bash
npx @modelcontextprotocol/inspector node Server~/build/index.js
```

### 启用控制台日志

在 PowerShell 中：

```powershell
$env:LOGGING = "true"
$env:LOGGING_FILE = "true"
```

在命令提示符/终端中：

```bash
set LOGGING=true
set LOGGING_FILE=true
```

## 与 Unity 6.2 AI 功能的比较

MCP Unity 和即将推出的 Unity 6.2 AI 功能是互补的：

- **MCP Unity**：专注于编辑器自动化和交互，允许外部 AI 控制和查询 Unity 编辑器
- **Unity 6.2 AI**：专注于编辑器内内容创建（生成纹理、动画等）和内置 AI 功能

您可以同时使用两者来获得最佳的开发体验。

## 扩展和自定义

MCP Unity 具有很强的可扩展性：

### C# 端扩展

创建继承自 `McpToolBase` 的新 C# 类来暴露自定义 Unity 编辑器功能，然后在 `McpUnityServer.cs` 中注册这些工具。

### Node.js 端扩展

在 `Server/src/tools/` 目录中定义相应的 TypeScript 工具处理程序，包括 Zod 模式，并在 `Server/src/index.ts` 中注册。

## 总结

MCP Unity 与 Cursor 编辑器的结合为 Unity 开发者提供了强大的 AI 辅助开发环境。通过自然语言与 Unity 编辑器交互，开发者可以：

1. **提高开发效率**：自动化重复任务，专注于创意和复杂问题解决
2. **增强生产力**：通过 AI 助手直接与 Unity 编辑器功能交互
3. **改善可访问性**：让不熟悉 Unity 编辑器或 C# 脚本的用户也能对项目做出有意义的贡献
4. **无缝集成**：与各种支持 MCP 的 AI 助手和 IDE 协同工作

这个指南为您提供了开始使用 MCP Unity 与 Cursor 编辑器的完整流程。随着您熟悉这些工具，您会发现更多创新的使用方式来改善您的 Unity 开发工作流程。
