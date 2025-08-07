# SinglePlayerModePanel CodeBind 迁移工作单

**迁移日期**: 2025-08-06  
**迁移文件**: SinglePlayerModePanel.cs  
**原始组件数量**: 28个  

## 原始组件分析

### 面板配置 (5个)
1. `m_panelRoot` → GameObject → `PanelRoot_GO`
2. `m_titleText` → TextMeshProUGUI → `Title_Txt`
3. `m_modesContainer` → Transform → `ModesContainer_Tr`
4. `m_modeButtonPrefab` → GameObject → `ModeButtonPrefab_GO`
5. `m_backButton` → Button → `Back_Btn`

### 练习模式配置 (4个)
6. `m_practicePanel` → GameObject → `PracticePanel_GO`
7. `m_freePracticeButton` → Button → `FreePractice_Btn`
8. `m_targetPracticeButton` → Button → `TargetPractice_Btn`
9. `m_skillChallengeButton` → Button → `SkillChallenge_Btn`

### AI对战配置 (5个)
10. `m_aiPanel` → GameObject → `AIPanel_GO`
11. `m_difficultyContainer` → Transform → `DifficultyContainer_Tr`
12. `m_difficultyButtonPrefab` → GameObject → `DifficultyButtonPrefab_GO`
13. `m_difficultySlider` → Slider → `Difficulty_Sld`
14. `m_difficultyText` → TextMeshProUGUI → `DifficultyLevel_Txt`

### 个人成绩显示 (6个)
15. `m_statsPanel` → GameObject → `StatsPanel_GO`
16. `m_totalGamesText` → TextMeshProUGUI → `TotalGames_Txt`
17. `m_winRateText` → TextMeshProUGUI → `WinRate_Txt`
18. `m_bestScoreText` → TextMeshProUGUI → `BestScore_Txt`
19. `m_playTimeText` → TextMeshProUGUI → `PlayTime_Txt`
20. `m_lastPlayedText` → TextMeshProUGUI → `LastPlayed_Txt`

### 本地化键 (4个)
21. `m_titleKey` → string (配置参数，不需要绑定)
22. `m_practiceKey` → string (配置参数，不需要绑定)
23. `m_aiKey` → string (配置参数，不需要绑定)
24. `m_statsKey` → string (配置参数，不需要绑定)

## CodeBind 转换结果

**需要 CodeBind 绑定的组件**: 20个UI组件  
**不需要绑定的**: 4个配置字符串，8个非UI组件字段  

### 生成的属性映射

```csharp
// 自动生成的属性 (CodeBind)
public GameObject PanelRootGO { get; private set; }
public TextMeshProUGUI TitleTxt { get; private set; }
public Transform ModesContainerTr { get; private set; }
public GameObject ModeButtonPrefabGO { get; private set; }
public Button BackBtn { get; private set; }
public GameObject PracticePanelGO { get; private set; }
public Button FreePracticeBtn { get; private set; }
public Button TargetPracticeBtn { get; private set; }
public Button SkillChallengeBtn { get; private set; }
public GameObject AIPanelGO { get; private set; }
public Transform DifficultyContainerTr { get; private set; }
public GameObject DifficultyButtonPrefabGO { get; private set; }
public Slider DifficultySld { get; private set; }
public TextMeshProUGUI DifficultyLevelTxt { get; private set; }
public GameObject StatsPanelGO { get; private set; }
public TextMeshProUGUI TotalGamesTxt { get; private set; }
public TextMeshProUGUI WinRateTxt { get; private set; }
public TextMeshProUGUI BestScoreTxt { get; private set; }
public TextMeshProUGUI PlayTimeTxt { get; private set; }
public TextMeshProUGUI LastPlayedTxt { get; private set; }
```

## 迁移步骤清单

- [ ] 1. 备份原始文件
- [ ] 2. 添加 `[MonoCodeBind('_')]` 特性
- [ ] 3. 将类声明为 `partial`
- [ ] 4. 移除 SerializeField 字段声明
- [ ] 5. 更新代码中的所有组件引用
- [ ] 6. 保留配置字符串字段
- [ ] 7. 编译验证
- [ ] 8. 功能测试

**预计节省**: 从 15 分钟手动绑定缩短到 2 分钟自动生成