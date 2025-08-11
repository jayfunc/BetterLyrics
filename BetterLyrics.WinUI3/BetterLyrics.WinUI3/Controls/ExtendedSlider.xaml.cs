using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class ExtendedSlider : UserControl
    {
        public ExtendedSlider()
        {
            InitializeComponent();
        }

        private void Subtract()
        {
            if (Value - Frequency < Minimum)
            {
                Value = Minimum;
            }
            else
            {
                Value -= Frequency;
            }
        }

        private void Add()
        {
            if (Value + Frequency > Maximum)
            {
                Value = Maximum;
            }
            else
            {
                Value += Frequency;
            }
        }

        private void SubtractTimer_Tick(object? sender, object e)
        {
            Subtract();
        }

        private void AddTimer_Tick(object? sender, object e)
        {
            Add();
        }

        public static readonly DependencyProperty FrequencyProperty =
            DependencyProperty.Register(nameof(Frequency), typeof(double), typeof(ExtendedSlider), new PropertyMetadata(default));
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(ExtendedSlider), new PropertyMetadata(default));
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(ExtendedSlider), new PropertyMetadata(default));
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(ExtendedSlider), new PropertyMetadata(default));
        public static readonly DependencyProperty DefaultProperty =
            DependencyProperty.Register(nameof(Default), typeof(double), typeof(ExtendedSlider), new PropertyMetadata(default));
        public static readonly DependencyProperty ResetButtonVisibilityProperty =
            DependencyProperty.Register(nameof(ResetButtonVisibility), typeof(Visibility), typeof(ExtendedSlider), new PropertyMetadata(Visibility.Visible));
        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(nameof(Unit), typeof(string), typeof(ExtendedSlider), new PropertyMetadata(""));

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
            set => SetValue(ValueProperty, value);
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

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            Value = Default;
        }

        private void SubtractButton_Click(object sender, RoutedEventArgs e)
        {
            Subtract();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Add();
        }
    }
}
