using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace CoolControls.WinUI3.Controls.Internals
{
    internal class RibbedGlassVisualHelper : IDisposable
    {
        private static volatile int instanceCount = 0;
        private static object globalLocker = new object();

        private bool disposedValue;
        private DisposableCollector disposables = new DisposableCollector();

        private Compositor compositor;
        private float blurAmount = 2;

        private Visual? sourceVisual;

        private CompositionPropertySet propSet;
        private ExpressionAnimation sourceVisualScalePropertyBind;
        private ExpressionAnimation visualSizePropertyBind;
        private ExpressionAnimation childVisualSizePropertyBind;
        private ExpressionAnimation visualSizeBind;
        private ExpressionAnimation childVisualSizeBind;
        private ExpressionAnimation glassMaskSizeBind;
        private ExpressionAnimation sourceVisualScaleBind;
        private ExpressionAnimation childVisualOffsetBind;
        private ExpressionAnimation childVisualBrushTransformBind;
        private ExpressionAnimation blurAmountBind;

        private CompositionVisualSurface sourceVisualSurface;
        private CompositionSurfaceBrush sourceVisualSurfaceBrush;
        private ContainerVisual rootChildVisual;
        private ContainerVisual childVisualsContainer;

        private SpriteVisual glassMaskVisual;
        private SpriteVisual glassMaskSurfaceVisual;
        private CompositionVisualSurface glassMaskSurface;
        private CompositionSurfaceBrush glassMaskBrush;

        private SpriteVisual scaledContentSurfaceVisual;
        private CompositionEffectBrush scaledContentEffectBrush;
        private CompositionEffectBrush scaledContentEffectBlurredBrush;
        private CompositionVisualSurface scaledContentSurface;

        private Stack<ChildVisualEntry> usedChildVisual;

        public RibbedGlassVisualHelper(Compositor compositor)
        {
            Interlocked.Increment(ref instanceCount);

            this.compositor = compositor;

            EnsurePropertiesAndBindings();
            EnsureVisualsContainer();
            EnsureSourceBrush();
            EnsureScaledContentSurface();
            EnsureGlassMaskVisual();

            rootChildVisual.Children.InsertAtTop(glassMaskVisual);

            usedChildVisual = new Stack<ChildVisualEntry>();
        }

        public Visual? SourceVisual
        {
            get => sourceVisual;
            set
            {
                if (sourceVisual != value)
                {
                    if (sourceVisual != null)
                    {
                        propSet.StopAnimation("SourceVisualSize");
                        visualSizePropertyBind.ClearParameter("visual");
                    }

                    sourceVisualSurface.SourceVisual = value;
                    sourceVisual = value;

                    if (sourceVisual != null)
                    {
                        visualSizePropertyBind.SetReferenceParameter("visual", sourceVisual);
                        propSet.StartAnimation("SourceVisualSize", visualSizePropertyBind);
                    }
                    var size = sourceVisual?.Size ?? Vector2.Zero;
                    UpdateSize(size.X, size.Y);
                }
            }
        }

        public Visual EffectVisual => rootChildVisual;

        public float ChildVisualWidth
        {
            get
            {
                if (propSet.TryGetScalar(nameof(ChildVisualWidth), out var value) == CompositionGetValueStatus.Succeeded)
                {
                    return value;
                }
                return 0;
            }
            set
            {
                propSet.InsertScalar(nameof(ChildVisualWidth), value);
            }
        }

        public float SourceVisualScale
        {
            get
            {
                if (propSet.TryGetScalar(nameof(SourceVisualScale), out var value) == CompositionGetValueStatus.Succeeded)
                {
                    return value;
                }
                return 0;
            }
            set
            {
                propSet.InsertScalar(nameof(SourceVisualScale), value);
            }
        }

        public float BlurAmount
        {
            get => blurAmount;
            set
            {
                if (blurAmount != value)
                {
                    blurAmount = value;
                    propSet.InsertScalar(nameof(BlurAmount), value);
                    UpdateScaledContentBlurState();
                }
            }
        }

        #region Ensure Resource

        [MemberNotNull(
            nameof(propSet),
            nameof(sourceVisualScalePropertyBind),
            nameof(visualSizePropertyBind),
            nameof(childVisualSizePropertyBind),
            nameof(visualSizeBind),
            nameof(childVisualSizeBind),
            nameof(glassMaskSizeBind),
            nameof(sourceVisualScaleBind),
            nameof(childVisualOffsetBind),
            nameof(childVisualBrushTransformBind),
            nameof(blurAmountBind))]
        private void EnsurePropertiesAndBindings()
        {
            propSet = compositor.CreatePropertySet()
                .TraceDisposable(disposables);
            propSet.InsertScalar(nameof(SourceVisualScale), 0.25f);
            propSet.InsertScalar(nameof(ChildVisualWidth), 18f);
            propSet.InsertScalar(nameof(BlurAmount), blurAmount);

            propSet.InsertMatrix3x2("ContentTransform", Matrix3x2.Identity);
            propSet.InsertVector2("SourceVisualSize", Vector2.Zero);
            propSet.InsertVector2("ChildVisualSize", Vector2.Zero);
            propSet.InsertScalar("VisualCount", 0f);

            visualSizePropertyBind = compositor.CreateExpressionAnimation("visual.Size")
                .TraceDisposable(disposables);

            sourceVisualScalePropertyBind = compositor.CreateExpressionAnimation("Matrix3x2.CreateFromScale(Vector2(this.Target.SourceVisualScale, 1f))")
                .TraceDisposable(disposables);
            propSet.StartAnimation("ContentTransform", sourceVisualScalePropertyBind);

            childVisualSizePropertyBind = compositor.CreateExpressionAnimation("Vector2(this.Target.ChildVisualWidth + 1f, this.Target.SourceVisualSize.Y)")
                .TraceDisposable(disposables);
            propSet.StartAnimation("ChildVisualSize", childVisualSizePropertyBind);

            visualSizeBind = compositor.CreateExpressionAnimation("prop.SourceVisualSize")
                .TraceDisposable(disposables);
            visualSizeBind.SetReferenceParameter("prop", propSet);

            childVisualSizeBind = compositor.CreateExpressionAnimation("prop.ChildVisualSize")
                .TraceDisposable(disposables);
            childVisualSizeBind.SetReferenceParameter("prop", propSet);

            glassMaskSizeBind = compositor.CreateExpressionAnimation("Vector2(prop.ChildVisualWidth, 1)")
                .TraceDisposable(disposables);
            glassMaskSizeBind.SetReferenceParameter("prop", propSet);

            sourceVisualScaleBind = compositor.CreateExpressionAnimation("prop.ContentTransform")
                .TraceDisposable(disposables);
            sourceVisualScaleBind.SetReferenceParameter("prop", propSet);

            childVisualOffsetBind = compositor.CreateExpressionAnimation("Vector3(this.Target._Index * prop.ChildVisualWidth, 0f, 0f)")
                .TraceDisposable(disposables);
            childVisualOffsetBind.SetReferenceParameter("prop", propSet);

            childVisualBrushTransformBind = compositor.CreateExpressionAnimation("Matrix3x2.CreateFromTranslation(Vector2((prop.ChildVisualWidth / (1 - prop.SourceVisualScale) / prop.VisualCount - prop.ChildVisualWidth * prop.SourceVisualScale) * this.Target._Index, 0))")
                .TraceDisposable(disposables);
            childVisualBrushTransformBind.SetReferenceParameter("prop", propSet);

            blurAmountBind = compositor.CreateExpressionAnimation("prop.BlurAmount")
                .TraceDisposable(disposables);
            blurAmountBind.SetReferenceParameter("prop", propSet);
        }

        [MemberNotNull(
            nameof(sourceVisualSurface),
            nameof(sourceVisualSurfaceBrush))]
        private void EnsureSourceBrush()
        {
            sourceVisualSurface = compositor.CreateVisualSurface()
                .TraceDisposable(disposables);

            sourceVisualSurface.StartAnimation("SourceSize", visualSizeBind);

            sourceVisualSurfaceBrush = compositor.CreateSurfaceBrush(sourceVisualSurface)
                .TraceDisposable(disposables);
            sourceVisualSurfaceBrush.Stretch = CompositionStretch.None;
            sourceVisualSurfaceBrush.HorizontalAlignmentRatio = 0;
            sourceVisualSurfaceBrush.VerticalAlignmentRatio = 0;
        }

        [MemberNotNull(
            nameof(scaledContentSurfaceVisual),
            nameof(scaledContentEffectBrush),
            nameof(scaledContentEffectBlurredBrush),
            nameof(scaledContentSurface))]
        private void EnsureScaledContentSurface()
        {
            scaledContentSurfaceVisual = compositor.CreateSpriteVisual().TraceDisposable(disposables);
            scaledContentSurfaceVisual.StartAnimation("Size", visualSizeBind);

            var transformEffect = new Transform2DEffect()
            {
                Name = "TransformEffect",
                Source = new CompositionEffectSourceParameter("source"),
                TransformMatrix = Matrix3x2.Identity
            }.TraceDisposable(disposables);

            scaledContentEffectBrush = compositor
                .CreateEffectFactory(transformEffect, new[] { "TransformEffect.TransformMatrix" }).TraceDisposable(disposables)
                .CreateBrush().TraceDisposable(disposables);

            scaledContentEffectBrush.SetSourceParameter("source", sourceVisualSurfaceBrush);
            scaledContentEffectBrush.Properties.StartAnimation("TransformEffect.TransformMatrix", sourceVisualScaleBind);

            var blurredEffect = new GaussianBlurEffect()
            {
                Name = "BlurEffect",
                BlurAmount = 2,
                BorderMode = EffectBorderMode.Soft,
                Source = new Transform2DEffect()
                {
                    Name = "TransformEffect",
                    Source = new CompositionEffectSourceParameter("source"),
                    TransformMatrix = Matrix3x2.Identity
                }
            }.TraceDisposable(disposables);


            scaledContentEffectBlurredBrush = compositor
                .CreateEffectFactory(blurredEffect, new[] { "TransformEffect.TransformMatrix", "BlurEffect.BlurAmount" }).TraceDisposable(disposables)
                .CreateBrush().TraceDisposable(disposables);

            scaledContentEffectBlurredBrush.SetSourceParameter("source", sourceVisualSurfaceBrush);
            scaledContentEffectBlurredBrush.Properties.StartAnimation("TransformEffect.TransformMatrix", sourceVisualScaleBind);
            scaledContentEffectBlurredBrush.Properties.StartAnimation("BlurEffect.BlurAmount", blurAmountBind);

            scaledContentSurface = compositor.CreateVisualSurface().TraceDisposable(disposables);
            scaledContentSurface.SourceVisual = scaledContentSurfaceVisual;
            scaledContentSurface.StartAnimation("SourceSize", visualSizeBind);

            UpdateScaledContentBlurState();
        }

        [MemberNotNull(
            nameof(rootChildVisual),
            nameof(childVisualsContainer))]
        private void EnsureVisualsContainer()
        {
            rootChildVisual = compositor.CreateContainerVisual()
                .TraceDisposable(disposables);
            rootChildVisual.Clip = compositor.CreateInsetClip();
            rootChildVisual.StartAnimation("Size", visualSizeBind);

            childVisualsContainer = compositor.CreateContainerVisual()
                .TraceDisposable(disposables);
            childVisualsContainer.RelativeSizeAdjustment = Vector2.One;

            rootChildVisual.Children.InsertAtTop(childVisualsContainer);
        }

        [MemberNotNull(
            nameof(glassMaskSurfaceVisual),
            nameof(glassMaskSurface),
            nameof(glassMaskBrush),
            nameof(glassMaskVisual))]
        private void EnsureGlassMaskVisual()
        {
            glassMaskSurfaceVisual = compositor.CreateSpriteVisual()
                .TraceDisposable(disposables);
            glassMaskSurfaceVisual.StartAnimation("Size", glassMaskSizeBind);

            var gradientBrush = compositor.CreateLinearGradientBrush();
            gradientBrush.StartPoint = new Vector2(0, 0);
            gradientBrush.EndPoint = new Vector2(1, 0);
            gradientBrush.MappingMode = CompositionMappingMode.Relative;
            gradientBrush.ExtendMode = CompositionGradientExtendMode.Clamp;
            gradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(0f, Color.FromArgb(20, 0, 0, 0)));
            gradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(0.25f, Color.FromArgb(0, 0, 0, 0)));
            gradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(0.75f, Color.FromArgb(0, 0, 0, 0)));
            gradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(1f, Color.FromArgb(20, 0, 0, 0)));

            glassMaskSurfaceVisual.Brush = gradientBrush;

            glassMaskSurface = compositor.CreateVisualSurface()
                .TraceDisposable(disposables);
            glassMaskSurface.SourceVisual = glassMaskSurfaceVisual;
            glassMaskSurface.StartAnimation("SourceSize", glassMaskSizeBind);

            glassMaskBrush = compositor.CreateSurfaceBrush(glassMaskSurface)
                .TraceDisposable(disposables);
            glassMaskBrush.HorizontalAlignmentRatio = 0;
            glassMaskBrush.VerticalAlignmentRatio = 0;
            glassMaskBrush.Stretch = CompositionStretch.None;

            var maskTileEffect = new BorderEffect()
            {
                ExtendX = Microsoft.Graphics.Canvas.CanvasEdgeBehavior.Wrap,
                ExtendY = Microsoft.Graphics.Canvas.CanvasEdgeBehavior.Clamp,
                Source = new CompositionEffectSourceParameter("source"),
            }.TraceDisposable(disposables);

            var maskTiledBrush = compositor
                .CreateEffectFactory(maskTileEffect).TraceDisposable(disposables)
                .CreateBrush().TraceDisposable(disposables);

            maskTiledBrush.SetSourceParameter("source", glassMaskBrush);

            glassMaskVisual = compositor.CreateSpriteVisual()
                .TraceDisposable(disposables);
            glassMaskVisual.StartAnimation("Size", visualSizeBind);
            glassMaskVisual.Brush = maskTiledBrush;
        }

        #endregion Ensure Resource


        private void UpdateScaledContentBlurState()
        {
            if (blurAmount > 0)
            {
                scaledContentSurfaceVisual.Brush = scaledContentEffectBlurredBrush;
            }
            else
            {
                scaledContentSurfaceVisual.Brush = scaledContentEffectBrush;
            }
        }


        internal void UpdateSize(double width, double height)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(RibbedGlassVisualHelper));

            int visualCount = 0;

            var childVisualWidth = ChildVisualWidth;
            var sourceVisualScale = SourceVisualScale;

            if (width > 0 && height > 0 && sourceVisual != null)
            {
                visualCount = (int)Math.Ceiling(width / ChildVisualWidth) + 1;
            }

            propSet.InsertScalar("VisualCount", visualCount);

            while (usedChildVisual.Count > visualCount)
            {
                var entry = usedChildVisual.Pop();

                childVisualsContainer.Children.Remove(entry.Visual);
                entry.Visual.StopAnimation("Size");
                entry.Visual.StopAnimation("Offset");
                entry.Brush.StopAnimation("TransformMatrix");
                entry.Visual.Properties.InsertScalar("_Index", -1);
                entry.Brush.Properties.InsertScalar("_Index", -1);
                entry.Brush.Surface = null;

                entry.Dispose();
            }

            while (usedChildVisual.Count < visualCount)
            {
                var entry = CreateChildVisualEntry(compositor);
                entry.Visual.Properties.InsertScalar("_Index", usedChildVisual.Count);
                entry.Brush.Properties.InsertScalar("_Index", usedChildVisual.Count);

                usedChildVisual.Push(entry);

                entry.Visual.StartAnimation("Size", childVisualSizeBind);
                entry.Visual.StartAnimation("Offset", childVisualOffsetBind);
                entry.Brush.StartAnimation("TransformMatrix", childVisualBrushTransformBind);
                childVisualsContainer.Children.InsertAtTop(entry.Visual);

                entry.Brush.Surface = scaledContentSurface;
            }
        }


        private static ChildVisualEntry CreateChildVisualEntry(Compositor compositor)
        {
            var visual = SpriteVisualPool.Instance.GetOrCreate();

            var brush = compositor.CreateSurfaceBrush();
            brush.Stretch = CompositionStretch.None;
            brush.HorizontalAlignmentRatio = 0;
            brush.VerticalAlignmentRatio = 0;

            visual.Brush = brush;

            return new ChildVisualEntry(visual, brush);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                lock (globalLocker)
                {
                    var _instanceCount = Interlocked.Decrement(ref instanceCount);

                    sourceVisual = null;
                    UpdateSize(0, 0);

                    disposedValue = true;

                    rootChildVisual?.Children.RemoveAll();
                    childVisualsContainer?.Children.RemoveAll();

                    disposables.Dispose();
                    disposables = null!;
                }

            }
        }

        ~RibbedGlassVisualHelper()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private class ChildVisualEntry : IDisposable
        {
            private bool disposedValue;

            public ChildVisualEntry(SpriteVisual visual, CompositionSurfaceBrush brush)
            {
                Visual = visual;
                Brush = brush;
            }

            public SpriteVisual Visual { get; private set; }

            public CompositionSurfaceBrush Brush { get; private set; }

            public void Dispose()
            {
                if (!disposedValue)
                {
                    SpriteVisualPool.Instance.Return(Visual);
                    Visual = null!;

                    Brush?.Dispose();
                    Brush = null!;

                    disposedValue = true;
                }
            }
        }
    }
}
