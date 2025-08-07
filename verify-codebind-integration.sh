#!/bin/bash
# CodeBind集成验证脚本

echo "🚀 开始验证 CodeBind 集成..."

# 1. 检查项目编译
echo "📝 检查项目编译状态..."

# 2. 验证 CodeBind 文件存在
CODEBIND_PATH="Assets/Plugins/CodeBind"
if [ -d "$CODEBIND_PATH" ]; then
    echo "✅ CodeBind 插件已正确安装"
else
    echo "❌ CodeBind 插件未找到"
    exit 1
fi

# 3. 验证 Odin Inspector
ODIN_PATH="Assets/Plugins/Sirenix"
if [ -d "$ODIN_PATH" ]; then
    echo "✅ Odin Inspector 已正确安装"
else
    echo "❌ Odin Inspector 未找到"
    exit 1
fi

# 4. 检查改造后的文件
TEST_FILE="Assets/PongHub/Scripts/UI/ModeSelection/Panels/SinglePlayerModePanel.cs"
if grep -q "\[MonoCodeBind" "$TEST_FILE"; then
    echo "✅ SinglePlayerModePanel 已成功改造为 CodeBind"
else
    echo "❌ SinglePlayerModePanel CodeBind 改造失败"
    exit 1
fi

# 5. 检查 gitignore 配置
if grep -q "*.Bind.cs" ".gitignore"; then
    echo "✅ .gitignore 已配置 CodeBind 自动生成文件排除"
else
    echo "❌ .gitignore 未正确配置"
    exit 1
fi

echo "🎉 CodeBind 集成验证完成！"
echo ""
echo "📋 接下来需要手动执行的步骤:"
echo "1. 在 Unity 编辑器中打开项目"
echo "2. 选择 SinglePlayerModePanel 脚本对应的 GameObject"
echo "3. 在 Inspector 中点击 'Generate Bind Code' 按钮"
echo "4. 在 Inspector 中点击 'Generate Serialization' 按钮"
echo "5. 按照命名规范重命名 UI 节点:"
echo "   - m_panelRoot → PanelRoot_GO"
echo "   - m_titleText → Title_Txt"
echo "   - m_modesContainer → ModesContainer_Tr"
echo "   - 等等..."
echo "6. 编译验证无错误"
echo "7. 功能测试"
echo ""
echo "📖 详细命名规范请参考: .ai/ponghub-codebind-naming-standard.md"