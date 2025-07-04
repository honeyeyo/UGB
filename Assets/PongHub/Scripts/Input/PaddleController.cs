using UnityEngine;

namespace PongHub.Input
{
    /// <summary>
    /// 球拍控制器
    /// 处理球拍的抓取、释放和跟随逻辑
    /// </summary>
    public class PaddleController : MonoBehaviour
    {
        [Header("球拍设置")]
        [SerializeField]
        [Tooltip("Left Paddle Prefab / 左手球拍预制件 - Prefab for left hand paddle")]
        private GameObject m_leftPaddlePrefab;   // 左手球拍预制件

        [SerializeField]
        [Tooltip("Right Paddle Prefab / 右手球拍预制件 - Prefab for right hand paddle")]
        private GameObject m_rightPaddlePrefab;  // 右手球拍预制件

        [SerializeField]
        [Tooltip("Paddle Grip Distance / 球拍抓取距离 - Maximum distance to grip paddle")]
        private float m_paddleGripDistance = 0.1f; // 球拍抓取距离

        [Header("跟随设置")]
        [SerializeField]
        [Tooltip("Follow Speed / 跟随速度 - Speed at which paddle follows hand")]
        private float m_followSpeed = 15f;       // 跟随速度

        [SerializeField]
        [Tooltip("Rotation Speed / 旋转速度 - Speed of paddle rotation")]
        private float m_rotationSpeed = 20f;     // 旋转速度

        [SerializeField]
        [Tooltip("Use Physics Joint / 使用物理关节 - Whether to use physics joint for paddle")]
        private bool m_usePhysicsJoint = true;   // 是否使用物理关节

        [Header("物理设置")]
        // [SerializeField] private float m_jointSpring = 3000f;     // 关节弹性
        // [SerializeField] private float m_jointDamper = 50f;       // 关节阻尼
        [SerializeField]
        [Tooltip("Max Joint Force / 最大关节力 - Maximum force for physics joint")]
        private float m_maxJointForce = 10000f;  // 最大关节力

        [Header("音效")]
        [SerializeField]
        [Tooltip("Paddle Grip Sound / 抓取音效 - Audio clip for paddle grip")]
        private AudioClip m_paddleGripSound;     // 抓取音效

        [SerializeField]
        [Tooltip("Paddle Release Sound / 释放音效 - Audio clip for paddle release")]
        private AudioClip m_paddleReleaseSound;  // 释放音效

        [Header("组件引用")]
        [SerializeField]
        [Tooltip("Left Hand Anchor / 左手锚点 - Transform anchor for left hand")]
        private Transform m_leftHandAnchor;      // 左手锚点

        [SerializeField]
        [Tooltip("Right Hand Anchor / 右手锚点 - Transform anchor for right hand")]
        private Transform m_rightHandAnchor;     // 右手锚点

        // 球拍实例
        private GameObject m_leftPaddleInstance;
        private GameObject m_rightPaddleInstance;

        // 物理关节
        private FixedJoint m_leftPaddleJoint;
        private FixedJoint m_rightPaddleJoint;

        // 状态管理
        private bool m_isLeftPaddleGripped = false;
        private bool m_isRightPaddleGripped = false;

        // 非物理跟随
        private Vector3 m_leftPaddleTargetPosition;
        private Quaternion m_leftPaddleTargetRotation;
        private Vector3 m_rightPaddleTargetPosition;
        private Quaternion m_rightPaddleTargetRotation;

        private AudioSource m_audioSource;

        private void Awake()
        {
            InitializePaddleController();
        }

        private void Start()
        {
            CreatePaddleInstances();
        }

        private void Update()
        {
            UpdatePaddlePositions();
        }

        /// <summary>
        /// 初始化球拍控制器
        /// </summary>
        private void InitializePaddleController()
        {
            // 添加音频源
            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
            m_audioSource.playOnAwake = false;
            m_audioSource.spatialBlend = 1f; // 3D音效

            Debug.Log("PaddleController 初始化完成");
        }

        /// <summary>
        /// 创建球拍实例
        /// </summary>
        private void CreatePaddleInstances()
        {
            // 创建左手球拍
            if (m_leftPaddlePrefab != null && m_leftHandAnchor != null)
            {
                m_leftPaddleInstance = Instantiate(m_leftPaddlePrefab);
                m_leftPaddleInstance.name = "LeftPaddle";
                SetupPaddle(m_leftPaddleInstance, true);
            }

            // 创建右手球拍
            if (m_rightPaddlePrefab != null && m_rightHandAnchor != null)
            {
                m_rightPaddleInstance = Instantiate(m_rightPaddlePrefab);
                m_rightPaddleInstance.name = "RightPaddle";
                SetupPaddle(m_rightPaddleInstance, false);
            }
        }

        /// <summary>
        /// 设置球拍
        /// </summary>
        private void SetupPaddle(GameObject paddle, bool isLeftHand)
        {
            if (paddle == null) return;

            // 确保球拍有Rigidbody
            Rigidbody paddleRb = paddle.GetComponent<Rigidbody>();
            if (paddleRb == null)
            {
                paddleRb = paddle.AddComponent<Rigidbody>();
            }

            // 配置物理属性
            paddleRb.mass = 0.2f; // 200g
            paddleRb.drag = 1f;
            paddleRb.angularDrag = 5f;
            paddleRb.useGravity = false;

            // 初始位置（放在手部附近但未抓取）
            Transform handAnchor = isLeftHand ? m_leftHandAnchor : m_rightHandAnchor;
            if (handAnchor != null)
            {
                Vector3 initialPosition = handAnchor.position + handAnchor.forward * 0.3f;
                paddle.transform.position = initialPosition;
                paddle.transform.rotation = handAnchor.rotation;
            }

            // 添加球拍组件标识
            PaddleIdentifier identifier = paddle.GetComponent<PaddleIdentifier>();
            if (identifier == null)
            {
                identifier = paddle.AddComponent<PaddleIdentifier>();
            }
            identifier.IsLeftHand = isLeftHand;

            Debug.Log($"{(isLeftHand ? "左" : "右")}手球拍已创建");
        }

        /// <summary>
        /// 抓取球拍
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        public void GripPaddle(bool isLeftHand)
        {
            GameObject paddle = isLeftHand ? m_leftPaddleInstance : m_rightPaddleInstance;
            Transform handAnchor = isLeftHand ? m_leftHandAnchor : m_rightHandAnchor;

            if (paddle == null || handAnchor == null) return;

            // 检查距离
            float distance = Vector3.Distance(paddle.transform.position, handAnchor.position);
            if (distance > m_paddleGripDistance)
            {
                Debug.Log($"{(isLeftHand ? "左" : "右")}手球拍距离过远，无法抓取");
                return;
            }

            // 设置状态
            if (isLeftHand)
            {
                m_isLeftPaddleGripped = true;
            }
            else
            {
                m_isRightPaddleGripped = true;
            }

            // 使用物理关节或直接跟随
            if (m_usePhysicsJoint)
            {
                CreatePhysicsJoint(paddle, handAnchor, isLeftHand);
            }
            else
            {
                SetupDirectFollow(paddle, handAnchor, isLeftHand);
            }

            // 播放音效
            PlayPaddleGripSound();

            Debug.Log($"{(isLeftHand ? "左" : "右")}手球拍已抓取");
        }

        /// <summary>
        /// 释放球拍
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        public void ReleasePaddle(bool isLeftHand)
        {
            GameObject paddle = isLeftHand ? m_leftPaddleInstance : m_rightPaddleInstance;

            if (paddle == null) return;

            // 设置状态
            if (isLeftHand)
            {
                m_isLeftPaddleGripped = false;
                DestroyPhysicsJoint(ref m_leftPaddleJoint);
            }
            else
            {
                m_isRightPaddleGripped = false;
                DestroyPhysicsJoint(ref m_rightPaddleJoint);
            }

            // 恢复重力
            Rigidbody paddleRb = paddle.GetComponent<Rigidbody>();
            if (paddleRb != null)
            {
                paddleRb.useGravity = true;
            }

            // 播放音效
            PlayPaddleReleaseSound();

            Debug.Log($"{(isLeftHand ? "左" : "右")}手球拍已释放");
        }

        /// <summary>
        /// 创建物理关节
        /// </summary>
        private void CreatePhysicsJoint(GameObject paddle, Transform handAnchor, bool isLeftHand)
        {
            // 创建一个隐形的刚体作为手部锚点
            GameObject handRigidBodyObj = new GameObject($"{(isLeftHand ? "Left" : "Right")}HandRigidBody");
            handRigidBodyObj.transform.SetParent(handAnchor);
            handRigidBodyObj.transform.localPosition = Vector3.zero;
            handRigidBodyObj.transform.localRotation = Quaternion.identity;

            Rigidbody handRb = handRigidBodyObj.AddComponent<Rigidbody>();
            handRb.isKinematic = true;

            // 创建关节
            FixedJoint joint = paddle.AddComponent<FixedJoint>();
            joint.connectedBody = handRb;
            joint.breakForce = m_maxJointForce;
            joint.breakTorque = m_maxJointForce;

            // 保存关节引用
            if (isLeftHand)
            {
                m_leftPaddleJoint = joint;
            }
            else
            {
                m_rightPaddleJoint = joint;
            }

            // 禁用重力
            Rigidbody paddleRb = paddle.GetComponent<Rigidbody>();
            if (paddleRb != null)
            {
                paddleRb.useGravity = false;
            }
        }

        /// <summary>
        /// 销毁物理关节
        /// </summary>
        private void DestroyPhysicsJoint(ref FixedJoint joint)
        {
            if (joint != null)
            {
                // 销毁手部刚体对象
                if (joint.connectedBody != null)
                {
                    Destroy(joint.connectedBody.gameObject);
                }

                Destroy(joint);
                joint = null;
            }
        }

        /// <summary>
        /// 设置直接跟随
        /// </summary>
        private void SetupDirectFollow(GameObject paddle, Transform handAnchor, bool isLeftHand)
        {
            // 禁用重力
            Rigidbody paddleRb = paddle.GetComponent<Rigidbody>();
            if (paddleRb != null)
            {
                paddleRb.useGravity = false;
                paddleRb.isKinematic = true;
            }

            // 设置目标位置和旋转
            if (isLeftHand)
            {
                m_leftPaddleTargetPosition = handAnchor.position;
                m_leftPaddleTargetRotation = handAnchor.rotation;
            }
            else
            {
                m_rightPaddleTargetPosition = handAnchor.position;
                m_rightPaddleTargetRotation = handAnchor.rotation;
            }
        }

        /// <summary>
        /// 更新球拍位置
        /// </summary>
        private void UpdatePaddlePositions()
        {
            // 更新左手球拍
            if (m_isLeftPaddleGripped && m_leftPaddleInstance != null && !m_usePhysicsJoint)
            {
                UpdatePaddleFollow(m_leftPaddleInstance, m_leftHandAnchor);
            }

            // 更新右手球拍
            if (m_isRightPaddleGripped && m_rightPaddleInstance != null && !m_usePhysicsJoint)
            {
                UpdatePaddleFollow(m_rightPaddleInstance, m_rightHandAnchor);
            }
        }

        /// <summary>
        /// 更新球拍跟随
        /// </summary>
        private void UpdatePaddleFollow(GameObject paddle, Transform handAnchor)
        {
            if (paddle == null || handAnchor == null) return;

            // 平滑移动到目标位置
            Vector3 targetPosition = handAnchor.position;
            Quaternion targetRotation = handAnchor.rotation;

            paddle.transform.position = Vector3.Lerp(
                paddle.transform.position,
                targetPosition,
                m_followSpeed * Time.deltaTime
            );

            paddle.transform.rotation = Quaternion.Lerp(
                paddle.transform.rotation,
                targetRotation,
                m_rotationSpeed * Time.deltaTime
            );
        }

        /// <summary>
        /// 播放球拍抓取音效
        /// </summary>
        private void PlayPaddleGripSound()
        {
            if (m_paddleGripSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_paddleGripSound);
            }
        }

        /// <summary>
        /// 播放球拍释放音效
        /// </summary>
        private void PlayPaddleReleaseSound()
        {
            if (m_paddleReleaseSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_paddleReleaseSound);
            }
        }

        #region 公共属性和方法

        /// <summary>
        /// 获取球拍实例
        /// </summary>
        public GameObject GetPaddleInstance(bool isLeftHand)
        {
            return isLeftHand ? m_leftPaddleInstance : m_rightPaddleInstance;
        }

        /// <summary>
        /// 检查球拍是否被抓取
        /// </summary>
        public bool IsPaddleGripped(bool isLeftHand)
        {
            return isLeftHand ? m_isLeftPaddleGripped : m_isRightPaddleGripped;
        }

        /// <summary>
        /// 强制释放所有球拍
        /// </summary>
        public void ReleaseAllPaddles()
        {
            if (m_isLeftPaddleGripped)
            {
                ReleasePaddle(true);
            }

            if (m_isRightPaddleGripped)
            {
                ReleasePaddle(false);
            }
        }

        /// <summary>
        /// 重置球拍位置
        /// </summary>
        public void ResetPaddlePositions()
        {
            // 重置左手球拍
            if (m_leftPaddleInstance != null && m_leftHandAnchor != null)
            {
                Vector3 resetPosition = m_leftHandAnchor.position + m_leftHandAnchor.forward * 0.3f;
                m_leftPaddleInstance.transform.position = resetPosition;
                m_leftPaddleInstance.transform.rotation = m_leftHandAnchor.rotation;
            }

            // 重置右手球拍
            if (m_rightPaddleInstance != null && m_rightHandAnchor != null)
            {
                Vector3 resetPosition = m_rightHandAnchor.position + m_rightHandAnchor.forward * 0.3f;
                m_rightPaddleInstance.transform.position = resetPosition;
                m_rightPaddleInstance.transform.rotation = m_rightHandAnchor.rotation;
            }

            Debug.Log("球拍位置已重置");
        }

        #endregion

        private void OnDestroy()
        {
            // 清理关节
            DestroyPhysicsJoint(ref m_leftPaddleJoint);
            DestroyPhysicsJoint(ref m_rightPaddleJoint);
        }
    }

    /// <summary>
    /// 球拍标识组件
    /// </summary>
    public class PaddleIdentifier : MonoBehaviour
    {
        public bool IsLeftHand { get; set; }

        /// <summary>
        /// 获取球拍类型描述
        /// </summary>
        public string GetPaddleDescription()
        {
            return IsLeftHand ? "左手球拍" : "右手球拍";
        }
    }
}