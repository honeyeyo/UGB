# Markdownlint 规则说明

## 📋 概述

本项目使用 Markdownlint 工具确保所有 Markdown 文档的格式一致性和可读性。所有新创建的文档都必须通过 Markdown Lint 验证。

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
  "MD041": false,
  "MD022": true,
  "MD032": true,
  "MD031": true,
  "MD040": true,
  "MD047": true,
  "MD001": true,
  "MD009": true
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
8. **行长度 (MD013)**: 限制行长度为120字符

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

### VSCode 集成

安装 VSCode 扩展：

```text
Name: markdownlint
Publisher: David Anson
```

配置 VSCode 设置：

```json
{
  "markdownlint.config": {
    "MD013": {
      "line_length": 120,
      "heading_line_length": 120,
      "code_block_line_length": 120
    },
    "MD033": false,
    "MD041": false
  }
}
```

## ✅ 文档创建流程

### 标准流程

1. **编写文档内容**
2. **运行自动修复**: `markdownlint filename.md --fix`
3. **手动检查**: 修复剩余的格式问题
4. **验证通过**: `markdownlint filename.md` 无错误输出
5. **提交文档**

### 自动化检查

在 Git 提交前自动检查：

```bash
# 添加到 .git/hooks/pre-commit
#!/bin/sh
markdownlint Documentation/*.md
if [ $? -ne 0 ]; then
  echo "Markdown lint failed. Please fix the issues before committing."
  exit 1
fi
```

## 📋 常见问题修复

### 标题周围缺少空行 (MD022)

```markdown
## 错误示例
文本内容
### 下一个标题

## 正确示例

文本内容

### 下一个标题
```

### 列表周围缺少空行 (MD032)

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

### 代码块缺少语言标识 (MD040)

**错误示例**：

````markdown
```
代码内容
```
````

**正确示例**：

````markdown
```csharp
代码内容
```
````

### 代码块周围缺少空行 (MD031)

**错误示例**：

````markdown
文本内容
```csharp
代码内容
```
更多文本
````

**正确示例**：

````markdown
文本内容

```csharp
代码内容
```

更多文本
````

### 标题层级跳跃 (MD001)

```markdown
## 错误示例
# 标题1
### 标题3 (跳过了2级标题)

## 正确示例
# 标题1
## 标题2
### 标题3
```

### 行长度超限 (MD013)

```markdown
## 错误示例
这是一行非常长的文本，超过了120个字符的限制，需要进行换行处理以符合Markdown格式规范要求。

## 正确示例
这是一行非常长的文本，超过了120个字符的限制，需要进行换行处理
以符合Markdown格式规范要求。
```

### 行尾空格 (MD009)

```markdown
## 错误示例
文本内容[空格][空格]
下一行

## 正确示例
文本内容
下一行
```

## 🎯 目标

通过遵循这些规则，我们确保：

- **文档格式一致性**: 所有文档遵循统一标准
- **更好的可读性**: 标准化格式提升阅读体验
- **更容易维护**: 一致的格式便于文档维护
- **支持自动化处理**: 标准格式支持工具链集成
- **符合行业标准**: 遵循Markdown最佳实践

## 🔄 AI文档生成规则

### 强制要求

所有AI生成的Markdown文档必须：

1. **自动Lint检查**: 生成后立即进行格式验证
2. **空行规范**: 严格遵循标题、列表、代码块的空行要求
3. **代码块标识**: 所有代码块必须指定正确的语言
4. **行长度控制**: 自动处理超长行的换行
5. **结构验证**: 确保标题层级的连续性

### 质量保证

- **零Lint错误**: 生成的文档必须通过所有Lint检查
- **格式一致**: 与现有文档保持相同的格式标准
- **可读性优先**: 在符合规则的前提下优化可读性
- **自动修复**: 优先使用可自动修复的格式错误

所有新创建的文档都已通过 Markdownlint 验证！
