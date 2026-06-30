using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace BetterLyrics.WinUI3.Controls
{
    public partial class SafeNumberBox : NumberBox
    {
        private bool _isSyncing = false;

        public SafeNumberBox()
        {
            this.ValueChanged += OnValueChanged;
            this.Loaded += OnSafeNumberBoxLoaded;
        }

        private void OnSafeNumberBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (IsIntegerOnly)
            {
                var formatter = new Windows.Globalization.NumberFormatting.DecimalFormatter
                {
                    FractionDigits = 0,
                    NumberRounder = new Windows.Globalization.NumberFormatting.IncrementNumberRounder
                    {
                        Increment = 1,
                        RoundingAlgorithm = Windows.Globalization.NumberFormatting.RoundingAlgorithm.RoundHalfUp
                    }
                };

                this.NumberFormatter = formatter;
            }
        }

        public double DefaultValue
        {
            get { return (double)GetValue(DefaultValueProperty); }
            set { SetValue(DefaultValueProperty, value); }
        }

        public static readonly DependencyProperty DefaultValueProperty =
            DependencyProperty.Register(nameof(DefaultValue), typeof(double), typeof(SafeNumberBox), new PropertyMetadata(0.0));

        public bool IsIntegerOnly
        {
            get { return (bool)GetValue(IsIntegerOnlyProperty); }
            set { SetValue(IsIntegerOnlyProperty, value); }
        }

        public static readonly DependencyProperty IsIntegerOnlyProperty =
            DependencyProperty.Register(nameof(IsIntegerOnly), typeof(bool), typeof(SafeNumberBox), new PropertyMetadata(false));

        public int IntValue
        {
            get { return (int)GetValue(IntValueProperty); }
            set { SetValue(IntValueProperty, value); }
        }

        public static readonly DependencyProperty IntValueProperty =
            DependencyProperty.Register(nameof(IntValue), typeof(int), typeof(SafeNumberBox), new PropertyMetadata(0, OnIntValueInternalChanged));

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
                double finalValue = sender.Value;

                if (double.IsNaN(finalValue))
                {
                    finalValue = DefaultValue;
                }

                if (IsIntegerOnly)
                {
                    finalValue = Math.Round(finalValue);
                }

                if (sender.Value != finalValue || double.IsNaN(sender.Value))
                {
                    sender.Value = finalValue;
                    if (double.IsNaN(args.NewValue))
                    {
                        sender.Text = finalValue.ToString();
                    }
                }

                int newIntValue = Convert.ToInt32(finalValue);
                if (this.IntValue != newIntValue)
                {
                    this.IntValue = newIntValue;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }
}
