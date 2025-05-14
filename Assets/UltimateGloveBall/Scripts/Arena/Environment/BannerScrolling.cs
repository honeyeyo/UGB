// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.Utilities;
using UnityEngine;

namespace UltimateGloveBall.Arena.Environment
{
    /// <summary>
    /// 控制竞技场横幅的滚动。主要功能包括:
    /// 1. 横幅的滚动动画
    /// 2. 定期更换横幅上的Logo
    /// </summary>
    public class BannerScrolling : MonoBehaviour
    {
        /// <summary>
        /// 横幅动画的阶段枚举
        /// </summary>
        private enum Phases
        {
            Paused,     // 暂停阶段
            Scroll,     // 滚动阶段
            Swap,       // 切换Logo阶段
        }
        [SerializeField, AutoSet] private MeshRenderer m_meshRenderer;  // 横幅的网格渲染器

        [SerializeField] private float m_movementStep = -0.05f;        // 横向滚动步长
        [SerializeField] private float m_stepSpeed = 0.1f;            // 滚动速度

        [SerializeField] private float m_swapStep = 0.1f;             // Logo切换步长

        [SerializeField] private float m_scrollTime = 2f;             // 滚动持续时间
        [SerializeField] private float m_pauseTime = 2f;              // 暂停持续时间

        private float m_timer;                                        // 阶段计时器
        private float m_stepTimer;                                    // 步进计时器
        private int m_stepCount;                                      // 步进计数器
        private Phases m_phase = Phases.Paused;                       // 当前动画阶段
        private Vector2 m_scrollPosition = Vector2.zero;              // 纹理偏移位置

        private Material m_material;                                  // 横幅材质

        /// <summary>
        /// 初始化时获取横幅材质
        /// </summary>
        private void Awake()
        {
            // 由于只有一个实例,直接使用材质而不需要属性块
            m_material = m_meshRenderer.material;
        }

        /// <summary>
        /// 每帧更新横幅动画状态
        /// </summary>
        private void Update()
        {
            m_timer += Time.deltaTime;

            switch (m_phase)
            {
                case Phases.Paused:
                    HandlePause();
                    break;
                case Phases.Scroll:
                    HandleScroll();
                    break;
                case Phases.Swap:
                    HandleSwap();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 处理暂停阶段
        /// </summary>
        private void HandlePause()
        {
            if (m_timer >= m_pauseTime)
            {
                m_timer -= m_pauseTime;
                m_phase = Phases.Scroll;
            }
        }

        /// <summary>
        /// 处理横向滚动阶段
        /// </summary>
        private void HandleScroll()
        {
            if (m_timer >= m_scrollTime)
            {
                m_timer -= m_scrollTime;
                m_phase = Phases.Swap;
                m_stepTimer = 0;
            }
            else
            {
                m_stepTimer += Time.deltaTime;
                if (m_stepTimer >= m_stepSpeed)
                {
                    m_stepTimer -= m_stepSpeed;
                    ScrollImage();
                }
            }
        }

        /// <summary>
        /// 处理Logo切换阶段
        /// </summary>
        private void HandleSwap()
        {
            m_stepTimer += Time.deltaTime;
            if (m_stepTimer >= m_stepSpeed)
            {
                m_stepCount++;
                m_stepTimer -= m_stepSpeed;
                SwapImage();
            }

            if (m_stepCount >= Mathf.Abs(1f / m_swapStep))
            {
                m_timer = 0;
                m_phase = Phases.Paused;
                m_stepTimer = 0;
                m_stepCount = 0;
            }
        }

        /// <summary>
        /// 执行横幅横向滚动
        /// </summary>
        private void ScrollImage()
        {
            m_scrollPosition.x += m_movementStep;
            if (m_scrollPosition.x > 1)
            {
                m_scrollPosition.x -= 1;
            }
            if (m_scrollPosition.x < -1)
            {
                m_scrollPosition.x += 1;
            }
            m_material.mainTextureOffset = m_scrollPosition;
        }

        /// <summary>
        /// 执行Logo垂直切换
        /// </summary>
        private void SwapImage()
        {
            m_scrollPosition.y += m_swapStep / 4f; // 在4个Logo图像间切换
            if (m_scrollPosition.y > 1)
            {
                m_scrollPosition.y -= 1;
            }
            if (m_scrollPosition.y < -1)
            {
                m_scrollPosition.y += 1;
            }
            m_material.mainTextureOffset = m_scrollPosition;
        }
    }
}