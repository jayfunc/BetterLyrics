using System;
using Windows.Globalization.NumberFormatting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Controls;

public partial class SafeNumberBox : NumberBox
{
    public static readonly DependencyProperty DefaultValueProperty =
        DependencyProperty.Register(nameof(DefaultValue), typeof(double), typeof(SafeNumberBox),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty IsIntegerOnlyProperty =
        DependencyProperty.Register(nameof(IsIntegerOnly), typeof(bool), typeof(SafeNumberBox),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IntValueProperty =
        DependencyProperty.Register(nameof(IntValue), typeof(int), typeof(SafeNumberBox),
            new PropertyMetadata(0, OnIntValueInternalChanged));

    private bool _isSyncing;

    public SafeNumberBox()
    {
        ValueChanged += OnValueChanged;
        Loaded += OnSafeNumberBoxLoaded;
    }

    public double DefaultValue
    {
        get => (double)GetValue(DefaultValueProperty);
        set => SetValue(DefaultValueProperty, value);
    }

    public bool IsIntegerOnly
    {
        get => (bool)GetValue(IsIntegerOnlyProperty);
        set => SetValue(IsIntegerOnlyProperty, value);
    }

    public int IntValue
    {
        get => (int)GetValue(IntValueProperty);
        set => SetValue(IntValueProperty, value);
    }

    private void OnSafeNumberBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (IsIntegerOnly)
        {
            var formatter = new DecimalFormatter
            {
                FractionDigits = 0,
                NumberRounder = new IncrementNumberRounder
                {
                    Increment = 1,
                    RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp
                }
            };

            NumberFormatter = formatter;
        }
    }

    private static void OnIntValueInternalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SafeNumberBox target)
        {
            if (target._isSyncing) return;

            target._isSyncing = true;
            target.Value = (int)e.NewValue;
            target._isSyncing = false;
        }
    }

    private void OnValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isSyncing) return;
        _isSyncing = true;

        try
        {
            var finalValue = sender.Value;

            if (double.IsNaN(finalValue)) finalValue = DefaultValue;

            if (IsIntegerOnly) finalValue = Math.Round(finalValue);

            if (sender.Value != finalValue || double.IsNaN(sender.Value))
            {
                sender.Value = finalValue;
                if (double.IsNaN(args.NewValue)) sender.Text = finalValue.ToString();
            }

            var newIntValue = Convert.ToInt32(finalValue);
            if (IntValue != newIntValue) IntValue = newIntValue;
        }
        finally
        {
            _isSyncing = false;
        }
    }
}