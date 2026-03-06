using CoolControls.WinUI3.Controls.Internals;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;

namespace CoolControls.WinUI3.Controls
{
    [ContentProperty(Name = "Child")]
    internal class RibbedGlassView : Control
    {
        public RibbedGlassView()
        {
            this.DefaultStyleKey = typeof(RibbedGlassView);
            this.Loaded += RibbedGlassVisual_Loaded;
            this.Unloaded += RibbedGlassVisual_Unloaded;
        }

        private RibbedGlassVisualHelper? helper;
        private Visual? contentVisual;
        private Canvas? EffectHost;
        private Border? ContentBorder;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            contentVisual = null;

            if (ContentBorder != null)
            {
                ContentBorder.SizeChanged -= ContentBorder_SizeChanged;
            }
            if (EffectHost != null)
            {
                ElementCompositionPreview.SetElementChildVisual(EffectHost, null);
            }

            ClearEffects();

            EffectHost = (Canvas)GetTemplateChild(nameof(EffectHost));
            ContentBorder = (Border)GetTemplateChild(nameof(ContentBorder));

            if (ContentBorder != null)
            {
                ContentBorder.SizeChanged += ContentBorder_SizeChanged;
                contentVisual = ElementCompositionPreview.GetElementVisual(ContentBorder);
            }

            UpdateEffects();
        }

        private RibbedGlassVisualHelper EnsureHelper()
        {
            if (helper == null)
            {
                helper = new RibbedGlassVisualHelper(ElementCompositionPreview.GetElementVisual(this).Compositor);
                helper.ChildVisualWidth = (float)RibWidth;
                helper.SourceVisualScale = (float)ChildScale;
                helper.BlurAmount = (float)BlurAmount;
            }
            return helper;
        }

        public UIElement Child
        {
            get { return (UIElement)GetValue(ChildProperty); }
            set { SetValue(ChildProperty, value); }
        }

        public static readonly DependencyProperty ChildProperty =
            DependencyProperty.Register("Child", typeof(UIElement), typeof(RibbedGlassView), new PropertyMetadata(null, (s, a) =>
            {
                if (s is RibbedGlassView sender && !Equals(a.NewValue, a.OldValue))
                {
                    sender.UpdateEffects();
                }
            }));



        public double RibWidth
        {
            get { return (double)GetValue(RibWidthProperty); }
            set { SetValue(RibWidthProperty, value); }
        }

        public static readonly DependencyProperty RibWidthProperty =
            DependencyProperty.Register("RibWidth", typeof(double), typeof(RibbedGlassView), new PropertyMetadata(18d, (s, a) =>
            {
                if (s is RibbedGlassView sender && !Equals(a.NewValue, a.OldValue))
                {
                    var helper = sender.helper;
                    if (helper != null)
                    {
                        helper.ChildVisualWidth = Convert.ToSingle(a.NewValue);
                    }
                    sender.UpdateEffects();
                }
            }));

        public double ChildScale
        {
            get { return (double)GetValue(ChildScaleProperty); }
            set { SetValue(ChildScaleProperty, value); }
        }

        public static readonly DependencyProperty ChildScaleProperty =
            DependencyProperty.Register("ChildScale", typeof(double), typeof(RibbedGlassView), new PropertyMetadata(0.25d, (s, a) =>
            {
                if (s is RibbedGlassView sender && !Equals(a.NewValue, a.OldValue))
                {
                    var helper = sender.helper;
                    if (helper != null)
                    {
                        helper.SourceVisualScale = Convert.ToSingle(a.NewValue);
                    }
                    sender.UpdateEffects();
                }
            }));



        public double BlurAmount
        {
            get { return (double)GetValue(BlurAmountProperty); }
            set { SetValue(BlurAmountProperty, value); }
        }

        public static readonly DependencyProperty BlurAmountProperty =
            DependencyProperty.Register("BlurAmount", typeof(double), typeof(RibbedGlassView), new PropertyMetadata(2d, (s, a) =>
            {
                if (s is RibbedGlassView sender && !Equals(a.NewValue, a.OldValue))
                {
                    var helper = sender.helper;
                    if (helper != null)
                    {
                        helper.BlurAmount = Convert.ToSingle(a.NewValue);
                    }
                    sender.UpdateEffects();
                }
            }));


        public bool IsEffectEnabled
        {
            get { return (bool)GetValue(IsEffectEnabledProperty); }
            set { SetValue(IsEffectEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsEffectEnabledProperty =
            DependencyProperty.Register("IsEffectEnabled", typeof(bool), typeof(RibbedGlassView), new PropertyMetadata(true, (s, a) =>
            {
                if (s is RibbedGlassView sender && !Equals(a.NewValue, a.OldValue))
                {
                    sender.UpdateEffects();
                    if (!(bool)a.NewValue)
                    {
                        var helper = Interlocked.Exchange(ref sender.helper, null);
                        helper?.Dispose();
                    }
                }
            }));


        private void UpdateEffects()
        {
            var flag = IsEffectEnabled
                && IsLoaded
                && ContentBorder != null
                && EffectHost != null
                && Child != null
                && ContentBorder.ActualWidth > 0
                && ContentBorder.ActualHeight > 0
                && ChildScale > 0
                && ChildScale < 1
                && RibWidth > 0
                && BlurAmount >= 0
                && BlurAmount < 200;

            var childVisualWidth = 12f;
            var visualCount = 0;

            if (flag)
            {
                visualCount = (int)Math.Ceiling(ContentBorder!.ActualWidth / childVisualWidth) + 1;
            }

            if (visualCount < 2) flag = false;

            if (flag)
            {
                ContentBorder!.Opacity = 0;

                var helper = EnsureHelper();
                helper.SourceVisual = contentVisual;
                helper.UpdateSize(ContentBorder.ActualWidth, ContentBorder.ActualHeight);
                ElementCompositionPreview.SetElementChildVisual(EffectHost, helper.EffectVisual);
            }
            else
            {
                ClearEffects();
            }
        }

        private void ClearEffects()
        {
            if (helper != null)
            {
                helper.SourceVisual = null;
            }

            if (EffectHost != null)
            {
                ElementCompositionPreview.SetElementChildVisual(EffectHost, null);
            }

            if (ContentBorder != null)
            {
                ContentBorder!.Opacity = 1;
            }
        }

        private void RibbedGlassVisual_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateEffects();
        }

        private void RibbedGlassVisual_Unloaded(object sender, RoutedEventArgs e)
        {
            UpdateEffects();
            if (!IsLoaded)
            {
                var helper = Interlocked.Exchange(ref this.helper, null);
                helper?.Dispose();
            }
        }

        private void ContentBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateEffects();
        }

    }
}
