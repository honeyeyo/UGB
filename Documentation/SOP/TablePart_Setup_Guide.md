# TablePart组件挂载SOP文档

## 概述
本文档详细说明如何为球桌的各个碰撞体部件正确挂载TablePart组件，实现差异化的碰撞检测和音效处理。

## 前置条件
- Unity 2022.3.52f1+已打开PongHub项目
- 已完成TablePart.cs和TablePartManager.cs脚本的添加
- TableSystem中的Table对象已配置多个BoxCollider组件

## 第一阶段：准备工作

### 1.1 确认Table对象结构
在Hierarchy中找到 `GameArea/TableSystem/Table` 对象，确认其结构：
```
Table (Root)
├── Multiple BoxCollider components
├── MeshRenderer
└── (其他组件)
```

### 1.2 识别现有Collider
Table对象上应该有多个BoxCollider组件，按从上到下顺序：
1. **第一个BoxCollider** - 桌面 (PhyMaterial: PhyWood)
2. **第二个BoxCollider** - 球网 (PhyMaterial: PhyNet)  
3. **第三个及以后** - 桌腿和横杠 (PhyMaterial: PhyMetalSolid)

## 第二阶段：创建子对象结构

### 2.1 创建桌面子对象
1. 右键点击Table对象 → Create Empty
2. 重命名为 `TableSurface`
3. 在Inspector中设置Tag为 `Untagged`（后续可创建专用Tag）
4. 移动第一个BoxCollider组件到TableSurface对象：
   - 选中TableSurface → Add Component → Box Collider
   - 在Table上右键第一个BoxCollider → Copy Component
   - 选中TableSurface的BoxCollider → 右键 → Paste Component Values
   - 删除Table上的第一个BoxCollider

### 2.2 创建球网子对象
1. 右键点击Table对象 → Create Empty
2. 重命名为 `TableNet`
3. 移动第二个BoxCollider组件到TableNet对象（同上述步骤）

### 2.3 创建桌腿支撑结构
1. 右键点击Table对象 → Create Empty
2. 重命名为 `TableSupports`
3. 为每个剩余的BoxCollider创建子对象：
   - 在TableSupports下创建空对象，命名为 `Leg1`, `Leg2`, `Support1` 等
   - 将对应的BoxCollider移动到各自的子对象

### 2.4 最终结构验证
完成后的结构应该如下：
```
Table (Root - 无BoxCollider)
├── TableSurface (1个BoxCollider - PhyWood)
├── TableNet (1个BoxCollider - PhyNet)
└── TableSupports (父容器)
    ├── Leg1 (1个BoxCollider - PhyMetalSolid)
    ├── Leg2 (1个BoxCollider - PhyMetalSolid)
    ├── Support1 (1个BoxCollider - PhyMetalSolid)
    └── Support2 (1个BoxCollider - PhyMetalSolid)
```

## 第三阶段：挂载TablePart组件

### 3.1 配置TableSurface
1. 选中TableSurface对象
2. 点击 `Add Component` → 搜索 `TablePart`
3. 配置TablePart组件：
   - **Part Type**: `Surface`
   - **Hit Sound**: 选择桌面击球音效
   - **Volume**: `1.0`
   - **Bounciness**: `1.0`
   - **Friction**: `1.0`
   - **Is Scoring Surface**: `✓ True`
   - **Causes Ball Death**: `☐ False`
   - **Debug Color**: `Green`

### 3.2 配置TableNet
1. 选中TableNet对象
2. 添加TablePart组件
3. 配置TablePart组件：
   - **Part Type**: `Net`
   - **Hit Sound**: 选择球网音效
   - **Volume**: `0.8`
   - **Bounciness**: `0.3`
   - **Friction**: `1.5`
   - **Is Scoring Surface**: `☐ False`
   - **Causes Ball Death**: `✓ True`
   - **Debug Color**: `Red`

### 3.3 配置TableSupports子对象
对每个桌腿和支撑对象重复以下步骤：

1. 选中子对象（如Leg1）
2. 添加TablePart组件
3. 配置TablePart组件：
   - **Part Type**: `Leg` (桌腿) 或 `Support` (横杠)
   - **Hit Sound**: 选择金属碰撞音效
   - **Volume**: `0.8`
   - **Bounciness**: `0.8`
   - **Friction**: `0.5`
   - **Is Scoring Surface**: `☐ False`
   - **Causes Ball Death**: `✓ True`
   - **Debug Color**: `Blue`

## 第四阶段：配置TablePartManager

### 4.1 添加管理器组件
1. 选中Table根对象
2. 点击 `Add Component` → 搜索 `TablePartManager`
3. 配置TablePartManager组件：
   - **Auto Find Parts**: `✓ True`
   - **Show Debug Info**: `✓ True`（调试时）

### 4.2 验证配置
1. 在TablePartManager上右键 → `Refresh Table Parts`
2. 在TablePartManager上右键 → `Validate Configuration`
3. 检查Console是否有错误信息

## 第五阶段：配置Ball对象集成

### 5.1 为Ball对象添加BallTableInteraction组件
1. 在Hierarchy中找到 `GameArea/BallSystem/Ball` 对象
2. 添加BallTableInteraction组件：
   - 点击 `Add Component` → 搜索 `BallTableInteraction`
3. 配置BallTableInteraction组件：
   - **Enable Table Part Detection**: `✓ True`
   - **Fallback to Tag Detection**: `✓ True`
   - **Volume Multiplier**: `1.0`
   - **Min Force for Sound**: `0.5`
   - **Show Debug Info**: `✓ True`（调试时）

### 5.2 修改Ball.cs的碰撞处理（可选）
如果需要集成TablePart系统到现有Ball碰撞处理，可以在Ball.cs的HandleCollision方法中添加：

```csharp
private void HandleCollision(Collision collision)
{
    // 现有代码...
    
    // 尝试使用TablePart系统
    var ballTableInteraction = GetComponent<BallTableInteraction>();
    if (ballTableInteraction != null && BallTableInteraction.IsTableRelated(collision.gameObject))
    {
        if (ballTableInteraction.HandleTableCollision(collision))
        {
            return; // TablePart系统已处理，不需要继续原有逻辑
        }
    }
    
    // 原有的碰撞处理逻辑...
}
```

## 第六阶段：测试验证

### 6.1 运行时测试
1. 进入Play模式
2. 在Scene视图中观察Gizmos（如果启用了Show Debug Info）
3. 查看Console中的调试信息

### 6.2 物理材质验证
1. 确认每个子对象的BoxCollider使用正确的PhysicMaterial：
   - TableSurface: PhyWood
   - TableNet: PhyNet
   - TableSupports子对象: PhyMetalSolid

### 6.3 碰撞测试
1. 创建测试球或使用现有Ball对象
2. 让球与不同部位碰撞
3. 观察Console中的TablePart调试信息
4. 验证不同音效是否正确播放

## 故障排除

### 常见问题
1. **TablePart组件找不到**：确认脚本已正确导入并编译
2. **碰撞检测不工作**：检查Collider的isTrigger设置（应为false）
3. **音效不播放**：确认AudioManager可用且音效文件已分配
4. **Physics Material丢失**：重新分配对应的PhysicMaterial
5. **Table脚本错误"Can't remove BoxCollider"**：Table脚本已更新，不再需要BoxCollider组件。现在Table主要作为空间锚点，碰撞检测由子对象的TablePart组件处理

### 调试技巧
1. 启用TablePart的`Show Debug Info`查看详细日志
2. 使用TablePartManager的Context Menu功能进行验证
3. 在Scene视图中启用Gizmos显示碰撞体边界

## 完成检查清单

- [ ] Table对象结构正确重组
- [ ] 所有子对象都有唯一的BoxCollider
- [ ] 所有子对象都挂载了TablePart组件
- [ ] TablePart配置符合部件类型
- [ ] TablePartManager已添加到Table根对象
- [ ] 通过了Validate Configuration检查
- [ ] 运行时测试无错误
- [ ] 碰撞检测和音效正常工作

## 注意事项

1. **不要直接修改共享的PhysicMaterial**：TablePart通过代码动态调整物理效果
2. **保持层级结构清晰**：便于后续维护和调试
3. **合理设置Debug模式**：发布版本时关闭调试信息
4. **定期验证配置**：修改结构后重新运行验证

---

**文档版本**: 1.0  
**最后更新**: 2025-08-16  
**适用版本**: Unity 2022.3.52f1+ / PongHub VR