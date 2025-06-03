# Gym 场景优化结构建议

## 建议的新场景层次结构：

```
Gym Scene Root
├── 🎮 Game Area (游戏核心区域)
│   ├── Ball (乒乓球)
│   ├── Table (球桌 - 如果有的话)
│   ├── Net (球网 - 如果有的话)
│   └── Paddles (球拍 - 如果有的话)
│
├── 🏢 Environment (环境)
│   ├── Architecture (建筑结构)
│   │   ├── Walls (墙壁)
│   │   ├── Floor (地板)
│   │   └── Ceiling (天花板)
│   │
│   ├── Gym Equipment (体育器材)
│   │   ├── Exercise Equipment (运动器材)
│   │   │   ├── SportLadder01b 系列
│   │   │   ├── SportBench01 系列
│   │   │   └── SportBrus1 系列
│   │   │
│   │   └── Storage (储物)
│   │       └── Door02_1 系列
│   │
│   └── Furniture (家具装饰)
│       ├── Lighting Fixtures (照明装置)
│       │   └── LightLum01_03 系列
│       │
│       └── Decorative Items (装饰物品)
│
├── 💡 Lighting System (灯光系统)
│   ├── Main Lighting
│   │   └── Directional Light
│   │
│   ├── Area Lights
│   │   ├── Ceiling Lights (天花板灯光)
│   │   ├── Wall Lights (墙面灯光)
│   │   └── Accent Lights (重点照明)
│   │
│   └── Light Probe Group
│
├── 🎨 Post Processing
│   └── Global Volume
│
└── 📐 Technical (技术对象)
    ├── Spawn Points (生成点)
    ├── Collision Boundaries (碰撞边界)
    └── Audio Sources (音频源)
```

## 优化效果：

### ✅ 性能优化：

- **静态物件合并**：将不动的装饰物件标记为 Static，启用静态批处理
- **LOD 系统**：为远距离物件添加 LOD 组件
- **遮挡剔除**：合理配置遮挡剔除设置

### ✅ 开发效率：

- **层次清晰**：按功能分类，便于查找和修改
- **组织规范**：统一命名规范，便于团队协作
- **模块化**：核心游戏逻辑与环境装饰分离

### ✅ 维护性：

- **独立修改**：修改环境不影响游戏逻辑
- **版本控制友好**：减少场景合并冲突
- **扩展性好**：便于添加新的游戏元素

## 实施步骤：

1. **备份当前场景**
2. **创建新的父对象结构**
3. **重新分配现有 GameObject**
4. **设置 Static 标记**
5. **优化光照设置**
6. **测试游戏功能**
