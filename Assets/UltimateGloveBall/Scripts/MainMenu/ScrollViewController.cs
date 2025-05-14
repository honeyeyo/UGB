// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateGloveBall.MainMenu
{
    /// <summary>
    /// 滚动视图控制器
    /// 用于处理VR控制器对UI滚动视图的交互
    /// 支持左右控制器通过摇杆控制滚动
    /// </summary>
    public class ScrollViewController : MonoBehaviour
    {
        /// <summary>
        /// 控制器指针枚举
        /// 用于标识当前激活的控制器
        /// </summary>
        private enum Pointers
        {
            None,           // 无激活控制器
            LeftPointer,    // 左控制器
            RightPointer    // 右控制器
        }

        [Header("UI组件引用")]
        [SerializeField] private GraphicRaycaster m_graphicRaycaster;    // UI射线检测器
        [SerializeField] private ScrollRect m_scrollRect;                // 滚动视图组件
        [SerializeField] private float m_scrollSpeed = 0.05f;            // 滚动速度
        [SerializeField] private GameObject m_targetPointer;             // 目标指针对象

        // Oculus控制器输入相关
        private OVRInput.Axis2D m_thumbstickL, m_thumbstickR;           // 左右摇杆输入
        private OVRInput.Controller m_controllerL, m_controllerR;       // 左右控制器引用

        private Pointers m_activeController = Pointers.None;            // 当前激活的控制器

        /// <summary>
        /// 初始化控制器输入设置
        /// </summary>
        private void Start()
        {
            m_thumbstickL = OVRInput.Axis2D.PrimaryThumbstick;         // 设置左摇杆输入
            m_thumbstickR = OVRInput.Axis2D.SecondaryThumbstick;       // 设置右摇杆输入
            m_controllerL = OVRInput.Controller.LTouch;                // 设置左控制器引用
            m_controllerR = OVRInput.Controller.RTouch;                // 设置右控制器引用
        }

        /// <summary>
        /// 每帧更新滚动视图状态
        /// 处理控制器输入并更新滚动位置
        /// </summary>
        private void Update()
        {
            // 获取左右摇杆的Y轴输入
            var leftInputY = OVRInput.Get(m_thumbstickL).y;
            var rightInputY = OVRInput.Get(m_thumbstickR).y;
            var newScrollPos = m_scrollRect.verticalNormalizedPosition;
            var isScrollingThisFrame = false;

            // 处理右控制器输入
            if (rightInputY != 0 && m_activeController != Pointers.LeftPointer &&
                IsPointerOverGUI(Pointers.RightPointer))
            {
                m_activeController = Pointers.RightPointer;
                newScrollPos += rightInputY * m_scrollSpeed * Time.deltaTime;
                isScrollingThisFrame = true;
            }
            // 处理左控制器输入
            else if (leftInputY != 0 && m_activeController != Pointers.RightPointer &&
                     IsPointerOverGUI(Pointers.LeftPointer))
            {
                m_activeController = Pointers.LeftPointer;
                newScrollPos += leftInputY * m_scrollSpeed * Time.deltaTime;
                isScrollingThisFrame = true;
            }

            // 限制滚动位置在有效范围内
            if (newScrollPos > 1) newScrollPos = 1;
            if (newScrollPos < -1) newScrollPos = -1;
            m_scrollRect.verticalNormalizedPosition = newScrollPos;

            // 如果没有滚动输入，重置激活控制器状态
            if (!isScrollingThisFrame)
            {
                m_activeController = Pointers.None;
            }
        }

        /// <summary>
        /// 检测指定控制器的射线是否指向目标UI
        /// </summary>
        /// <param name="pointer">要检测的控制器</param>
        /// <returns>如果射线指向目标UI则返回true，否则返回false</returns>
        private bool IsPointerOverGUI(Pointers pointer)
        {
            var controllerPosition = Vector3.zero;
            var controllerRotation = Quaternion.identity;

            // 获取指定控制器的位置和旋转
            if (pointer == Pointers.LeftPointer)
            {
                controllerPosition = OVRInput.GetLocalControllerPosition(m_controllerL);
                controllerRotation = OVRInput.GetLocalControllerRotation(m_controllerL);
            }

            if (pointer == Pointers.RightPointer)
            {
                controllerPosition = OVRInput.GetLocalControllerPosition(m_controllerR);
                controllerRotation = OVRInput.GetLocalControllerRotation(m_controllerR);
            }

            // 创建控制器射线
            var controllerRay = new Ray(controllerPosition, controllerRotation * Vector3.forward);

            // 检测射线是否击中物体
            if (Physics.Raycast(controllerRay, out var hit))
            {
                // 将击中点转换为屏幕坐标
                var screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, hit.point);
                var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };

                // 使用GraphicRaycaster检测UI元素
                var results = new List<RaycastResult>();
                m_graphicRaycaster.Raycast(eventData, results);

                // 检查是否击中目标指针
                foreach (var result in results)
                {
                    if (result.gameObject == m_targetPointer)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
