using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Threading;

namespace BetterLyrics.Avalonia.Controls;

// Ported from https://github.com/cnbluefire
public class AutoScrollView : ContentControl
{
    private Panel? _movingPanel;
    private ContentPresenter? _contentPresenter;
    private Rectangle? _duplicate;
    private TranslateTransform? _translateTransform;

    private Animation? _currentAnimation;
    private CancellationTokenSource? _animationCts;

    // Avalonia StyledProperties replace WinUI DependencyProperties
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<AutoScrollView, double>(nameof(Spacing), 20d);

    public static readonly StyledProperty<bool> IsPlayingProperty =
        AvaloniaProperty.Register<AutoScrollView, bool>(nameof(IsPlaying), true);

    public static readonly StyledProperty<double> ScrollingPixelsPerSecondProperty =
        AvaloniaProperty.Register<AutoScrollView, double>(nameof(ScrollingPixelsPerSecond), 30d);

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public bool IsPlaying
    {
        get => GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public double ScrollingPixelsPerSecond
    {
        get => GetValue(ScrollingPixelsPerSecondProperty);
        set => SetValue(ScrollingPixelsPerSecondProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SpacingProperty ||
            change.Property == IsPlayingProperty ||
            change.Property == ScrollingPixelsPerSecondProperty)
        {
            Dispatcher.UIThread.Post(UpdateAnimationState);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _movingPanel = e.NameScope.Find<Panel>("PART_MovingPanel");
        _contentPresenter = e.NameScope.Find<ContentPresenter>("PART_Content");
        _duplicate = e.NameScope.Find<Rectangle>("PART_Duplicate");

        if (_movingPanel != null)
        {
            _translateTransform = new TranslateTransform();
            _movingPanel.RenderTransform = _translateTransform;
        }

        if (_contentPresenter != null)
        {
            _contentPresenter.SizeChanged += (s, ev) => Dispatcher.UIThread.Post(UpdateAnimationState);
        }

        Dispatcher.UIThread.Post(UpdateAnimationState);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        Dispatcher.UIThread.Post(UpdateAnimationState);
    }

    private void UpdateAnimationState()
    {
        if (_movingPanel == null || _contentPresenter == null || _duplicate == null || _translateTransform == null)
            return;

        // Stop any running animations
        _animationCts?.Cancel();
        _animationCts = new CancellationTokenSource();

        var childWidth = _contentPresenter.Bounds.Width;
        var childHeight = _contentPresenter.Bounds.Height;
        var rootWidth = Bounds.Width;

        // Only scroll if the child is wider than the container and IsPlaying is true
        if (childWidth > rootWidth && IsPlaying && ScrollingPixelsPerSecond > 0)
        {
            var distance = childWidth + Spacing;

            // Setup the duplicate rectangle using a VisualBrush of the original content
            _duplicate.IsVisible = true;
            _duplicate.Width = childWidth;
            _duplicate.Height = childHeight;
            _duplicate.Margin = new Thickness(distance, 0, 0, 0);

            _duplicate.Fill = new VisualBrush(_contentPresenter)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            var duration = TimeSpan.FromSeconds(distance / ScrollingPixelsPerSecond);

            _currentAnimation = new Animation
            {
                Duration = duration,
                IterationCount = IterationCount.Infinite,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters = { new Setter(TranslateTransform.XProperty, 0.0) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters = { new Setter(TranslateTransform.XProperty, -distance) }
                    }
                }
            };

            // Start the Avalonia animation
            _currentAnimation.RunAsync(_movingPanel, _animationCts.Token);
        }
        else
        {
            _duplicate.IsVisible = false;
            _translateTransform.X = 0;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        base.MeasureOverride(new Size(double.PositiveInfinity, availableSize.Height));

        if (_contentPresenter != null)
        {
            double desiredHeight = _contentPresenter.DesiredSize.Height;

            double desiredWidth = double.IsInfinity(availableSize.Width)
                ? _contentPresenter.DesiredSize.Width
                : availableSize.Width;

            return new Size(desiredWidth, desiredHeight);
        }

        return base.MeasureOverride(availableSize);
    }
}