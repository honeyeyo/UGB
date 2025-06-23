# PostGame 功能改进 - 使用说明

## 📋 概述

本模块实现了乒乓球VR游戏的PostGame功能改进，主要特性包括：

- **每局结束显示PostGame**：每打完一个11分的一局就显示PostGame画面
- **详细统计数据**：显示比分、技术统计数据和局数信息
- **多样化选择**：提供"再来一局"、"去观众位"、"退出房间"等按钮

## 🗂️ 文件结构

```text
PostGame/
├── GameStatistics.cs          # 游戏统计数据结构
├── GameStatisticsTracker.cs   # 统计数据跟踪器
├── PostGameController.cs      # PostGame主控制器
├── TechnicalStatsPanel.cs     # 技术统计面板
├── PostGameManager.cs         # PostGame管理器
└── README.md                   # 使用说明（本文件）
```

## 🏗️ 组件说明

### 1. GameStatistics

**功能**：定义游戏统计数据结构

- 基础比分（PlayerAScore, PlayerBScore）
- 局数信息（CurrentSet, PlayerASetsWon, PlayerBSetsWon）
- 技术统计（制胜球、失误、发球得分等）
- 时长统计（SetDuration, TotalGameTime）
- 回合统计（LongestRally, AverageRallyLength）

### 2. GameStatisticsTracker

**功能**：实时收集和跟踪比赛统计数据

- 监听分数变化事件
- 跟踪回合时长和击球次数
- 网络同步统计数据
- 提供统计数据更新事件

### 3. PostGameController

**功能**：管理PostGame界面显示和交互

- 显示比赛结果和统计数据
- 处理用户按钮交互
- 播放胜利效果（烟花、音效）
- 支持观众模式切换和退出功能

### 4. TechnicalStatsPanel

**功能**：专门显示技术统计数据

- 制胜球、失误、发球统计
- 回合统计和时间统计
- 获胜率计算和显示
- 视觉效果（高亮获胜方）

### 5. PostGameManager

**功能**：协调所有PostGame组件

- 自动查找和配置组件
- 统一管理PostGame显示/隐藏
- 提供便捷的配置接口

## 🔧 配置步骤

### Step 1: 在GameManager中配置

1. 打开GameManager.cs所在的GameObject
2. 在Inspector中找到"PostGame 改进组件"部分
3. 配置以下字段：
   - **Post Game Controller**: 拖入PostGameController组件
   - **Statistics Tracker**: 拖入GameStatisticsTracker组件
   - **Points Per Set**: 设置每局分数（默认11分）
   - **Sets To Win**: 设置获胜所需局数（默认3局）

## 📞 支持与反馈

如有问题或建议，请联系开发团队或在项目仓库中提交Issue。
