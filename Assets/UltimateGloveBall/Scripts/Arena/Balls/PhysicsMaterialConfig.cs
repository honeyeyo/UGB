// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License

using UnityEngine;
using System.Collections.Generic;

namespace PongHub.Arena.Balls
{
    /// <summary>
    /// 物理材质配置管理器
    /// 统一管理游戏中所有物理材质的参数和行为
    /// 基于ITTF国际乒联标准设计
    /// </summary>
    [CreateAssetMenu(fileName = "PhysicsMaterialConfig", menuName = "PongHub/Physics Material Config")]
    public class PhysicsMaterialConfig : ScriptableObject
    {
        [System.Serializable]
        public class MaterialProperties
        {
            [Header("基础物理属性")]
            public string materialName;
            public float dynamicFriction = 0.3f;
            public float staticFriction = 0.35f;
            public float bounciness = 0.7f;

            [Header("旋转效果")]
            public float frictionMultiplier = 1.0f;    // 摩擦倍数（影响旋转生成）
            public float spinEfficiency = 0.8f;       // 旋转转换效率
            public float spinDecayRate = 1.0f;        // 旋转衰减倍数

            [Header("声音效果")]
            public AudioClip impactSound;
            public float soundVolume = 1.0f;
            public float soundPitch = 1.0f;

            [Header("视觉效果")]
            public ParticleSystem impactEffect;
            public Color trailColor = Color.white;

            [Header("游戏行为")]
            public bool canGenerateSpin = true;       // 是否能产生旋转
            public bool absorbsEnergy = false;        // 是否吸收动能
            public float energyAbsorption = 0f;       // 能量吸收率(0-1)
        }

        [Header("材质配置列表")]
        [SerializeField] private List<MaterialProperties> materials = new List<MaterialProperties>();

        // 材质名称到配置的映射
        private Dictionary<string, MaterialProperties> materialLookup;

        #region Unity Lifecycle
        private void OnEnable()
        {
            InitializeLookup();
        }

        private void OnValidate()
        {
            InitializeLookup();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化材质查找字典
        /// </summary>
        private void InitializeLookup()
        {
            materialLookup = new Dictionary<string, MaterialProperties>();

            foreach (var material in materials)
            {
                if (!string.IsNullOrEmpty(material.materialName))
                {
                    materialLookup[material.materialName] = material;
                }
            }

            // 确保所有默认材质存在
            EnsureDefaultMaterials();
        }

        /// <summary>
        /// 确保默认材质配置存在
        /// </summary>
        private void EnsureDefaultMaterials()
        {
            var defaultMaterials = new Dictionary<string, System.Action<MaterialProperties>>
            {
                ["PhyCAP"] = (props) => {
                    props.materialName = "PhyCAP";
                    props.dynamicFriction = 0.05f;
                    props.staticFriction = 0.05f;
                    props.bounciness = 0.91f;
                    props.frictionMultiplier = 0.1f;
                    props.spinEfficiency = 0.9f;
                    props.canGenerateSpin = true;
                },
                ["PhyRubber"] = (props) => {
                    props.materialName = "PhyRubber";
                    props.dynamicFriction = 0.7f;
                    props.staticFriction = 0.75f;
                    props.bounciness = 0.85f;
                    props.frictionMultiplier = 2.0f;
                    props.spinEfficiency = 1.0f;
                    props.canGenerateSpin = true;
                },
                ["PhyBlade"] = (props) => {
                    props.materialName = "PhyBlade";
                    props.dynamicFriction = 0.4f;
                    props.staticFriction = 0.45f;
                    props.bounciness = 0.6f;
                    props.frictionMultiplier = 1.0f;
                    props.spinEfficiency = 0.7f;
                    props.canGenerateSpin = true;
                },
                ["PhyWood"] = (props) => {
                    props.materialName = "PhyWood";
                    props.dynamicFriction = 0.3f;
                    props.staticFriction = 0.35f;
                    props.bounciness = 0.85f;
                    props.frictionMultiplier = 0.6f;
                    props.spinEfficiency = 0.8f;
                    props.canGenerateSpin = false;
                },
                ["PhyNet"] = (props) => {
                    props.materialName = "PhyNet";
                    props.dynamicFriction = 0.8f;
                    props.staticFriction = 0.85f;
                    props.bounciness = 0.4f;
                    props.frictionMultiplier = 1.5f;
                    props.spinEfficiency = 0.3f;
                    props.absorbsEnergy = true;
                    props.energyAbsorption = 0.6f;
                    props.canGenerateSpin = false;
                },
                ["PhyMetalSolid"] = (props) => {
                    props.materialName = "PhyMetalSolid";
                    props.dynamicFriction = 0.2f;
                    props.staticFriction = 0.25f;
                    props.bounciness = 0.2f;
                    props.frictionMultiplier = 0.3f;
                    props.spinEfficiency = 0.1f;
                    props.absorbsEnergy = true;
                    props.energyAbsorption = 0.8f;
                    props.canGenerateSpin = false;
                }
            };

            bool needsUpdate = false;
            foreach (var kvp in defaultMaterials)
            {
                if (!materialLookup.ContainsKey(kvp.Key))
                {
                    var newMaterial = new MaterialProperties();
                    kvp.Value(newMaterial);
                    materials.Add(newMaterial);
                    materialLookup[kvp.Key] = newMaterial;
                    needsUpdate = true;
                }
            }

            #if UNITY_EDITOR
            if (needsUpdate)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
            #endif
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 获取材质属性
        /// </summary>
        /// <param name="materialName">材质名称</param>
        /// <returns>材质属性，如果不存在则返回默认属性</returns>
        public MaterialProperties GetMaterialProperties(string materialName)
        {
            if (materialLookup == null) InitializeLookup();

            if (materialLookup.TryGetValue(materialName, out MaterialProperties properties))
            {
                return properties;
            }

            // 返回默认属性
            Debug.LogWarning($"未找到材质 '{materialName}'，使用默认属性");
            return GetDefaultProperties();
        }

        /// <summary>
        /// 获取摩擦倍数
        /// </summary>
        public float GetFrictionMultiplier(string materialName)
        {
            return GetMaterialProperties(materialName).frictionMultiplier;
        }

        /// <summary>
        /// 获取旋转效率
        /// </summary>
        public float GetSpinEfficiency(string materialName)
        {
            return GetMaterialProperties(materialName).spinEfficiency;
        }

        /// <summary>
        /// 获取能量吸收率
        /// </summary>
        public float GetEnergyAbsorption(string materialName)
        {
            return GetMaterialProperties(materialName).energyAbsorption;
        }

        /// <summary>
        /// 检查材质是否能产生旋转
        /// </summary>
        public bool CanGenerateSpin(string materialName)
        {
            return GetMaterialProperties(materialName).canGenerateSpin;
        }

        /// <summary>
        /// 检查材质是否吸收能量
        /// </summary>
        public bool AbsorbsEnergy(string materialName)
        {
            return GetMaterialProperties(materialName).absorbsEnergy;
        }

        /// <summary>
        /// 获取默认属性
        /// </summary>
        private MaterialProperties GetDefaultProperties()
        {
            return new MaterialProperties
            {
                materialName = "Default",
                dynamicFriction = 0.3f,
                staticFriction = 0.35f,
                bounciness = 0.7f,
                frictionMultiplier = 1.0f,
                spinEfficiency = 0.8f,
                canGenerateSpin = true
            };
        }

        /// <summary>
        /// 获取所有材质名称
        /// </summary>
        public string[] GetAllMaterialNames()
        {
            if (materialLookup == null) InitializeLookup();

            string[] names = new string[materialLookup.Count];
            materialLookup.Keys.CopyTo(names, 0);
            return names;
        }

        /// <summary>
        /// 更新材质属性
        /// </summary>
        public void UpdateMaterialProperties(string materialName, MaterialProperties newProperties)
        {
            if (materialLookup == null) InitializeLookup();

            if (materialLookup.ContainsKey(materialName))
            {
                materialLookup[materialName] = newProperties;

                // 更新列表中的对应项
                for (int i = 0; i < materials.Count; i++)
                {
                    if (materials[i].materialName == materialName)
                    {
                        materials[i] = newProperties;
                        break;
                    }
                }

                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }
        #endregion

        #region Editor Support
        #if UNITY_EDITOR
        [ContextMenu("生成默认材质配置")]
        private void GenerateDefaultMaterials()
        {
            materials.Clear();
            materialLookup = new Dictionary<string, MaterialProperties>();
            EnsureDefaultMaterials();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("验证材质配置")]
        private void ValidateMaterials()
        {
            InitializeLookup();
            Debug.Log($"已验证 {materials.Count} 个材质配置");

            foreach (var material in materials)
            {
                Debug.Log($"材质: {material.materialName}, 摩擦: {material.dynamicFriction}, 弹性: {material.bounciness}");
            }
        }
        #endif
        #endregion
    }
}
