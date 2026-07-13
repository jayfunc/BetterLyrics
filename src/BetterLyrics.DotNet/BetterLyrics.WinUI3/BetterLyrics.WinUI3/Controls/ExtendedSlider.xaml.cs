using System;
using BetterLyrics.Core.Events;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class ExtendedSlider : UserControl
{
    public static readonly DependencyProperty FrequencyProperty =
        DependencyProperty.Register(nameof(Frequency), typeof(double), typeof(ExtendedSlider),
            new PropertyMetadata(1.0));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(ExtendedSlider),
            new PropertyMetadata(default));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(ExtendedSlider),
            new PropertyMetadata(default));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(ExtendedSlider),
            new PropertyMetadata(default));

    private static readonly DependencyProperty RoundedValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(ExtendedSlider),
            new PropertyMetadata(default));

    public static readonly DependencyProperty DefaultProperty =
        DependencyProperty.Register(nameof(Default), typeof(double), typeof(ExtendedSlider),
            new PropertyMetadata(default));

    public static readonly DependencyProperty ResetButtonVisibilityProperty =
        DependencyProperty.Register(nameof(ResetButtonVisibility), typeof(Visibility), typeof(ExtendedSlider),
            new PropertyMetadata(Visibility.Visible));

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(nameof(Unit), typeof(string), typeof(ExtendedSlider), new PropertyMetadata(""));

    public static readonly DependencyProperty IsSliderEnabledProperty =
        DependencyProperty.Register(nameof(IsSliderEnabled), typeof(bool), typeof(ExtendedSlider),
            new PropertyMetadata(true));

    public ExtendedSlider()
    {
        InitializeComponent();
    }

    public double Frequency
    {
        get => (double)GetValue(FrequencyProperty);
        set => SetValue(FrequencyProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set
        {
            SetValue(ValueProperty, value);
            SetValue(RoundedValueProperty, value.ToString("F1").Replace(".0", ""));
        }
    }

    private string RoundedValue
    {
        get => (string)GetValue(RoundedValueProperty);
        set => SetValue(RoundedValueProperty, value);
    }

    public double Default
    {
        get => (double)GetValue(DefaultProperty);
        set => SetValue(DefaultProperty, value);
    }

    public Visibility ResetButtonVisibility
    {
        get => (Visibility)GetValue(ResetButtonVisibilityProperty);
        set => SetValue(ResetButtonVisibilityProperty, value);
    }

    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public bool IsSliderEnabled
    {
        get => (bool)GetValue(IsSliderEnabledProperty);
        set => SetValue(IsSliderEnabledProperty, value);
    }

    public event EventHandler<ExtendedSliderValueChangedByUserEventArgs>? ValueChangedByUser;

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

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        Value = Default;
        ValueChangedByUser?.Invoke(this, new ExtendedSliderValueChangedByUserEventArgs(Value));
    }

    private void SubtractButton_Click(object sender, RoutedEventArgs e)
    {
        Subtract();
        ValueChangedByUser?.Invoke(this, new ExtendedSliderValueChangedByUserEventArgs(Value));
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        Add();
        ValueChangedByUser?.Invoke(this, new ExtendedSliderValueChangedByUserEventArgs(Value));
    }

    private void Slider_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        ValueChangedByUser?.Invoke(this, new ExtendedSliderValueChangedByUserEventArgs(Value));
    }
}