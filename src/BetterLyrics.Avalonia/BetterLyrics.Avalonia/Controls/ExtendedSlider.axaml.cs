using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BetterLyrics.Core.Events;

namespace BetterLyrics.Avalonia.Controls;

public partial class ExtendedSlider : UserControl
{
    public static readonly StyledProperty<double> FrequencyProperty =
        AvaloniaProperty.Register<ExtendedSlider, double>(nameof(Frequency), 1.0);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<ExtendedSlider, double>(nameof(Minimum), 0.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<ExtendedSlider, double>(nameof(Maximum), 100.0);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ExtendedSlider, double>(nameof(Value), 0.0);

    // 🐛 已修复：原 WinUI 代码中 nameof(Value) 注册到了 RoundedValue 上
    private static readonly StyledProperty<string> RoundedValueProperty =
        AvaloniaProperty.Register<ExtendedSlider, string>(nameof(RoundedValue), "0");

    public static readonly StyledProperty<double> DefaultProperty =
        AvaloniaProperty.Register<ExtendedSlider, double>(nameof(Default), 0.0);

    // 💡 优化：Avalonia 中控制可见性统一使用 IsVisible (bool)，所以将原 Visibility 属性改为 bool
    public static readonly StyledProperty<bool> ShowResetButtonProperty =
        AvaloniaProperty.Register<ExtendedSlider, bool>(nameof(ShowResetButton), true);

    public static readonly StyledProperty<string> UnitProperty =
        AvaloniaProperty.Register<ExtendedSlider, string>(nameof(Unit), string.Empty);

    public static readonly StyledProperty<bool> IsSliderEnabledProperty =
        AvaloniaProperty.Register<ExtendedSlider, bool>(nameof(IsSliderEnabled), true);

    public ExtendedSlider()
    {
        InitializeComponent();
    }

    public double Frequency
    {
        get => GetValue(FrequencyProperty);
        set => SetValue(FrequencyProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private string RoundedValue
    {
        get => GetValue(RoundedValueProperty);
        set => SetValue(RoundedValueProperty, value);
    }

    public double Default
    {
        get => GetValue(DefaultProperty);
        set => SetValue(DefaultProperty, value);
    }

    public bool ShowResetButton
    {
        get => GetValue(ShowResetButtonProperty);
        set => SetValue(ShowResetButtonProperty, value);
    }

    public string Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public bool IsSliderEnabled
    {
        get => GetValue(IsSliderEnabledProperty);
        set => SetValue(IsSliderEnabledProperty, value);
    }

    public event EventHandler<ExtendedSliderValueChangedByUserEventArgs>? ValueChangedByUser;

    // 🌟 在属性系统底层监听变化，比在属性 Setter 中拦截安全得多
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            var val = change.GetNewValue<double>();
            RoundedValue = val.ToString("F1").Replace(".0", "");
        }
    }

    private void Subtract()
    {
        if (Value - Frequency < Minimum)
            Value = Minimum;
        else
            Value -= Frequency;
    }

    private void Add()
    {
        if (Value + Frequency > Maximum)
            Value = Maximum;
        else
            Value += Frequency;
    }

    private void ResetButton_Click(object? sender, RoutedEventArgs e)
    {
        Value = Default;
        ValueChangedByUser?.Invoke(this, new ExtendedSliderValueChangedByUserEventArgs(Value));
    }

    private void SubtractButton_Click(object? sender, RoutedEventArgs e)
    {
        Subtract();
        ValueChangedByUser?.Invoke(this, new ExtendedSliderValueChangedByUserEventArgs(Value));
    }

    private void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        Add();
        ValueChangedByUser?.Invoke(this, new ExtendedSliderValueChangedByUserEventArgs(Value));
    }

    private void Slider_PointerMoved(object? sender, PointerEventArgs e)
    {
        // 如果需要过滤确保只有鼠标按下时拖拽才触发，可以检查 e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
        ValueChangedByUser?.Invoke(this, new ExtendedSliderValueChangedByUserEventArgs(Value));
    }
}