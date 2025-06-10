// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

namespace Meta.Utilities
{
    /// <summary>
    /// 基于枚举键的高性能字典
    /// 使用数组作为底层存储，以枚举值的整数表示作为数组索引
    /// 提供O(1)时间复杂度的访问性能，适用于枚举键的场景
    /// </summary>
    /// <typeparam name="TKey">枚举类型的键</typeparam>
    /// <typeparam name="TValue">值的类型</typeparam>
    [Serializable]
    public class EnumDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : struct, Enum
    {
        /// <summary>
        /// 静态构造函数
        /// 在类型首次使用时验证所有枚举键的边界
        /// </summary>
        static EnumDictionary()
        {
            foreach (var key in AllKeys)
            {
                CheckBounds(key);
            }
        }

        /// <summary>
        /// 检查枚举键是否在有效边界内
        /// 使用积极内联优化以提高性能
        /// </summary>
        /// <param name="key">要检查的枚举键</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void CheckBounds(TKey key)
        {
            var val = ToInt32(key);
            if (val < 0 || val >= Length)
            {
                Debug.LogAssertion($"EnumDictionary key {typeof(TKey).Name}.{key} is out of bounds.");
            }
        }

        /// <summary>
        /// 将枚举值转换为32位整数
        /// 使用不安全代码以获得最佳性能
        /// </summary>
        /// <param name="key">枚举键</param>
        /// <returns>对应的整数值</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ToInt32(TKey key) => UnsafeUtility.EnumToInt(key);

        /// <summary>
        /// 所有可能的枚举键数组
        /// 在静态构造时初始化，包含枚举类型的所有值
        /// </summary>
        protected static TKey[] AllKeys { get; } = (TKey[])typeof(TKey).GetEnumValues();

        /// <summary>
        /// 内部数组的长度
        /// 基于枚举值的最大整数值加1来确定
        /// </summary>
        protected static int Length { get; } = AllKeys.Max(e => ToInt32(e)) + 1;

        /// <summary>
        /// 内部存储数组
        /// 使用枚举的整数值作为数组索引直接访问
        /// </summary>
        [SerializeField]
        protected TValue[] m_values = new TValue[Length];

        /// <summary>
        /// 字典的索引器
        /// 提供基于枚举键的O(1)访问性能
        /// </summary>
        /// <param name="key">枚举键</param>
        /// <returns>对应的值</returns>
        public TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckBounds(key);
                return m_values[ToInt32(key)];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_values[ToInt32(key)] = value;
        }

        /// <summary>
        /// 所有键的只读数组
        /// 返回包含所有可能枚举值的数组
        /// </summary>
        public ReadOnlyArray<TKey> Keys
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AllKeys;
        }

        /// <summary>
        /// 所有值的数组
        /// 直接返回内部存储数组
        /// </summary>
        public TValue[] Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_values;
        }

        // IDictionary接口实现
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Array.AsReadOnly(AllKeys);
        ICollection<TValue> IDictionary<TKey, TValue>.Values => m_values;

        /// <summary>
        /// 字典中键值对的数量
        /// 等于枚举类型的所有可能值的数量
        /// </summary>
        public int Count => Length;

        /// <summary>
        /// 字典是否为只读
        /// 此实现允许修改，返回false
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// 添加键值对到字典
        /// 实际上是设置指定键的值
        /// </summary>
        /// <param name="key">枚举键</param>
        /// <param name="value">要设置的值</param>
        public void Add(TKey key, TValue value) => this[key] = value;

        /// <summary>
        /// 添加键值对到字典
        /// </summary>
        /// <param name="item">包含键和值的键值对</param>
        public void Add(KeyValuePair<TKey, TValue> item) => this[item.Key] = item.Value;

        /// <summary>
        /// 检查是否包含指定的键值对
        /// </summary>
        /// <param name="item">要检查的键值对</param>
        /// <returns>如果包含则返回true</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item) => EqualityComparer<TValue>.Default.Equals(this[item.Key], item.Value);

        /// <summary>
        /// 检查是否包含指定的键
        /// 对于枚举字典，所有有效的枚举值都被视为存在的键
        /// </summary>
        /// <param name="key">要检查的键</param>
        /// <returns>始终返回true</returns>
        public bool ContainsKey(TKey key) => true;

        /// <summary>
        /// 枚举字典的自定义枚举器
        /// 提供高效的键值对遍历
        /// </summary>
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            /// <summary>
            /// 字典实例的内部引用
            /// </summary>
            internal EnumDictionary<TKey, TValue> m_dictionary;

            /// <summary>
            /// 键数组的枚举器
            /// </summary>
            internal ArraySegment<TKey>.Enumerator m_keyEnumerator;

            /// <summary>
            /// 当前键值对
            /// </summary>
            public KeyValuePair<TKey, TValue> Current => new(m_keyEnumerator.Current, m_dictionary[m_keyEnumerator.Current]);
            object IEnumerator.Current => Current;

            /// <summary>
            /// 释放枚举器资源
            /// </summary>
            public void Dispose() => m_keyEnumerator.Dispose();

            /// <summary>
            /// 移动到下一个元素
            /// </summary>
            /// <returns>如果成功移动到下一个元素则返回true</returns>
            public bool MoveNext() => m_keyEnumerator.MoveNext();

            /// <summary>
            /// 重置枚举器到初始位置
            /// </summary>
            public void Reset() => ((IEnumerator<TKey>)m_keyEnumerator).Reset();
        }

        /// <summary>
        /// 将字典内容复制到数组
        /// </summary>
        /// <param name="array">目标数组</param>
        /// <param name="arrayIndex">开始复制的索引</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => Pairs.ToArray().CopyTo(array, arrayIndex);

        /// <summary>
        /// 获取类型化的枚举器
        /// </summary>
        /// <returns>枚举器实例</returns>
        public Enumerator GetEnumerator() => new() { m_dictionary = this, m_keyEnumerator = new ArraySegment<TKey>(AllKeys).GetEnumerator() };
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 所有键值对的枚举
        /// </summary>
        protected IEnumerable<KeyValuePair<TKey, TValue>> Pairs => GetEnumerator().AsEnumerable();

        /// <summary>
        /// 尝试获取指定键的值
        /// 对于枚举字典，总是能获取到值（可能是默认值）
        /// </summary>
        /// <param name="key">枚举键</param>
        /// <param name="value">输出的值</param>
        /// <returns>始终返回true</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            value = this[key];
            return true;
        }

        /// <summary>
        /// 清空字典（未实现）
        /// 抛出NotImplementedException异常
        /// </summary>
        public void Clear() => throw new NotImplementedException();

        /// <summary>
        /// 移除指定键（未实现）
        /// 抛出NotImplementedException异常
        /// </summary>
        /// <param name="key">要移除的键</param>
        /// <returns>不会返回，总是抛出异常</returns>
        public bool Remove(TKey key) => throw new NotImplementedException();

        /// <summary>
        /// 移除指定键值对（未实现）
        /// 抛出NotImplementedException异常
        /// </summary>
        /// <param name="item">要移除的键值对</param>
        /// <returns>不会返回，总是抛出异常</returns>
        public bool Remove(KeyValuePair<TKey, TValue> item) => throw new NotImplementedException();
    }
}
