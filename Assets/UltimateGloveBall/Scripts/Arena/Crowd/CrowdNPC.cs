// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using UnityEngine;

namespace PongHub.Arena.Crowd
{
    /// <summary>
    /// NPC观众成员的表现类。主要功能包括:
    /// 1. 设置观众的面部表情
    /// 2. 更改观众的附件和身体颜色
    /// 3. 初始化时随机设置动画的开始时间和播放速度
    /// 4. 切换观众手持的道具
    /// </summary>
    public class CrowdNPC : MonoBehaviour
    {
        // 着色器属性ID
        private static readonly int s_bodyColorID = Shader.PropertyToID("_Body_Color");         // 身体颜色属性ID
        private static readonly int s_attachmentColorID = Shader.PropertyToID("_Attachment_Color"); // 附件颜色属性ID
        private static readonly int s_faceSwapID = Shader.PropertyToID("_Face_swap");          // 面部表情属性ID

        [SerializeField] private Animator[] m_animators;               // 动画控制器数组
        [SerializeField] private Renderer m_faceRenderer;              // 面部渲染器
        [SerializeField] private Renderer[] m_attachmentsRenderers;    // 附件渲染器数组
        [SerializeField] private Renderer m_bodyRenderer;              // 身体渲染器

        [SerializeField] private GameObject[] m_items;                 // 可持有的道具数组

        private int m_currentItemIndex;                               // 当前持有的道具索引
        private MaterialPropertyBlock m_materialBlock;                // 材质属性块

        /// <summary>
        /// 初始化时确定当前持有的道具
        /// </summary>
        private void Awake()
        {
            for (var i = 0; i < m_items.Length; ++i)
            {
                if (m_items[i].activeSelf)
                {
                    m_currentItemIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// 初始化观众,设置动画参数和面部表情
        /// </summary>
        /// <param name="timeOffset">动画开始时间的偏移量</param>
        /// <param name="speed">动画播放速度</param>
        /// <param name="face">面部表情的UV坐标</param>
        public void Init(float timeOffset, float speed, Vector2 face)
        {
            foreach (var animator in m_animators)
            {
                if (animator != null)
                {
                    animator.speed = speed;
                    if (animator.isActiveAndEnabled)
                    {
                        var info = animator.GetCurrentAnimatorStateInfo(0);
                        animator.Play(info.shortNameHash, 0, timeOffset);
                    }
                }
            }

            m_materialBlock ??= new MaterialPropertyBlock();
            m_faceRenderer.GetPropertyBlock(m_materialBlock);
            m_materialBlock.SetVector(s_faceSwapID, face);
            m_faceRenderer.SetPropertyBlock(m_materialBlock);
        }

        /// <summary>
        /// 设置观众附件的颜色(用于表示支持的队伍)
        /// </summary>
        /// <param name="color">附件颜色</param>
        public void SetColor(Color color)
        {
            m_materialBlock ??= new MaterialPropertyBlock();
            foreach (var rend in m_attachmentsRenderers)
            {
                if (rend != null)
                {
                    rend.GetPropertyBlock(m_materialBlock);
                    m_materialBlock.SetColor(s_attachmentColorID, color);
                    rend.SetPropertyBlock(m_materialBlock);
                }
            }
        }

        /// <summary>
        /// 设置观众身体的颜色
        /// </summary>
        /// <param name="color">身体颜色</param>
        public void SetBodyColor(Color color)
        {
            m_materialBlock ??= new MaterialPropertyBlock();
            m_bodyRenderer.GetPropertyBlock(m_materialBlock);
            m_materialBlock.SetColor(s_bodyColorID, color);
            m_bodyRenderer.SetPropertyBlock(m_materialBlock);
        }

        /// <summary>
        /// 切换观众手持的道具
        /// </summary>
        /// <param name="itemIndex">新道具的索引</param>
        public void ChangeItem(int itemIndex)
        {
            if (itemIndex >= 0 && itemIndex < m_items.Length)
            {
                m_items[m_currentItemIndex].SetActive(false);
                m_items[itemIndex].SetActive(true);
                m_currentItemIndex = itemIndex;
            }
        }
    }
}