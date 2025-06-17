# PongHub代码风格规则

## 1. 网络同步实现
- 使用 `com.meta.multiplayer.netcode-photon` 包进行网络同步
- 网络同步类继承 `NetworkBehaviour`
- 使用 `NetworkVariable<T>` 进行状态同步
- 使用 `[ServerRpc]` 和 `[ClientRpc]` 进行远程调用
- 避免使用 `Photon.Pun` 命名空间，只使用 `Unity.Netcode`

## 2. 命名规范
- 私有字段使用 `m_` 前缀
- 公共属性使用 PascalCase
- 方法名使用 PascalCase
- 枚举值使用 PascalCase
- 常量使用全大写下划线分隔

## 3. 组件组织
- 使用 `[RequireComponent]` 标记必需的组件
- 在 `Awake` 中获取和验证组件引用
- 使用 `[SerializeField]` 标记需要在Inspector中配置的字段
- 使用 `[Header]` 对Inspector中的字段进行分组

## 4. 代码结构
- 字段声明顺序：组件引用、配置数据、状态变量
- 方法顺序：生命周期方法、公共方法、私有方法、属性
- 使用 `#region` 对代码进行分组（可选）
- 保持方法的单一职责

## 5. 注释规范
- 使用中文注释
- 为公共方法和属性添加注释
- 为复杂的逻辑添加注释
- 使用 `//` 进行单行注释，避免使用 `/* */`

## 6. 资源引用
- 使用 `ScriptableObject` 存储配置数据
- 使用 `[CreateAssetMenu]` 标记可创建的配置资源
- 使用 `Resources` 文件夹存储动态加载的资源
- 使用 `Addressables` 系统管理资源（如果项目使用）

## 7. 错误处理
- 使用 `Debug.LogWarning` 和 `Debug.LogError` 进行错误提示
- 在关键操作前进行空值检查
- 使用 `try-catch` 处理可能的异常
- 在 `OnDestroy` 中清理资源

## 8. 性能优化
- 缓存组件引用
- 避免在 `Update` 中分配内存
- 使用对象池管理频繁创建销毁的对象
- 使用 `[SerializeField]` 而不是 `GetComponent` 获取组件

## 9. 第三方包使用
- 优先使用 Meta 示例中使用的包
- 避免引入新的第三方包
- 使用 PongHub 项目中的工具类和辅助方法
- 保持与项目依赖相同的版本号

## 10. 代码示例
```csharp
using UnityEngine;
using Unity.Netcode;

namespace PongHub.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class ExampleComponent : NetworkBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private Rigidbody m_rigidbody;

        [Header("配置")]
        [SerializeField] private ExampleData m_data;

        // 网络同步变量
        private NetworkVariable<Vector3> m_networkPosition = new();

        private void Awake()
        {
            if (m_rigidbody == null)
                m_rigidbody = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                // 初始化网络状态
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ExampleServerRpc()
        {
            // 服务器逻辑
        }

        [ClientRpc]
        private void ExampleClientRpc()
        {
            // 客户端逻辑
        }

        // 属性
        public Vector3 NetworkPosition => m_networkPosition.Value;
    }
}
```