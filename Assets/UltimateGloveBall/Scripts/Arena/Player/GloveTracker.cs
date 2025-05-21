// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using UnityEngine;

namespace PongHub.Arena.Player
{
    /// <summary>
    /// 手套追踪器组件,用于将手套锚定到此组件的Transform上。
    /// 在AvatarNetworking之后执行,以确保正确跟随手腕位置。
    /// 主要功能是同步更新手套和手臂骨骼的位置和旋转。
    /// </summary>
    [DefaultExecutionOrder(10100)] // 设置较高的执行顺序,确保在其他组件之后更新
    public class GloveTracker : MonoBehaviour
    {
        /// <summary>
        /// 要追踪的手套对象引用
        /// </summary>
        public Glove Glove;

        /// <summary>
        /// 手套骨骼网络同步组件引用
        /// </summary> 
        public GloveArmatureNetworking Armature;

        /// <summary>
        /// 每帧更新手套位置
        /// </summary>
        private void Update()
        {
            UpdateTracking();
        }

        /// <summary>
        /// 更新手套和骨骼的位置追踪
        /// 将手套和骨骼的位置、旋转同步到当前Transform
        /// </summary>
        public void UpdateTracking()
        {
            // 确保手套和骨骼组件都存在
            if (Glove && Armature)
            {
                // 同步移动手套和骨骼
                {
                    // 获取当前Transform信息
                    var trans = transform;
                    var wristPosition = trans.position;  // 手腕位置
                    var wristRotation = trans.rotation;  // 手腕旋转

                    // 更新手套位置和旋转
                    Glove.Move(wristPosition, wristRotation);

                    // 更新骨骼位置和旋转
                    var armTrans = Armature.transform;
                    armTrans.position = wristPosition;
                    armTrans.rotation = wristRotation;
                }
            }
        }
    }
}