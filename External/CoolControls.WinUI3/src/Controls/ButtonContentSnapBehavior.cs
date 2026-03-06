using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core.AnimationMetrics;

namespace CoolControls.WinUI3.Controls
{
    public class ButtonContentSnapBehavior : Behavior<ButtonBase>
    {
        private const double DurationSeconds = 0.3;

        private bool attached;
        private long paddingChangedEventToken;
        private long contentChangedEventToken;
        private VisualStateGroup? visualStateGroup;
        private ContentPresenter? contentPresenter;
        private Visual? contentVisual;

        private Compositor compositor;
        private CompositionPropertySet propSet;
        private Vector3KeyFrameAnimation translationAnimation1;
        private Vector3KeyFrameAnimation translationAnimation2;

        public ButtonContentSnapBehavior()
        {
            compositor = CompositionTarget.GetCompositorForCurrentThread();

            propSet = compositor.CreatePropertySet();
            propSet.InsertVector3("Offset", Vector3.Zero);

            translationAnimation1 = compositor.CreateVector3KeyFrameAnimation();
            translationAnimation1.InsertKeyFrame(1, Vector3.Zero);
            translationAnimation1.Duration = TimeSpan.FromSeconds(DurationSeconds);
            translationAnimation1.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;

            translationAnimation2 = compositor.CreateVector3KeyFrameAnimation();
            translationAnimation2.InsertExpressionKeyFrame(1, "propSet.Offset");
            translationAnimation2.Duration = TimeSpan.FromSeconds(DurationSeconds);
            translationAnimation2.SetReferenceParameter("propSet", propSet);
            translationAnimation2.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;
        }

        public ButtonContentSnapType SnapType
        {
            get { return (ButtonContentSnapType)GetValue(SnapTypeProperty); }
            set { SetValue(SnapTypeProperty, value); }
        }

        public static readonly DependencyProperty SnapTypeProperty =
            DependencyProperty.Register("SnapType", typeof(ButtonContentSnapType), typeof(ButtonContentSnapBehavior), new PropertyMetadata(ButtonContentSnapType.None, (s, a) =>
            {
                if (s is ButtonContentSnapBehavior sender && !Equals(a.NewValue, a.OldValue))
                {
                    sender.UpdateSnapType();
                }
            }));

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            TryLoadContent((ButtonBase)sender);
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            UnloadContent((ButtonBase)sender);
        }

        private void AssociatedObject_LayoutUpdated(object? sender, object e)
        {
            TryLoadContent(AssociatedObject);
        }

        private void TryLoadContent(ButtonBase? button)
        {
            if (button == null) return;

            button.LayoutUpdated -= AssociatedObject_LayoutUpdated;

            if (button.IsLoaded)
            {
                if (VisualTreeHelper.GetChildrenCount(button) > 0)
                {
                    LoadContent(button);
                }
                else
                {
                    button.LayoutUpdated += AssociatedObject_LayoutUpdated;
                }
            }
            else
            {
                UnloadContent(button);
            }
        }

        private void LoadContent(ButtonBase button)
        {
            if (attached) return;

            paddingChangedEventToken = button.RegisterPropertyChangedCallback(Control.PaddingProperty, OnPaddingPropertyChanged);
            visualStateGroup = VisualStateManager.GetVisualStateGroups((FrameworkElement)VisualTreeHelper.GetChild(button, 0)).FirstOrDefault(c => c.Name == "CommonStates");

            if (visualStateGroup != null)
            {
                visualStateGroup.CurrentStateChanging += VisualStateGroup_CurrentStateChanging;
            }

            contentPresenter = FindChild<ContentPresenter>(button);

            if (contentPresenter != null)
            {
                contentChangedEventToken = contentPresenter.RegisterPropertyChangedCallback(ContentPresenter.ContentProperty, OnContentChanged);
                var actualContent = VisualTreeHelper.GetChild(contentPresenter, 0) as UIElement;

                if (actualContent != null)
                {
                    contentVisual = ElementCompositionPreview.GetElementVisual(actualContent);
                    contentVisual.IsPixelSnappingEnabled = true;
                    ElementCompositionPreview.SetIsTranslationEnabled(actualContent, true);
                }
            }

            attached = true;
            UpdateSnapType();
        }

        private void UnloadContent(ButtonBase button)
        {
            if (!attached) return;

            attached = false;

            button.LayoutUpdated += AssociatedObject_LayoutUpdated;

            button.UnregisterPropertyChangedCallback(Control.PaddingProperty, paddingChangedEventToken);
            paddingChangedEventToken = 0;

            if (visualStateGroup != null)
            {
                visualStateGroup.CurrentStateChanging -= VisualStateGroup_CurrentStateChanging;
            }
            visualStateGroup = null;

            if (contentPresenter != null)
            {
                contentPresenter.UnregisterPropertyChangedCallback(ContentPresenter.ContentProperty, contentChangedEventToken);
                contentChangedEventToken = 0;
                contentPresenter = null;
            }

            if (contentVisual != null)
            {
                contentVisual.StopAnimation("Translation");
                contentVisual = null;
            }

            propSet.InsertVector3("Offset", Vector3.Zero);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;

            TryLoadContent(AssociatedObject);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;

            UnloadContent(AssociatedObject);
        }

        private void OnPaddingPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            UpdateSnapType();
        }

        private void OnContentChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (attached && contentPresenter != null)
            {
                if (contentVisual != null)
                {
                    contentVisual.StopAnimation("Translation");
                    contentVisual = null;
                }

                var actualContent = VisualTreeHelper.GetChild(contentPresenter, 0) as UIElement;

                if (actualContent != null)
                {
                    contentVisual = ElementCompositionPreview.GetElementVisual(actualContent);
                    contentVisual.IsPixelSnappingEnabled = true;
                    ElementCompositionPreview.SetIsTranslationEnabled(actualContent, true);
                }
            }
        }

        private void VisualStateGroup_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            if (!attached || contentVisual == null) return;

            if (e.NewState?.Name == "PointerOver" || e.NewState?.Name == "Pressed")
            {
                contentVisual.StartAnimation("Translation", translationAnimation1);
            }
            else
            {
                contentVisual.StartAnimation("Translation", translationAnimation2);
            }
        }


        private void UpdateSnapType()
        {
            var button = AssociatedObject;
            if (button == null) return;

            if (attached)
            {
                var hover = false;

                if (visualStateGroup != null)
                {
                    hover = visualStateGroup.CurrentState?.Name == "PointerOver"
                        || visualStateGroup.CurrentState?.Name == "Pressed";
                }

                //var padding = button.Padding;
                var padding = new Thickness(4);

                var offset = SnapType switch
                {
                    ButtonContentSnapType.Left => new Vector3((float)(-padding.Left), 0, 0),
                    ButtonContentSnapType.Top => new Vector3(0, (float)(-padding.Top), 0),
                    ButtonContentSnapType.Right => new Vector3((float)(padding.Right), 0, 0),
                    ButtonContentSnapType.Bottom => new Vector3(0, (float)(padding.Bottom), 0),
                    _ => Vector3.Zero
                };

                propSet.InsertVector3("Offset", offset);

                if (contentVisual != null)
                {
                    contentVisual.StopAnimation("Translation");

                    if (hover)
                    {
                        contentVisual.Properties.InsertVector3("Translation", Vector3.Zero);
                    }
                    else
                    {
                        contentVisual.Properties.InsertVector3("Translation", offset);
                    }
                }
            }
            else
            {

            }
        }

        private static T? FindChild<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T value) return value;

                var value2 = FindChild<T>(child);
                if (value2 != null) return value2;
            }

            return null;
        }
    }

    public enum ButtonContentSnapType
    {
        None,
        Left,
        Top,
        Right,
        Bottom
    }
}
