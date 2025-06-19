# Markdownlint 规则说明

## 📋 概述

本项目使用 Markdownlint 工具确保所有 Markdown 文档的格式一致性和可读性。

## 🔧 配置文件

项目根目录的 `.markdownlint.json` 文件包含以下配置：

```json
{
  "MD013": {
    "line_length": 120,
    "heading_line_length": 120,
    "code_block_line_length": 120
  },
  "MD033": false,
  "MD041": false
}
```

## 📝 主要规则

### 必须遵守的规则

1. **标题格式 (MD022)**: 标题前后必须有空行
2. **列表格式 (MD032)**: 列表前后必须有空行
3. **代码块格式 (MD031)**: 代码块前后必须有空行
4. **代码块语言 (MD040)**: 所有代码块必须指定语言
5. **文件结尾 (MD047)**: 文件必须以单个换行符结尾
6. **标题层级 (MD001)**: 标题层级必须递增，不可跳跃
7. **尾随空格 (MD009)**: 不允许行尾空格

### 已禁用的规则

- **MD033**: 允许内联HTML（为了支持表格和特殊格式）
- **MD041**: 允许文档不以 h1 标题开始

## 🛠️ 使用方法

### 安装 Markdownlint

```bash
npm install -g markdownlint-cli
```

### 检查文档

```bash
# 检查单个文件
markdownlint filename.md

# 检查多个文件
markdownlint Documentation/*.md

# 自动修复可修复的问题
markdownlint filename.md --fix
```

## ✅ 验证流程

在创建或修改 Markdown 文档时：

1. 编写文档内容
2. 运行 `markdownlint filename.md --fix` 自动修复
3. 手动检查并修复剩余问题
4. 确认 `markdownlint filename.md` 无错误输出

## 📋 常见问题修复

### 标题周围缺少空行

```markdown
## 错误示例
文本内容
### 下一个标题

## 正确示例

文本内容

### 下一个标题
```

### 列表周围缺少空行

```markdown
## 错误示例
文本内容
- 列表项1
- 列表项2
更多文本

## 正确示例

文本内容

- 列表项1
- 列表项2

更多文本
```

### 代码块缺少语言标识

```markdown
## 错误示例

```text
代码内容
```

## 正确示例

```csharp
代码内容
```

## 🎯 目标

通过遵循这些规则，我们确保：

- 文档格式一致性
- 更好的可读性
- 更容易维护
- 支持自动化处理
- 符合行业标准

所有新创建的音频系统文档都已通过 Markdownlint 验证！
