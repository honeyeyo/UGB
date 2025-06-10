// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.Utilities
{
    /// <summary>
    /// 扩展方法工具类
    /// 提供各种类型的扩展方法，包括数值比较、反射操作、集合处理、
    /// 字符串操作、四元数运算、协程处理等功能
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// 判断两个浮点数是否在指定精度范围内相等
        /// </summary>
        /// <param name="a">第一个浮点数</param>
        /// <param name="b">第二个浮点数</param>
        /// <param name="epsilon">精度阈值，默认为0.0001f</param>
        /// <returns>如果两个数在精度范围内相等则返回true</returns>
        public static bool IsCloseTo(this float a, float b, float epsilon = 0.0001f)
        {
            return Mathf.Abs(a - b) < epsilon;
        }

        /// <summary>
        /// 判断两个Vector3是否在指定半径范围内相等
        /// </summary>
        /// <param name="a">第一个Vector3</param>
        /// <param name="b">第二个Vector3</param>
        /// <param name="epsilonRadius">精度半径，默认为0.0001f</param>
        /// <returns>如果两个向量在精度范围内相等则返回true</returns>
        public static bool IsCloseTo(this Vector3 a, Vector3 b, float epsilonRadius = 0.0001f)
        {
            return (a - b).sqrMagnitude < epsilonRadius * epsilonRadius;
        }

        /// <summary>
        /// 通过反射获取对象的指定方法并创建委托
        /// </summary>
        /// <typeparam name="Delegate">委托类型</typeparam>
        /// <param name="target">目标对象</param>
        /// <param name="name">方法名称</param>
        /// <returns>创建的委托，如果找不到匹配的方法则返回默认值</returns>
        public static Delegate GetMethod<Delegate>(this object target, string name)
        {
            var flags = BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic;
            var methods = target.GetType().GetMethods(flags);
            foreach (var method in methods)
            {
                if (method.Name == name)
                {
                    try
                    {
                        return (Delegate)(object)method.CreateDelegate(typeof(Delegate), target);
                    }
                    catch (ArgumentException) { }
                }
            }
            return default;
        }

        /// <summary>
        /// 通过反射获取对象的指定字段值
        /// 支持继承层次结构中的字段查找
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">字段名称</param>
        /// <returns>字段的值，如果找不到则返回默认值</returns>
        public static object GetField(this object target, string name)
        {
            var flags = BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic;
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var fields = type.GetFields(flags);
                foreach (var field in fields)
                {
                    if (field.Name == name)
                    {
                        try
                        {
                            return field.GetValue(target);
                        }
                        catch (ArgumentException) { }
                    }
                }
            }
            return default;
        }

        /// <summary>
        /// 通过反射获取对象的指定属性值
        /// 支持继承层次结构中的属性查找
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">属性名称</param>
        /// <returns>属性的值，如果找不到则返回默认值</returns>
        public static object GetProperty(this object target, string name)
        {
            var flags = BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic;
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var props = type.GetProperties(flags);
                foreach (var prop in props)
                {
                    if (prop.Name == name)
                    {
                        try
                        {
                            return prop.GetValue(target);
                        }
                        catch (ArgumentException) { }
                    }
                }
            }
            return default;
        }

        /// <summary>
        /// 通过反射设置对象的指定属性值
        /// 支持继承层次结构中的属性查找
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">属性名称</param>
        /// <param name="value">要设置的值</param>
        public static void SetProperty(this object target, string name, object value)
        {
            var flags = BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic;
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var props = type.GetProperties(flags);
                foreach (var prop in props)
                {
                    if (prop.Name == name)
                    {
                        try
                        {
                            prop.SetValue(target, value);
                        }
                        catch (ArgumentException) { }
                    }
                }
            }
        }

        /// <summary>
        /// 将集合转换为用分隔符连接的字符串
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="list">要转换的集合</param>
        /// <param name="separator">分隔符，默认为", "</param>
        /// <returns>连接后的字符串</returns>
        public static string ListToString<T>(this IEnumerable<T> list, string separator = ", ") => string.Join(separator, list);

        /// <summary>
        /// 为Vector2添加Z坐标，转换为Vector3
        /// </summary>
        /// <param name="vec">Vector2实例</param>
        /// <param name="z">Z坐标值</param>
        /// <returns>转换后的Vector3</returns>
        public static Vector3 WithZ(this Vector2 vec, float z) => new(vec.x, vec.y, z);

        /// <summary>
        /// 将对象序列化为JSON字符串
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="pretty">是否格式化JSON，默认为true</param>
        /// <returns>JSON字符串</returns>
        public static string ToJson(this object obj, bool pretty = true) => JsonUtility.ToJson(obj, pretty);

        /// <summary>
        /// 将两个枚举按元素配对组合
        /// </summary>
        /// <typeparam name="A">第一个枚举的元素类型</typeparam>
        /// <typeparam name="B">第二个枚举的元素类型</typeparam>
        /// <param name="enumA">第一个枚举</param>
        /// <param name="enumB">第二个枚举</param>
        /// <returns>元组的枚举，包含配对的元素</returns>
        public static IEnumerable<(A first, B second)> Zip<A, B>(this IEnumerable<A> enumA, IEnumerable<B> enumB)
        {
            var a = enumA.GetEnumerator();
            var b = enumB.GetEnumerator();
            while (a.MoveNext() && b.MoveNext())
            {
                yield return (a.Current, b.Current);
            }
        }

        /// <summary>
        /// 为枚举中的每个元素添加索引
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="values">要枚举的集合</param>
        /// <returns>包含索引和值的键值对枚举</returns>
        public static IEnumerable<KeyValuePair<int, T>> Enumerate<T>(this IEnumerable<T> values)
        {
            var i = 0;
            foreach (var value in values)
            {
                yield return new(i, value);
                i += 1;
            }
        }

        /// <summary>
        /// 将值类型枚举转换为可为空类型的枚举
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="values">原始枚举</param>
        /// <returns>可为空类型的枚举</returns>
        public static IEnumerable<T?> AsNullables<T>(this IEnumerable<T> values) where T : struct => values.Cast<T?>();

        /// <summary>
        /// 从枚举中排除指定的元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="values">原始枚举</param>
        /// <param name="exempt">要排除的元素</param>
        /// <returns>排除指定元素后的枚举</returns>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> values, T exempt)
        {
            var comparer = EqualityComparer<T>.Default;
            foreach (var value in values)
                if (comparer.Equals(value, exempt) is false)
                    yield return value;
        }

        /// <summary>
        /// 将枚举转换为临时的NativeArray
        /// </summary>
        /// <typeparam name="T">非托管类型</typeparam>
        /// <param name="values">要转换的枚举</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>临时的NativeArray</returns>
        public static NativeArray<T> ToTempArray<T>(this IEnumerable<T> values, int maxLength) where T : unmanaged
        {
            var array = new NativeList<T>(maxLength, Allocator.Temp);
            foreach (var value in values)
            {
                array.AddNoResize(value);
            }
            return array;
        }

        /// <summary>
        /// 检查字符串是否为null或空
        /// </summary>
        /// <param name="str">要检查的字符串</param>
        /// <returns>如果字符串为null或空则返回true</returns>
        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);

        /// <summary>
        /// 检查字符串是否为null、空或仅包含空白字符
        /// </summary>
        /// <param name="str">要检查的字符串</param>
        /// <returns>如果字符串为null、空或仅包含空白字符则返回true</returns>
        public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

        /// <summary>
        /// 如果字符串为null或空，则返回替代字符串
        /// </summary>
        /// <param name="str">原始字符串</param>
        /// <param name="other">替代字符串</param>
        /// <returns>原始字符串或替代字符串</returns>
        public static string IfNullOrEmpty(this string str, string other) => str.IsNullOrEmpty() ? other : str;

        /// <summary>
        /// 生成从0到指定数值的整数序列
        /// </summary>
        /// <param name="end">结束数值（不包含）</param>
        /// <returns>整数序列</returns>
        public static IEnumerable<int> CountTo(this int end)
        {
            Assert.IsTrue(end >= 0, $"Cannot count to {end}.");

            for (var i = 0; i != end; ++i)
                yield return i;
        }

        /// <summary>
        /// 将枚举器转换为可枚举集合（泛型版本）
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="enumerator">枚举器</param>
        /// <returns>可枚举集合</returns>
        public static IEnumerable<T> AsEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        /// <summary>
        /// 将枚举器转换为可枚举集合（非泛型版本）
        /// </summary>
        /// <param name="enumerator">枚举器</param>
        /// <returns>可枚举集合</returns>
        public static IEnumerable AsEnumerable(this IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        /// <summary>
        /// 将非泛型枚举器转换为泛型可枚举集合
        /// </summary>
        /// <typeparam name="T">目标元素类型</typeparam>
        /// <param name="enumerator">非泛型枚举器</param>
        /// <returns>泛型可枚举集合</returns>
        public static IEnumerable<T> AsEnumerable<T>(this IEnumerator enumerator) =>
            enumerator.AsEnumerable().Cast<T>();

        /// <summary>
        /// 计算从一个四元数到另一个四元数的相对旋转
        /// </summary>
        /// <param name="from">起始四元数</param>
        /// <param name="to">目标四元数</param>
        /// <returns>相对旋转四元数</returns>
        public static Quaternion FromTo(this Quaternion from, Quaternion to) => Quaternion.Inverse(from) * to;

        /// <summary>
        /// 按比例缩放四元数旋转
        /// </summary>
        /// <param name="rotation">要缩放的旋转</param>
        /// <param name="t">缩放因子</param>
        /// <returns>缩放后的旋转</returns>
        public static Quaternion Scale(this Quaternion rotation, float t) => Quaternion.SlerpUnclamped(Quaternion.identity, rotation, t);

        /// <summary>
        /// 将轴角向量（角度）转换为四元数
        /// </summary>
        /// <param name="axisAngle">轴角向量，角度以度为单位</param>
        /// <returns>对应的四元数</returns>
        public static Quaternion AxisAngleToQuaternion(this Vector3 axisAngle) =>
            Quaternion.AngleAxis(axisAngle.magnitude, axisAngle.normalized);

        /// <summary>
        /// 将四元数转换为轴角向量（角度）
        /// </summary>
        /// <param name="rotation">四元数旋转</param>
        /// <returns>轴角向量，角度以度为单位</returns>
        public static Vector3 QuaternionToAxisAngle(this Quaternion rotation)
        {
            rotation.ToAngleAxis(out var angle, out var axis);
            return angle * axis;
        }

        /// <summary>
        /// 获取刚体的角速度（以度为单位的四元数形式）
        /// </summary>
        /// <param name="rigidbody">刚体对象</param>
        /// <returns>角速度四元数</returns>
        public static Quaternion GetAngularVelocity(this Rigidbody rigidbody) =>
            (rigidbody.angularVelocity * Mathf.Rad2Deg).AxisAngleToQuaternion();

        /// <summary>
        /// 设置刚体的角速度（使用度为单位的四元数）
        /// </summary>
        /// <param name="rigidbody">刚体对象</param>
        /// <param name="angularVelocity">角速度四元数</param>
        public static void SetAngularVelocity(this Rigidbody rigidbody, Quaternion angularVelocity) =>
            rigidbody.angularVelocity = angularVelocity.QuaternionToAxisAngle() * Mathf.Deg2Rad;

        /// <summary>
        /// 在协程完成后执行指定动作
        /// </summary>
        /// <param name="routine">协程</param>
        /// <param name="action">要执行的动作</param>
        /// <returns>组合的协程</returns>
        public static IEnumerator Then(this IEnumerator routine, Action action)
        {
            while (routine.MoveNext())
                yield return routine.Current;
            action();
        }

        /// <summary>
        /// 解构IGrouping为键和值的集合
        /// </summary>
        /// <typeparam name="TKey">键的类型</typeparam>
        /// <typeparam name="TElement">元素的类型</typeparam>
        /// <param name="grouping">分组对象</param>
        /// <param name="key">输出的键</param>
        /// <param name="values">输出的值集合</param>
        public static void Deconstruct<TKey, TElement>(
            this IGrouping<TKey, TElement> grouping,
            out TKey key,
            out IEnumerable<TElement> values) =>
            (key, values) = (grouping.Key, grouping);

        /// <summary>
        /// 查找指定值在枚举中的索引
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="values">要搜索的枚举</param>
        /// <param name="value">要查找的值</param>
        /// <returns>找到的索引，如果未找到则返回null</returns>
        public static int? IndexOf<T>(this IEnumerable<T> values, T value)
        {
            var comparer = EqualityComparer<T>.Default;
            var count = 0;
            foreach (var el in values)
            {
                if (comparer.Equals(el, value))
                    return count;
                count += 1;
            }
            return null;
        }

        /// <summary>
        /// 过滤出存在的Unity对象（排除已销毁的对象）
        /// </summary>
        /// <typeparam name="T">Unity对象类型</typeparam>
        /// <param name="values">要过滤的对象枚举</param>
        /// <returns>存在的对象枚举</returns>
        public static IEnumerable<T> WhereExists<T>(this IEnumerable<T> values) where T : UnityEngine.Object
        {
            foreach (var value in values)
                if (value != null)
                    yield return value;
        }

        /// <summary>
        /// 过滤出非空的引用类型对象
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        /// <param name="values">要过滤的对象枚举</param>
        /// <returns>非空对象枚举</returns>
        public static IEnumerable<T> WhereNonNull<T>(this IEnumerable<T> values) where T : class
        {
            foreach (var value in values)
                if (value != null)
                    yield return value;
        }

        /// <summary>
        /// 过滤出有值的可空值类型
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="values">要过滤的可空值枚举</param>
        /// <returns>有值的对象枚举</returns>
        public static IEnumerable<T> WhereNonNull<T>(this IEnumerable<T?> values) where T : struct
        {
            foreach (var value in values)
                if (value.HasValue)
                    yield return value.Value;
        }

        /// <summary>
        /// 过滤出非默认值的对象
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="values">要过滤的枚举</param>
        /// <returns>非默认值的对象枚举</returns>
        public static IEnumerable<T> WhereNonDefault<T>(this IEnumerable<T> values)
        {
            var comparer = EqualityComparer<T>.Default;
            foreach (var value in values)
                if (!comparer.Equals(value, default))
                    yield return value;
        }

        /// <summary>
        /// 计算Vector3集合的总和
        /// </summary>
        /// <param name="values">Vector3枚举</param>
        /// <returns>所有向量的总和</returns>
        public static Vector3 Sum(this IEnumerable<Vector3> values)
        {
            var sum = Vector3.zero;
            foreach (var value in values)
                sum += value;
            return sum;
        }

        /// <summary>
        /// 计算Vector3集合的平均值
        /// </summary>
        /// <param name="values">Vector3枚举</param>
        /// <returns>所有向量的平均值</returns>
        public static Vector3 Average(this IEnumerable<Vector3> values)
        {
            var avg = Vector3.zero;
            foreach (var (i, value) in values.Enumerate())
            {
                var mul = 1.0f / (i + 1);
                avg = avg * (i * mul) + value * mul;
            }
            return avg;
        }

        /// <summary>
        /// 从数组中随机选择一个元素的引用
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="values">数组</param>
        /// <returns>随机元素的引用</returns>
        public static ref T RandomElement<T>(this T[] values) =>
            ref values[UnityEngine.Random.Range(0, values.Length)];

        /// <summary>
        /// 修复事件中的空引用（仅在开发版本和编辑器中执行）
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <param name="evt">要修复的事件</param>
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void FixEventImpl<T>(ref T evt) where T : Delegate
        {
            if (evt != null)
            {
                foreach (var d in evt.GetInvocationList())
                {
                    // Debug.LogWarning(("event", d.Method, d.Target, d));
                    if (d.Target is UnityEngine.Object obj && obj == null)
                    {
                        var exception = new MissingReferenceException($"Event handler {d.Method.DeclaringType}.{d.Method.Name} is on a null target object and must be unsubscribed from in OnDisable or OnDestroy.");
                        Debug.LogException(exception, obj);
                        evt = (T)Delegate.Remove(evt, d);
                    }
                }
            }
        }

        /// <summary>
        /// 修复事件中的空引用并返回修复后的事件
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <param name="evt">要修复的事件</param>
        /// <returns>修复后的事件</returns>
        public static T FixEvent<T>(this T evt) where T : Delegate
        {
            FixEventImpl(ref evt);
            return evt;
        }

        /// <summary>
        /// 递归设置游戏对象及其所有子对象的层级
        /// </summary>
        /// <param name="go">游戏对象</param>
        /// <param name="layer">要设置的层级</param>
        public static void SetLayerToChilds(this GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (var i = 0; i < t.childCount; ++i)
            {
                var child = t.GetChild(i);
                child.gameObject.SetLayerToChilds(layer);
            }
        }

        /// <summary>
        /// 顺序执行两个协程
        /// </summary>
        /// <param name="first">第一个协程</param>
        /// <param name="second">第二个协程</param>
        /// <returns>组合的协程</returns>
        public static IEnumerator RoutineThen(this IEnumerator first, IEnumerator second)
        {
            yield return first;
            yield return second;
        }

        /// <summary>
        /// 在等待指令后执行协程
        /// </summary>
        /// <param name="first">等待指令</param>
        /// <param name="second">要执行的协程</param>
        /// <returns>组合的协程</returns>
        public static IEnumerator RoutineThen(this YieldInstruction first, IEnumerator second)
        {
            yield return first;
            yield return second;
        }

        /// <summary>
        /// 将Task转换为协程等待条件
        /// </summary>
        /// <param name="task">要等待的任务</param>
        /// <returns>等待任务完成的WaitUntil</returns>
        public static WaitUntil ToRoutine(this Task task) => new(() => task.IsCompleted);

        /// <summary>
        /// 根据指定键选择器找到具有最大值的元素，如果集合为空则返回默认值
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="values">要搜索的枚举</param>
        /// <param name="toKey">键选择器函数</param>
        /// <returns>具有最大键值的元素，如果集合为空则返回默认值</returns>
        public static T MaxByOrDefault<T>(this IEnumerable<T> values, Func<T, float> toKey)
        {
            var iter = values.GetEnumerator();
            if (iter.MoveNext() is false)
                return default;

            var maxValue = iter.Current;
            var max = toKey(maxValue);
            while (iter.MoveNext())
            {
                var currentMax = toKey(iter.Current);
                if (max < currentMax)
                {
                    max = currentMax;
                    maxValue = iter.Current;
                }
            }

            return maxValue;
        }

        /// <summary>
        /// 将Span转换为NativeSlice
        /// </summary>
        /// <typeparam name="T">非托管类型</typeparam>
        /// <param name="value">要转换的Span</param>
        /// <returns>对应的NativeSlice</returns>
        public static NativeSlice<T> AsNativeSlice<T>(this Span<T> value) where T : unmanaged
        {
            if (value.Length == 0)
                return default;

            unsafe
            {
                var ptr = UnsafeUtility.AddressOf(ref value[0]);
                var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(ptr, UnsafeUtility.SizeOf<T>(), value.Length);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif
                return slice;
            }
        }

#nullable enable
        /// <summary>
        /// 解构可空的二元组
        /// </summary>
        /// <typeparam name="T0">第一个元素类型</typeparam>
        /// <typeparam name="T1">第二个元素类型</typeparam>
        /// <param name="value">可空的二元组</param>
        /// <param name="a">输出的第一个元素</param>
        /// <param name="b">输出的第二个元素</param>
        public static void Deconstruct<T0, T1>(this (T0, T1)? value, out T0? a, out T1? b)
        {
            (a, b) = value is { } data ? data : (default, default);
        }

        /// <summary>
        /// 解构可空的三元组
        /// </summary>
        /// <typeparam name="T0">第一个元素类型</typeparam>
        /// <typeparam name="T1">第二个元素类型</typeparam>
        /// <typeparam name="T2">第三个元素类型</typeparam>
        /// <param name="value">可空的三元组</param>
        /// <param name="a">输出的第一个元素</param>
        /// <param name="b">输出的第二个元素</param>
        /// <param name="c">输出的第三个元素</param>
        public static void Deconstruct<T0, T1, T2>(this (T0, T1, T2)? value, out T0? a, out T1? b, out T2? c)
        {
            (a, b, c) = value is { } data ? data : (default, default, default);
        }
#nullable restore
    }
}
