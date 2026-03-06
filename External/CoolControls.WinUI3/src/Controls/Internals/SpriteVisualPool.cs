using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoolControls.WinUI3.Controls.Internals
{
    internal class SpriteVisualPool : DependencyObject, IDisposable
    {
        private static object staticLocker = new object();
        private static SpriteVisualPool? instance;

        public static SpriteVisualPool Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (staticLocker)
                    {
                        if (instance == null)
                        {
                            instance = new SpriteVisualPool();
                        }
                    }
                }

                return instance;
            }
        }


        private bool disposedValue;
        private readonly Compositor compositor;
        private readonly int maximumRetained;

        private Queue<SpriteVisual> items;
        private System.Timers.Timer clearTimer;

        internal SpriteVisualPool(int maximumRetained = 200)
        {
            VerifyAccess();

            this.compositor = CompositionTarget.GetCompositorForCurrentThread();

            items = new Queue<SpriteVisual>();

            clearTimer = new System.Timers.Timer(TimeSpan.FromMinutes(1));
            clearTimer.AutoReset = false;
            clearTimer.Elapsed += ClearTimer_Elapsed;
            this.maximumRetained = maximumRetained;
        }

        private void ClearTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (disposedValue) return;
                Clear();
            });
        }

        public SpriteVisual GetOrCreate()
        {
            ThrowIfDisposed();
            VerifyAccess();

            clearTimer.Stop();

            if (!items.TryDequeue(out var result))
            {
                result = compositor.CreateSpriteVisual();
            }
            return result;
        }

        public void Return(SpriteVisual spriteVisual)
        {
            ThrowIfDisposed();
            VerifyAccess();

            Reset(spriteVisual);

            if (items.Count < maximumRetained)
            {
                items.Enqueue(spriteVisual);
            }
            else
            {
                spriteVisual.Dispose();
            }

            clearTimer.Stop();
            clearTimer.Start();
        }

        public void Clear()
        {
            ThrowIfDisposed();
            VerifyAccess();

            clearTimer.Stop();

            var items = this.items;
            this.items = new Queue<SpriteVisual>();

            while (items.TryDequeue(out var visual))
            {
                visual.Dispose();
            }
        }

        private void Reset(SpriteVisual spriteVisual)
        {
            spriteVisual.StopAnimation("AnchorPoint");
            spriteVisual.StopAnimation("CenterPoint");
            spriteVisual.StopAnimation("IsVisible");
            spriteVisual.StopAnimation("Offset");
            spriteVisual.StopAnimation("Opacity");
            spriteVisual.StopAnimation("Orientation");
            spriteVisual.StopAnimation("RelativeOffsetAdjustment");
            spriteVisual.StopAnimation("RelativeSizeAdjustment");
            spriteVisual.StopAnimation("RotationAngle");
            spriteVisual.StopAnimation("RotationAngleInDegrees");
            spriteVisual.StopAnimation("RotationAxis");
            spriteVisual.StopAnimation("Scale");
            spriteVisual.StopAnimation("Size");
            spriteVisual.StopAnimation("TransformMatrix");

            spriteVisual.AnchorPoint = Vector2.Zero;
            spriteVisual.BackfaceVisibility = CompositionBackfaceVisibility.Inherit;
            spriteVisual.BorderMode = CompositionBorderMode.Inherit;
            spriteVisual.Brush = null;
            spriteVisual.CenterPoint = Vector3.Zero;
            spriteVisual.Children.RemoveAll();
            spriteVisual.Clip = null;
            spriteVisual.Comment = "";
            spriteVisual.CompositeMode = CompositionCompositeMode.Inherit;
            spriteVisual.ImplicitAnimations = null;
            spriteVisual.IsHitTestVisible = true;
            spriteVisual.IsPixelSnappingEnabled = false;
            spriteVisual.IsVisible = true;
            spriteVisual.Offset = Vector3.Zero;
            spriteVisual.Opacity = 1;
            spriteVisual.Orientation = Quaternion.Identity;
            spriteVisual.ParentForTransform = null;
            spriteVisual.RelativeOffsetAdjustment = Vector3.Zero;
            spriteVisual.RelativeSizeAdjustment = Vector2.Zero;
            spriteVisual.RotationAngle = 0;
            spriteVisual.RotationAngleInDegrees = 0;
            spriteVisual.RotationAxis = new Vector3(0, 0, 1);
            spriteVisual.Scale = Vector3.One;
            spriteVisual.Shadow = null;
            spriteVisual.Size = Vector2.Zero;
            spriteVisual.TransformMatrix = Matrix4x4.Identity;
        }

        private void VerifyAccess()
        {
            if (DispatcherQueue == null || !DispatcherQueue.HasThreadAccess)
            {
                throw new InvalidOperationException("Cross-thread operation not valid");
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(nameof(SpriteVisualPool));
            }
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                VerifyAccess();

                clearTimer.Stop();
                clearTimer.Dispose();
                clearTimer = null!;

                var items = this.items;
                this.items = new Queue<SpriteVisual>();

                while (items.TryDequeue(out var visual))
                {
                    visual.Dispose();
                }

                disposedValue = true;
            }
        }

    }
}
