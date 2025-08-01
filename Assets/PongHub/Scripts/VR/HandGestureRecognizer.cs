using UnityEngine;
using System.Collections.Generic;
using Meta.Utilities.Input;

namespace PongHub.VR
{
    /// <summary>
    /// 手势识别器
    /// 基于OVRHand和OVRSkeleton数据识别各种手势
    /// 针对VR乒乓球游戏优化的手势识别算法
    /// </summary>
    public class HandGestureRecognizer
    {
        private float m_confidenceThreshold = 0.8f;
        private Dictionary<EnhancedXRInputManager.HandGesture, float> m_gestureConfidences = new Dictionary<EnhancedXRInputManager.HandGesture, float>();

        // 手势识别的关键骨骼索引（基于OVRSkeleton.BoneId）
        private struct HandBoneIndices
        {
            public const int ThumbTip = (int)OVRSkeleton.BoneId.Hand_ThumbTip;
            public const int IndexTip = (int)OVRSkeleton.BoneId.Hand_Index3;
            public const int MiddleTip = (int)OVRSkeleton.BoneId.Hand_Middle3;
            public const int RingTip = (int)OVRSkeleton.BoneId.Hand_Ring3;
            public const int PinkyTip = (int)OVRSkeleton.BoneId.Hand_Pinky3;
            
            public const int IndexMcp = (int)OVRSkeleton.BoneId.Hand_Index1;
            public const int MiddleMcp = (int)OVRSkeleton.BoneId.Hand_Middle1;
            public const int RingMcp = (int)OVRSkeleton.BoneId.Hand_Ring1;
            public const int PinkyMcp = (int)OVRSkeleton.BoneId.Hand_Pinky1;
            
            public const int WristRoot = (int)OVRSkeleton.BoneId.Hand_WristRoot;
            public const int Palm = (int)OVRSkeleton.BoneId.Hand_WristRoot; // 使用腕关节作为手掌参考
        }

        /// <summary>
        /// 设置置信度阈值
        /// </summary>
        public void SetConfidenceThreshold(float threshold)
        {
            m_confidenceThreshold = Mathf.Clamp01(threshold);
        }

        /// <summary>
        /// 识别手势
        /// </summary>
        public EnhancedXRInputManager.HandGesture RecognizeGesture(OVRHand hand, OVRSkeleton skeleton)
        {
            if (hand == null || !hand.IsDataValid)
                return EnhancedXRInputManager.HandGesture.None;

            // 清除之前的置信度数据
            m_gestureConfidences.Clear();

            // 计算各种手势的置信度
            CalculatePinchConfidence(hand, skeleton);
            CalculatePointConfidence(hand, skeleton);
            CalculateFistConfidence(hand, skeleton);
            CalculateOpenHandConfidence(hand, skeleton);
            CalculateThumbsUpConfidence(hand, skeleton);
            CalculatePaddleGripConfidence(hand, skeleton);
            CalculateMenuGestureConfidence(hand, skeleton);

            // 找出置信度最高的手势
            var bestGesture = EnhancedXRInputManager.HandGesture.None;
            float bestConfidence = m_confidenceThreshold;

            foreach (var kvp in m_gestureConfidences)
            {
                if (kvp.Value > bestConfidence)
                {
                    bestGesture = kvp.Key;
                    bestConfidence = kvp.Value;
                }
            }

            return bestGesture;
        }

        /// <summary>
        /// 获取指定手势的置信度
        /// </summary>
        public float GetGestureConfidence(EnhancedXRInputManager.HandGesture gesture)
        {
            return m_gestureConfidences.TryGetValue(gesture, out float confidence) ? confidence : 0f;
        }

        /// <summary>
        /// 计算捏取手势置信度
        /// </summary>
        private void CalculatePinchConfidence(OVRHand hand, OVRSkeleton skeleton)
        {
            float confidence = 0f;

            // 使用OVRHand内置的捏取检测
            if (hand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            {
                confidence += 0.6f;
            }

            // 检查食指和拇指的距离
            if (skeleton != null && skeleton.IsDataValid)
            {
                var bones = skeleton.Bones;
                if (bones != null && bones.Count > HandBoneIndices.ThumbTip && bones.Count > HandBoneIndices.IndexTip)
                {
                    var thumbTip = bones[HandBoneIndices.ThumbTip].Transform.position;
                    var indexTip = bones[HandBoneIndices.IndexTip].Transform.position;
                    float distance = Vector3.Distance(thumbTip, indexTip);

                    // 捏取时拇指和食指应该很近（通常<0.03m）
                    if (distance < 0.03f)
                    {
                        confidence += 0.4f * (1f - distance / 0.03f);
                    }
                }
            }

            m_gestureConfidences[EnhancedXRInputManager.HandGesture.Pinch] = Mathf.Clamp01(confidence);
        }

        /// <summary>
        /// 计算指向手势置信度
        /// </summary>
        private void CalculatePointConfidence(OVRHand hand, OVRSkeleton skeleton)
        {
            float confidence = 0f;

            if (skeleton != null && skeleton.IsDataValid)
            {
                var bones = skeleton.Bones;
                if (bones != null && bones.Count > HandBoneIndices.PinkyMcp)
                {
                    // 食指伸直，其他手指弯曲
                    bool indexExtended = IsFingerExtended(bones, HandBoneIndices.IndexMcp, HandBoneIndices.IndexTip);
                    bool middleCurled = !IsFingerExtended(bones, HandBoneIndices.MiddleMcp, HandBoneIndices.MiddleTip);
                    bool ringCurled = !IsFingerExtended(bones, HandBoneIndices.RingMcp, HandBoneIndices.RingTip);
                    bool pinkyCurled = !IsFingerExtended(bones, HandBoneIndices.PinkyMcp, HandBoneIndices.PinkyTip);

                    if (indexExtended) confidence += 0.4f;
                    if (middleCurled) confidence += 0.2f;
                    if (ringCurled) confidence += 0.2f;
                    if (pinkyCurled) confidence += 0.2f;
                }
            }

            m_gestureConfidences[EnhancedXRInputManager.HandGesture.Point] = Mathf.Clamp01(confidence);
        }

        /// <summary>
        /// 计算握拳手势置信度
        /// </summary>
        private void CalculateFistConfidence(OVRHand hand, OVRSkeleton skeleton)
        {
            float confidence = 0f;

            if (skeleton != null && skeleton.IsDataValid)
            {
                var bones = skeleton.Bones;
                if (bones != null && bones.Count > HandBoneIndices.PinkyMcp)
                {
                    // 所有手指都弯曲
                    bool indexCurled = !IsFingerExtended(bones, HandBoneIndices.IndexMcp, HandBoneIndices.IndexTip);
                    bool middleCurled = !IsFingerExtended(bones, HandBoneIndices.MiddleMcp, HandBoneIndices.MiddleTip);
                    bool ringCurled = !IsFingerExtended(bones, HandBoneIndices.RingMcp, HandBoneIndices.RingTip);
                    bool pinkyCurled = !IsFingerExtended(bones, HandBoneIndices.PinkyMcp, HandBoneIndices.PinkyTip);

                    if (indexCurled) confidence += 0.25f;
                    if (middleCurled) confidence += 0.25f;
                    if (ringCurled) confidence += 0.25f;
                    if (pinkyCurled) confidence += 0.25f;
                }
            }

            m_gestureConfidences[EnhancedXRInputManager.HandGesture.Fist] = Mathf.Clamp01(confidence);
        }

        /// <summary>
        /// 计算张开手势置信度
        /// </summary>
        private void CalculateOpenHandConfidence(OVRHand hand, OVRSkeleton skeleton)
        {
            float confidence = 0f;

            if (skeleton != null && skeleton.IsDataValid)
            {
                var bones = skeleton.Bones;
                if (bones != null && bones.Count > HandBoneIndices.PinkyMcp)
                {
                    // 所有手指都伸直
                    bool indexExtended = IsFingerExtended(bones, HandBoneIndices.IndexMcp, HandBoneIndices.IndexTip);
                    bool middleExtended = IsFingerExtended(bones, HandBoneIndices.MiddleMcp, HandBoneIndices.MiddleTip);
                    bool ringExtended = IsFingerExtended(bones, HandBoneIndices.RingMcp, HandBoneIndices.RingTip);
                    bool pinkyExtended = IsFingerExtended(bones, HandBoneIndices.PinkyMcp, HandBoneIndices.PinkyTip);

                    if (indexExtended) confidence += 0.25f;
                    if (middleExtended) confidence += 0.25f;
                    if (ringExtended) confidence += 0.25f;
                    if (pinkyExtended) confidence += 0.25f;
                }
            }

            m_gestureConfidences[EnhancedXRInputManager.HandGesture.OpenHand] = Mathf.Clamp01(confidence);
        }

        /// <summary>
        /// 计算点赞手势置信度
        /// </summary>
        private void CalculateThumbsUpConfidence(OVRHand hand, OVRSkeleton skeleton)
        {
            float confidence = 0f;

            if (skeleton != null && skeleton.IsDataValid)
            {
                var bones = skeleton.Bones;
                if (bones != null && bones.Count > HandBoneIndices.ThumbTip)
                {
                    // 拇指伸直向上，其他手指弯曲
                    var thumbTip = bones[HandBoneIndices.ThumbTip].Transform.position;
                    var wrist = bones[HandBoneIndices.WristRoot].Transform.position;
                    
                    // 检查拇指是否向上（Y轴正方向）
                    Vector3 thumbDirection = (thumbTip - wrist).normalized;
                    if (thumbDirection.y > 0.7f) // 拇指大致向上
                    {
                        confidence += 0.5f;
                    }

                    // 其他手指弯曲
                    bool indexCurled = !IsFingerExtended(bones, HandBoneIndices.IndexMcp, HandBoneIndices.IndexTip);
                    bool middleCurled = !IsFingerExtended(bones, HandBoneIndices.MiddleMcp, HandBoneIndices.MiddleTip);
                    bool ringCurled = !IsFingerExtended(bones, HandBoneIndices.RingMcp, HandBoneIndices.RingTip);

                    if (indexCurled) confidence += 0.17f;
                    if (middleCurled) confidence += 0.17f;
                    if (ringCurled) confidence += 0.16f;
                }
            }

            m_gestureConfidences[EnhancedXRInputManager.HandGesture.ThumbsUp] = Mathf.Clamp01(confidence);
        }

        /// <summary>
        /// 计算球拍握持手势置信度（乒乓球专用）
        /// </summary>
        private void CalculatePaddleGripConfidence(OVRHand hand, OVRSkeleton skeleton)
        {
            float confidence = 0f;

            if (skeleton != null && skeleton.IsDataValid)
            {
                var bones = skeleton.Bones;
                if (bones != null && bones.Count > HandBoneIndices.ThumbTip)
                {
                    // 球拍握持：拇指和食指轻微分开，其他手指弯曲但不完全握拳
                    var thumbTip = bones[HandBoneIndices.ThumbTip].Transform.position;
                    var indexTip = bones[HandBoneIndices.IndexTip].Transform.position;
                    float thumbIndexDistance = Vector3.Distance(thumbTip, indexTip);

                    // 拇指和食指应该有适中的距离（0.04-0.08m）
                    if (thumbIndexDistance > 0.04f && thumbIndexDistance < 0.08f)
                    {
                        confidence += 0.4f;
                    }

                    // 中指、无名指、小指应该弯曲但不完全握拳
                    bool middlePartialCurled = IsFingerPartiallyCurled(bones, HandBoneIndices.MiddleMcp, HandBoneIndices.MiddleTip);
                    bool ringPartialCurled = IsFingerPartiallyCurled(bones, HandBoneIndices.RingMcp, HandBoneIndices.RingTip);
                    bool pinkyPartialCurled = IsFingerPartiallyCurled(bones, HandBoneIndices.PinkyMcp, HandBoneIndices.PinkyTip);

                    if (middlePartialCurled) confidence += 0.2f;
                    if (ringPartialCurled) confidence += 0.2f;
                    if (pinkyPartialCurled) confidence += 0.2f;
                }
            }

            m_gestureConfidences[EnhancedXRInputManager.HandGesture.PaddleGrip] = Mathf.Clamp01(confidence);
        }

        /// <summary>
        /// 计算菜单手势置信度
        /// </summary>
        private void CalculateMenuGestureConfidence(OVRHand hand, OVRSkeleton skeleton)
        {
            float confidence = 0f;

            if (skeleton != null && skeleton.IsDataValid)
            {
                var bones = skeleton.Bones;
                if (bones != null && bones.Count > HandBoneIndices.RingTip)
                {
                    // 菜单手势：食指和中指伸直，其他手指弯曲（类似"Peace"手势）
                    bool indexExtended = IsFingerExtended(bones, HandBoneIndices.IndexMcp, HandBoneIndices.IndexTip);
                    bool middleExtended = IsFingerExtended(bones, HandBoneIndices.MiddleMcp, HandBoneIndices.MiddleTip);
                    bool ringCurled = !IsFingerExtended(bones, HandBoneIndices.RingMcp, HandBoneIndices.RingTip);
                    bool pinkyCurled = !IsFingerExtended(bones, HandBoneIndices.PinkyMcp, HandBoneIndices.PinkyTip);

                    if (indexExtended && middleExtended) confidence += 0.5f;
                    if (ringCurled) confidence += 0.25f;
                    if (pinkyCurled) confidence += 0.25f;
                }
            }

            m_gestureConfidences[EnhancedXRInputManager.HandGesture.MenuGesture] = Mathf.Clamp01(confidence);
        }

        /// <summary>
        /// 检查手指是否伸直
        /// </summary>
        private bool IsFingerExtended(IList<OVRBone> bones, int mcpIndex, int tipIndex)
        {
            if (bones == null || bones.Count <= mcpIndex || bones.Count <= tipIndex)
                return false;

            var mcpPos = bones[mcpIndex].Transform.position;
            var tipPos = bones[tipIndex].Transform.position;
            var wristPos = bones[HandBoneIndices.WristRoot].Transform.position;

            // 计算手指长度和理论最大长度
            float fingerLength = Vector3.Distance(mcpPos, tipPos);
            float wristToMcp = Vector3.Distance(wristPos, mcpPos);
            
            // 简单的伸直检测：指尖到掌关节的距离应该大于某个阈值
            // 这个方法不是最精确的，但对于VR游戏已经足够
            float extensionRatio = fingerLength / (wristToMcp + 0.01f); // 避免除以零
            
            return extensionRatio > 0.8f; // 可调整的阈值
        }

        /// <summary>
        /// 检查手指是否部分弯曲（不完全伸直，也不完全握拳）
        /// </summary>
        private bool IsFingerPartiallyCurled(IList<OVRBone> bones, int mcpIndex, int tipIndex)
        {
            if (bones == null || bones.Count <= mcpIndex || bones.Count <= tipIndex)
                return false;

            var mcpPos = bones[mcpIndex].Transform.position;
            var tipPos = bones[tipIndex].Transform.position;
            var wristPos = bones[HandBoneIndices.WristRoot].Transform.position;

            float fingerLength = Vector3.Distance(mcpPos, tipPos);
            float wristToMcp = Vector3.Distance(wristPos, mcpPos);
            float extensionRatio = fingerLength / (wristToMcp + 0.01f);
            
            // 部分弯曲：不是完全伸直（>0.8）也不是完全握拳（<0.3）
            return extensionRatio > 0.3f && extensionRatio < 0.8f;
        }
    }
}