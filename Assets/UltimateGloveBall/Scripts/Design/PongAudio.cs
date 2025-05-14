using UnityEngine;

namespace PongHub.Design
{
    [CreateAssetMenu(fileName = "PongAudio", menuName = "PongHub/Audio")]
    public class PongAudio : ScriptableObject
    {
        [Header("球拍音效")]
        public AudioClip PaddleHitClip;        // 击球音效
        public AudioClip PaddleMissClip;       // 挥空音效
        public AudioClip PaddleGrabClip;       // 抓取音效

        [Header("球音效")]
        public AudioClip BallBounceClip;       // 弹跳音效
        public AudioClip BallHitTableClip;     // 击桌音效
        public AudioClip BallHitNetClip;       // 击网音效

        [Header("环境音效")]
        public AudioClip CrowdCheerClip;       // 观众欢呼
        public AudioClip ScorePointClip;       // 得分音效
        public AudioClip GameStartClip;        // 开始音效
        public AudioClip GameEndClip;          // 结束音效
    }
}
