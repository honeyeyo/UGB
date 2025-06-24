# 乒乓球VR游戏 PostGame 功能改进设计文档

## 📋 项目概述

### 当前状态

- PostGame场景仅在整场比赛结束后显示
- 只有简单的重新开始按钮和烟花效果
- 缺少详细的比分统计和技术数据展示
- 没有灵活的游戏选项（观众模式、退出等）

### 改进目标

- **每局结束显示**：每打完一个11分的一局就显示PostGame画面
- **详细统计**：显示比分、技术统计数据和局数信息
- **多样选择**：提供"再来一局"、"去观众位"、"退出房间"等按钮

## 🎯 功能设计

### 1. 游戏流程改进

#### 1.1 当前流程

```text
比赛开始 → 游戏进行 → 整场比赛结束 → PostGame
```

#### 1.2 改进后流程

```text
比赛开始 → 第一局游戏 → 局结束PostGame → 选择继续/观战/退出
                ↓
            下一局开始 → 游戏进行 → 局结束PostGame → ...
                ↓
            达到局数上限 → 最终PostGame → 显示总成绩
```

### 2. PostGame触发机制

#### 2.1 触发条件

- **单局结束**：任一方达到11分且领先2分以上
- **平分规则**：10平后每2分差距触发
- **最大分数**：防止无限延长的最大分数限制

#### 2.2 数据收集

```csharp
public class GameStatistics
{
    // 基础比分
    public int PlayerAScore { get; set; }
    public int PlayerBScore { get; set; }
    public int CurrentSet { get; set; }
    public int MaxSets { get; set; }

    // 技术统计
    public int PlayerAWinners { get; set; }      // 制胜球
    public int PlayerAErrors { get; set; }       // 失误
    public int PlayerAServeAces { get; set; }    // 发球得分
    public int PlayerAReturnWins { get; set; }   // 接发球得分

    public int PlayerBWinners { get; set; }
    public int PlayerBErrors { get; set; }
    public int PlayerBServeAces { get; set; }
    public int PlayerBReturnWins { get; set; }

    // 比赛时长
    public float SetDuration { get; set; }
    public float TotalGameTime { get; set; }

    // 最长回合
    public int LongestRally { get; set; }
    public float AverageRallyLength { get; set; }
}
```

## 🎨 UI界面设计

### 3. PostGame界面布局

#### 3.1 主要区域划分

```text
┌─────────────────────────────────────┐
│              比赛结果标题            │
│          "第X局 - 玩家A获胜"         │
├─────────────────────────────────────┤
│  比分显示    │    技术统计区域       │
│   A: 11     │  制胜球: A-8  B-6    │
│   B: 7      │  失误数: A-3  B-5    │
│             │  发球得分: A-2 B-3   │
│             │  最长回合: 15次       │
├─────────────────────────────────────┤
│           局数进度条                │
│    ●●○  (当前2-1领先)              │
├─────────────────────────────────────┤
│  [再来一局]  [去观众位]  [退出房间]  │
└─────────────────────────────────────┘
```

#### 3.2 按钮功能说明

- **再来一局**：继续下一局比赛，重置球桌，倒计时开始
- **去观众位**：切换到观众模式，可以观看其他玩家比赛
- **退出房间**：离开当前房间，返回主菜单

### 4. 数据展示优化

#### 4.1 比分显示增强

```csharp
public class ScoreDisplayData
{
    public string WinnerText { get; set; }        // "玩家A获胜" 或 "平局"
    public Color WinnerColor { get; set; }        // 获胜方颜色
    public string ScoreText { get; set; }         // "11 - 7"
    public string SetProgressText { get; set; }   // "第2局 (总比分 2-1)"
    public bool IsMatchComplete { get; set; }     // 是否完成整场比赛
}
```

#### 4.2 技术统计面板

```csharp
public class TechnicalStatsPanel : MonoBehaviour
{
    [Header("统计显示")]
    [SerializeField] private TMP_Text winnersStatsText;
    [SerializeField] private TMP_Text errorsStatsText;
    [SerializeField] private TMP_Text serveStatsText;
    [SerializeField] private TMP_Text rallyStatsText;
    [SerializeField] private TMP_Text timeStatsText;

    public void UpdateStats(GameStatistics stats)
    {
        winnersStatsText.text = $"制胜球: {stats.PlayerAWinners} - {stats.PlayerBWinners}";
        errorsStatsText.text = $"失误: {stats.PlayerAErrors} - {stats.PlayerBErrors}";
        serveStatsText.text = $"发球得分: {stats.PlayerAServeAces} - {stats.PlayerBServeAces}";
        rallyStatsText.text = $"最长回合: {stats.LongestRally}次";
        timeStatsText.text = $"本局用时: {FormatTime(stats.SetDuration)}";
    }
}
```

## 🔧 技术实现

### 5. 代码结构改动

#### 5.1 GameManager 扩展

```csharp
public class GameManager : NetworkBehaviour
{
    // 新增字段
    [SerializeField] private PostGameController m_postGameController;
    [SerializeField] private GameStatisticsTracker m_statisticsTracker;
    [SerializeField] private int m_maxSetsInMatch = 5; // 五局三胜

    // 新增事件
    public static event Action<GameStatistics> OnSetCompleted;
    public static event Action<GameStatistics> OnMatchCompleted;

    // 修改现有方法
    private void CheckGameEnd()
    {
        var score = m_gameState.Score;
        var teamAScore = score.GetTeamScore(NetworkedTeam.Team.TeamA);
        var teamBScore = score.GetTeamScore(NetworkedTeam.Team.TeamB);

        // 检查是否达到单局结束条件
        if (IsSetComplete(teamAScore, teamBScore))
        {
            EndCurrentSet();
        }
    }

    private bool IsSetComplete(int scoreA, int scoreB)
    {
        // 基本获胜条件：11分且领先2分
        if ((scoreA >= 11 && scoreA - scoreB >= 2) ||
            (scoreB >= 11 && scoreB - scoreA >= 2))
        {
            return true;
        }

        // 平分延长赛：10-10后领先2分
        if (scoreA >= 10 && scoreB >= 10)
        {
            return Math.Abs(scoreA - scoreB) >= 2;
        }

        return false;
    }

    private void EndCurrentSet()
    {
        var stats = m_statisticsTracker.GetCurrentStatistics();
        OnSetCompleted?.Invoke(stats);

        // 检查是否完成整场比赛
        if (IsMatchComplete())
        {
            EndMatch();
        }
        else
        {
            // 显示PostGame界面等待用户选择
            m_postGameController.ShowPostGameUI(stats, false);
        }
    }
}
```

#### 5.2 PostGameController 重构

```csharp
public class PostGameController : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private GameObject m_postGamePanel;
    [SerializeField] private TMP_Text m_resultTitle;
    [SerializeField] private TMP_Text m_scoreDisplay;
    [SerializeField] private TechnicalStatsPanel m_statsPanel;
    [SerializeField] private SetProgressDisplay m_progressDisplay;

    [Header("按钮")]
    [SerializeField] private Button m_nextSetButton;
    [SerializeField] private Button m_spectatorButton;
    [SerializeField] private Button m_exitButton;

    [Header("效果")]
    [SerializeField] private FireworksController m_fireworks;
    [SerializeField] private AudioSource m_victoryAudio;

    public void ShowPostGameUI(GameStatistics stats, bool isMatchComplete)
    {
        m_postGamePanel.SetActive(true);

        // 更新显示内容
        UpdateResultDisplay(stats, isMatchComplete);
        UpdateStatsDisplay(stats);
        UpdateProgressDisplay(stats);

        // 设置按钮状态
        m_nextSetButton.gameObject.SetActive(!isMatchComplete);
        m_nextSetButton.GetComponentInChildren<TMP_Text>().text =
            isMatchComplete ? "新的比赛" : "下一局";

        // 播放效果
        if (stats.CurrentSetWinner != NetworkedTeam.Team.NoTeam)
        {
            m_fireworks.PlayFireworks();
            m_victoryAudio.Play();
        }

        // 设置按钮事件
        SetupButtonEvents(isMatchComplete);
    }

    private void SetupButtonEvents(bool isMatchComplete)
    {
        m_nextSetButton.onClick.RemoveAllListeners();
        m_spectatorButton.onClick.RemoveAllListeners();
        m_exitButton.onClick.RemoveAllListeners();

        m_nextSetButton.onClick.AddListener(() => {
            if (isMatchComplete)
            {
                GameManager.Instance.StartNewMatch();
            }
            else
            {
                GameManager.Instance.StartNextSet();
            }
            HidePostGameUI();
        });

        m_spectatorButton.onClick.AddListener(() => {
            SwitchToSpectatorMode();
        });

        m_exitButton.onClick.AddListener(() => {
            ExitToMainMenu();
        });
    }

    private void SwitchToSpectatorMode()
    {
        // 实现切换到观众模式的逻辑
        var spawningManager = ArenaPlayerSpawningManager.Instance;
        var clientId = NetworkManager.Singleton.LocalClientId;

        // 销毁当前玩家对象，生成观众对象
        if (LocalPlayerEntities.Instance.Avatar != null)
        {
            LocalPlayerEntities.Instance.Avatar.NetworkObject.Despawn();
        }

        // 在观众席生成
        spawningManager.SpawnPlayer(clientId,
            LocalPlayerState.Instance.PlayerId,
            true, // isSpectator = true
            Vector3.zero);

        HidePostGameUI();
    }

    private void ExitToMainMenu()
    {
        // 断开网络连接并返回主菜单
        NetworkManager.Singleton.Shutdown();
        NavigationController.Instance.GoToMainMenu();
    }
}
```

#### 5.3 统计数据跟踪器

```csharp
public class GameStatisticsTracker : NetworkBehaviour
{
    private GameStatistics m_currentStats;
    private GameStatistics m_matchTotalStats;

    [Header("跟踪设置")]
    [SerializeField] private float m_rallyStartDelay = 1f;

    private float m_rallyStartTime;
    private int m_currentRallyHits;
    private bool m_rallyInProgress;

    public override void OnNetworkSpawn()
    {
        // 监听球的事件
        Ball.OnBallHit += OnBallHit;
        Ball.OnBallScored += OnBallScored;

        // 监听游戏事件
        GameManager.OnSetCompleted += OnSetCompleted;

        ResetSetStatistics();
    }

    private void OnBallHit(Vector3 hitPoint, NetworkedTeam.Team hittingTeam)
    {
        if (!m_rallyInProgress)
        {
            m_rallyStartTime = Time.time;
            m_rallyInProgress = true;
            m_currentRallyHits = 0;
        }

        m_currentRallyHits++;

        // 根据击球类型判断是否为制胜球或失误
        // 这里需要结合球的轨迹和速度进行判断
    }

    private void OnBallScored(NetworkedTeam.Team scoringTeam, bool isWinner)
    {
        // 更新得分统计
        if (scoringTeam == NetworkedTeam.Team.TeamA)
        {
            if (isWinner)
                m_currentStats.PlayerAWinners++;
            else
                m_currentStats.PlayerBErrors++;
        }
        else
        {
            if (isWinner)
                m_currentStats.PlayerBWinners++;
            else
                m_currentStats.PlayerAErrors++;
        }

        // 更新最长回合记录
        if (m_currentRallyHits > m_currentStats.LongestRally)
        {
            m_currentStats.LongestRally = m_currentRallyHits;
        }

        m_rallyInProgress = false;
    }

    public GameStatistics GetCurrentStatistics()
    {
        m_currentStats.SetDuration = Time.time - m_setStartTime;
        return m_currentStats;
    }
}
```

### 6. 网络同步考虑

#### 6.1 数据同步策略

```csharp
public class NetworkedGameStatistics : NetworkBehaviour
{
    [SerializeField] private NetworkVariable<GameStatisticsData> m_networkStats = new();

    public void UpdateStatistics(GameStatistics stats)
    {
        if (IsServer)
        {
            m_networkStats.Value = new GameStatisticsData(stats);
        }
    }

    [Serializable]
    public struct GameStatisticsData : INetworkSerializable
    {
        public int playerAScore;
        public int playerBScore;
        public int playerAWinners;
        public int playerBWinners;
        // ... 其他统计数据

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref playerAScore);
            serializer.SerializeValue(ref playerBScore);
            serializer.SerializeValue(ref playerAWinners);
            serializer.SerializeValue(ref playerBWinners);
            // ... 序列化其他字段
        }
    }
}
```

## 📋 实施计划

### 7. 开发阶段

#### 阶段一：基础功能（第1-2周）

- [ ] 修改GameManager的局结束检测逻辑
- [ ] 创建PostGameController基础结构
- [ ] 实现基本的UI布局和显示

#### 阶段二：统计系统（第3-4周）

- [ ] 实现GameStatisticsTracker
- [ ] 添加技术统计的收集逻辑
- [ ] 创建统计数据的UI显示组件

#### 阶段三：网络同步（第5周）

- [ ] 实现NetworkedGameStatistics
- [ ] 确保多人游戏中的数据一致性
- [ ] 测试网络断线重连的数据恢复

#### 阶段四：用户选择功能（第6周）

- [ ] 实现"再来一局"功能
- [ ] 实现"去观众位"模式切换
- [ ] 实现"退出房间"功能

#### 阶段五：优化和测试（第7-8周）

- [ ] UI动画和视觉效果优化
- [ ] 性能优化和内存管理
- [ ] 全面测试和bug修复

### 8. 配置和资源需求

#### 8.1 UI资源

- PostGame界面的背景图片
- 按钮的正常/悬停/按下状态贴图
- 统计图表的图标和装饰元素
- 局数进度指示器的视觉元素

#### 8.2 音效资源

- 局结束的音效
- 按钮点击音效
- 观众模式切换音效

#### 8.3 配置文件更新

```json
{
  "gameSettings": {
    "setsToWin": 3,
    "maxSetsInMatch": 5,
    "pointsPerSet": 11,
    "minimumLeadToWin": 2,
    "enableDeuce": true,
    "maxPointsInDeuce": 21
  },
  "postGameSettings": {
    "showStatistics": true,
    "autoProgressDelay": 10,
    "enableSpectatorSwitch": true,
    "fireworksDuration": 3.0
  }
}
```

## 🧪 测试计划

### 9. 测试用例

#### 9.1 功能测试

- [ ] 单局11分正常结束触发PostGame
- [ ] 平分情况下的正确触发
- [ ] 多局比赛的连续流程
- [ ] 统计数据的准确性
- [ ] 各按钮功能的正确执行

#### 9.2 网络测试

- [ ] 多人游戏中PostGame的同步显示
- [ ] 观众模式切换的网络同步
- [ ] 主机迁移时的状态保持

#### 9.3 边界测试

- [ ] 网络断线时的处理
- [ ] 快速连续得分的统计准确性
- [ ] 极长比赛时间的性能表现

## 📝 总结

这个改进方案将显著提升游戏的用户体验，通过：

1. **增强的比赛节奏**：每局结束后的停顿让玩家有时间消化比赛结果
2. **丰富的数据展示**：详细的技术统计满足竞技性需求
3. **灵活的游戏选择**：多样化的后续选项适应不同玩家需求
4. **完善的观众体验**：无缝的观众模式切换增加游戏的社交性

实施完成后，游戏将具备更专业的竞技体验和更强的可玩性。
