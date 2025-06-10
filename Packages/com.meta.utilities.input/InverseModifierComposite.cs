// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.ComponentModel;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

// based on com.unity.inputsystem/InputSystem/Actions/Composites/OneModifierComposite.cs

/// <summary>
/// 反向修饰符组合
/// 当修饰符按钮没有被按下时，绑定才生效
/// 显示格式为 "(NOT {modifier})+{binding}"
/// </summary>
[DisplayStringFormat("(NOT {modifier})+{binding}")]
[DisplayName("Binding With One Inverse Modifier")]
public class InverseModifierComposite : InputBindingComposite
{
    /// <summary>
    /// 在运行时和编辑器加载时初始化
    /// 向输入系统注册反向修饰符组合
    /// </summary>
    [RuntimeInitializeOnLoadMethod]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Initialize()
    {
        InputSystem.RegisterBindingComposite<InverseModifierComposite>("InverseModifierComposite");
    }

    /// <summary>
    /// 读取修饰符值（取反）
    /// 返回修饰符按钮没有被按下的状态
    /// </summary>
    /// <param name="context">输入绑定组合上下文</param>
    /// <returns>修饰符按钮的反向状态</returns>
    private bool ReadModifierValue(InputBindingCompositeContext context) => !context.ReadValueAsButton(m_modifier);

    /// <summary>
    /// Binding for the button that acts as a modifier, e.g. <c>&lt;Keyboard/ctrl</c>.
    /// </summary>
    /// <value>Part index to use with <see cref="InputBindingCompositeContext.ReadValue{T}(int)"/>.</value>
    /// <remarks>
    /// 此属性由输入系统自动分配
    /// </remarks>
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    // ReSharper disable once UnassignedField.Global
    [InputControl(layout = "Button")] public int m_modifier;

    /// <summary>
    /// 被修饰符门控的控件绑定
    /// 当修饰符被认为没有按下时，组合将采用此控件的值
    /// （即幅度等于或大于按钮按下点）
    /// </summary>
    /// <value>用于 <see cref="InputBindingCompositeContext.ReadValue{T}(int)"/> 的部分索引</value>
    /// <remarks>
    /// 此属性由输入系统自动分配
    /// </remarks>
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    // ReSharper disable once UnassignedField.Global
    [InputControl] public int m_binding;

    /// <summary>
    /// 从绑定到 <see cref="m_binding"/> 的控件读取的值类型
    /// </summary>
    public override Type valueType => m_valueType;

    /// <summary>
    /// 可能从绑定到 <see cref="m_binding"/> 的控件读取的最大值的大小
    /// </summary>
    public override int valueSizeInBytes => m_valueSizeInBytes;

    /// <summary>
    /// 值大小（字节）
    /// </summary>
    private int m_valueSizeInBytes;

    /// <summary>
    /// 值类型
    /// </summary>
    private Type m_valueType;

    /// <summary>
    /// 计算输入的幅度
    /// 只有当修饰符没有被按下时才返回绑定控件的幅度
    /// </summary>
    /// <param name="context">输入绑定组合上下文</param>
    /// <returns>输入幅度值</returns>
    public override float EvaluateMagnitude(ref InputBindingCompositeContext context) =>
        ReadModifierValue(context) ? context.EvaluateMagnitude(m_binding) : default;

    /// <summary>
    /// 读取输入值
    /// 如果修饰符没有被按下，则读取绑定控件的值；否则清空缓冲区
    /// </summary>
    /// <param name="context">输入绑定组合上下文</param>
    /// <param name="buffer">值缓冲区</param>
    /// <param name="bufferSize">缓冲区大小</param>
    public override unsafe void ReadValue(ref InputBindingCompositeContext context, void* buffer, int bufferSize)
    {
        if (ReadModifierValue(context))
            context.ReadValue(m_binding, buffer, bufferSize);
        else
            UnsafeUtility.MemClear(buffer, m_valueSizeInBytes);
    }

    /// <summary>
    /// 完成设置
    /// 确定值类型和大小
    /// </summary>
    /// <param name="context">输入绑定组合上下文</param>
    protected override void FinishSetup(ref InputBindingCompositeContext context)
    {
        DetermineValueTypeAndSize(ref context, m_binding, out m_valueType, out m_valueSizeInBytes);
    }

    /// <summary>
    /// 以对象形式读取值
    /// 如果修饰符没有被按下，则返回绑定控件的值对象
    /// </summary>
    /// <param name="context">输入绑定组合上下文</param>
    /// <returns>值对象</returns>
    public override object ReadValueAsObject(ref InputBindingCompositeContext context) =>
        ReadModifierValue(context) ? context.ReadValueAsObject(m_binding) : default;

    /// <summary>
    /// 确定值类型和大小的内部静态方法
    /// 遍历所有控件以确定合适的值类型和最大值大小
    /// </summary>
    /// <param name="context">输入绑定组合上下文</param>
    /// <param name="part">部分索引</param>
    /// <param name="valueType">输出的值类型</param>
    /// <param name="valueSizeInBytes">输出的值大小（字节）</param>
    internal static void DetermineValueTypeAndSize(ref InputBindingCompositeContext context, int part, out Type valueType, out int valueSizeInBytes)
    {
        valueSizeInBytes = 0;

        Type type = null;
        foreach (var control in context.controls)
        {
            if (control.part != part)
                continue;

            var controlType = control.control.valueType;
            if (type == null || controlType.IsAssignableFrom(type))
                type = controlType;
            else if (!type.IsAssignableFrom(controlType))
                type = typeof(object);

            valueSizeInBytes = Math.Max(control.control.valueSizeInBytes, valueSizeInBytes);
        }

        valueType = type;
    }
}
