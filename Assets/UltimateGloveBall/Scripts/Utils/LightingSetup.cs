// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/*
GI (Global Illumination) 是全局光照的缩写，在Unity中是一个重要的光照系统概念。让我为您详细解释：

全局光照（GI）是一种模拟真实世界光照行为的技术，它考虑了光线在场景中的多次反射和散射。在Unity中，GI系统主要包含以下特点：

1. 直接光照：来自光源的直接照射
2. 间接光照：光线在物体表面之间反射产生的照明效果
3. 环境光遮蔽：模拟物体之间相互遮挡产生的阴影效果

从代码中我们可以看到，`LightingSetup`类主要用于管理场景中需要参与全局光照计算的物体：

1. 通过`ContributeGI`标志来控制物体是否参与全局光照计算
2. 在光照烘焙前，会为特定物体（如人群中的"Body"部分）启用GI贡献
3. 光照烘焙完成后，可以还原这些设置

这种设置对于优化场景的光照效果和性能非常重要，因为：
- 不是所有物体都需要参与全局光照计算
- 某些物体（如动态物体）不需要参与光照烘焙
- 通过精确控制GI贡献，可以在保证视觉效果的同时优化性能

在Unity中，全局光照可以通过实时GI或烘焙GI来实现，具体使用哪种方式取决于项目的需求和性能考虑。

*/

namespace UltimateGloveBall.Utils
{
    /// <summary>
    /// 光照设置工具类
    /// 用于在生成光照贴图前设置场景中的游戏对象
    /// 保存需要启用的游戏对象、需要设置为静态GI的对象以及人群对象的引用
    /// 可以通过Inspector中的组件上下文菜单触发:
    /// - Setup for Lighting: 为光照烘焙准备场景
    /// - Revert after lighting: 还原"Setup for Lighting"所做的更改
    /// </summary>
    public class LightingSetup : MonoBehaviour
    {
        [SerializeField] private GameObject[] m_objectsToEnable;        // 需要启用的游戏对象数组
        [SerializeField] private Transform m_crowdRoot;                 // 人群对象的根节点
        [SerializeField] private GameObject[] m_contributeToGIStatic;   // 需要设置为静态GI的对象数组

        /// <summary>
        /// 为光照烘焙设置场景
        /// 启用指定对象、处理人群对象并设置静态GI标志
        /// </summary>
        [ContextMenu("Setup for Lighting")]
        private void Setup()
        {
            // 启用所有指定的游戏对象
            foreach (var obj in m_objectsToEnable)
            {
                obj.SetActive(true);
            }

            // 递归处理人群对象
            ProcessCrowdRecursively(m_crowdRoot, true);

            // 处理需要贡献GI的静态对象
            ProcessContributeToGI(true);
        }

        /// <summary>
        /// 还原光照烘焙的设置
        /// 禁用之前启用的对象并还原人群对象的设置
        /// </summary>
        [ContextMenu("Revert after lighting")]
        private void RevertSetup()
        {
            // 禁用所有之前启用的游戏对象
            foreach (var obj in m_objectsToEnable)
            {
                obj.SetActive(false);
            }

            // 递归还原人群对象的设置
            ProcessCrowdRecursively(m_crowdRoot, false);
        }

        /// <summary>
        /// 递归处理人群对象的GI设置
        /// </summary>
        /// <param name="root">要处理的根节点Transform</param>
        /// <param name="forLighting">是否为光照烘焙设置</param>
        private void ProcessCrowdRecursively(Transform root, bool forLighting)
        {
            var go = root.gameObject;
            // 检查对象是否有Renderer组件
            if (go.TryGetComponent(out Renderer _))
            {
                // 如果对象名称包含"Body"，设置其GI标志
                if (go.name.Contains("Body"))
                {
                    SetContributeGIFlag(go, forLighting);
                }
            }
            // 递归处理所有子对象
            for (var i = 0; i < root.childCount; i++)
            {
                ProcessCrowdRecursively(root.GetChild(i), forLighting);
            }
        }

        /// <summary>
        /// 处理需要贡献GI的静态对象
        /// </summary>
        /// <param name="forLighting">是否为光照烘焙设置</param>
        private void ProcessContributeToGI(bool forLighting)
        {
            foreach (var go in m_contributeToGIStatic)
            {
                SetContributeGIFlag(go, forLighting);
            }
        }

        /// <summary>
        /// 设置游戏对象的ContributeGI标志
        /// </summary>
        /// <param name="go">要设置的游戏对象</param>
        /// <param name="forLighting">true表示启用GI贡献，false表示禁用</param>
        private void SetContributeGIFlag(GameObject go, bool forLighting)
        {
            if (go == null)
            {
                return;
            }
#if UNITY_EDITOR
            // 获取当前的静态标志
            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            if (forLighting)
            {
                // 添加ContributeGI标志
                GameObjectUtility.SetStaticEditorFlags(go, flags | StaticEditorFlags.ContributeGI);
            }
            else
            {
                // 移除ContributeGI标志
                GameObjectUtility.SetStaticEditorFlags(go, flags & ~StaticEditorFlags.ContributeGI);
            }
#endif
        }
    }
}