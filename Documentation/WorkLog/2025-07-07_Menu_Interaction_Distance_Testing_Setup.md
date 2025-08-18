# 菜单交互距离测试场景设置指南

## 概述
本文档描述了如何设置和使用菜单交互距离测试场景，以测试不同距离下的VR菜单交互体验。

## 场景设置步骤

### 1. 复制测试场景
1. 复制 `Assets/PongHub/Scenes/Testing/UI Menu Collection.unity` 场景
2. 重命名为 `Assets/PongHub/Scenes/Testing/Menu_Interaction_Distance_Test.unity`

### 2. 添加测试控制器
1. 在场景中创建一个空游戏对象，命名为 `MenuInteractionDistanceTester`
2. 添加 `MenuInteractionDistanceTester.cs` 脚本组件
3. 设置脚本引用:
   - 找到场景中的 `VRMenuInteraction` 组件并设置引用
   - 找到场景中的 `MenuInputHandler` 组件并设置引用
   - 设置需要测试的菜单面板的 Transform 引用

### 3. 创建测试UI面板
1. 创建一个新的Canvas，命名为 `TestControlCanvas`
2. 设置Canvas的渲染模式为 `World Space`
3. 将Canvas放置在玩家视野中容易看到但不干扰测试的位置
4. 在Canvas中添加以下UI元素:
   - 标题文本: "菜单交互距离测试"
   - 距离显示文本 (用于显示当前测试距离)
   - 状态显示文本 (用于显示交互成功率)
   - "下一距离"按钮
   - "上一距离"按钮
   - "重置统计"按钮
5. 设置 `MenuInteractionDistanceTester` 组件的UI引用:
   - 设置距离显示文本引用
   - 设置状态显示文本引用
   - 设置按钮引用

### 4. 配置测试菜单
1. 在场景中放置 `MainMenuPanel` 和 `GameModePanel` 预制件
2. 确保菜单面板的初始位置在第一个测试距离 (1.0m)
3. 确保菜单面板的交互组件正常工作

### 5. 添加测试指示器
1. 在场景中添加距离标记，以便在VR中直观地看到不同的测试距离
2. 可以使用简单的文本标记或线条标记来表示不同的距离

## 使用方法

### 测试流程
1. 打开测试场景
2. 进入VR模式
3. 使用"下一距离"和"上一距离"按钮切换不同的测试距离
4. 在每个距离下尝试与菜单进行交互
5. 观察状态显示文本中的交互成功率
6. 记录测试结果到工作日志中

### 数据收集
在每个测试距离下，记录以下数据:
- 交互成功率
- 交互精确度 (主观评分)
- 视觉反馈效果 (主观评分)
- 触觉反馈效果 (主观评分)
- 音频反馈效果 (主观评分)
- 总体用户体验 (主观评分)

### 测试完成后
1. 分析测试数据
2. 确定最佳的交互距离范围
3. 更新 `VRMenuInteraction.cs` 脚本中的 `m_interactionDistance` 参数
4. 优化视觉、触觉和音频反馈系统，以提供更好的交互体验