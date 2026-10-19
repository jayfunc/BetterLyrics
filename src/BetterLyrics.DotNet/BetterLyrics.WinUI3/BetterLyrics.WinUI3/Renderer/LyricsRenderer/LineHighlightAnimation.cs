using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using System;

namespace BetterLyrics.WinUI3.Renderer.LyricsRenderer;

internal sealed class LineHighlightAnimation
{
    private const double HoverInDurationSeconds = 0.15;
    private const double HoverOutDurationSeconds = 0.12;
    private const double PressDownDurationSeconds = 0.08;
    private const double PressUpDurationSeconds = 0.30;

    private static readonly Func<double, double, double, double> HoverInInterpolator =
        EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine, EaseMode.Out);

    private static readonly Func<double, double, double, double> HoverOutInterpolator =
        EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine, EaseMode.In);

    private static readonly Func<double, double, double, double> PressDownInterpolator =
        EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Quad, EaseMode.Out);

    private static readonly Func<double, double, double, double> PressUpInterpolator =
        EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Back, EaseMode.Out);

    public LineHighlightAnimation(int lineIndex)
    {
        LineIndex = lineIndex;
        Hover = new ValueTransition<double>(0, HoverInInterpolator, HoverInDurationSeconds);
        Press = new ValueTransition<double>(0, PressDownInterpolator, PressDownDurationSeconds);
    }

    public int LineIndex { get; }

    public ValueTransition<double> Hover { get; }

    public ValueTransition<double> Press { get; }

    private bool IsHoverTarget { get; set; }

    private bool IsPressTarget { get; set; }

    public void SetHovered(bool value)
    {
        if (IsHoverTarget == value) return;

        IsHoverTarget = value;

        if (value)
        {
            Hover.SetInterpolator(HoverInInterpolator);
            Hover.SetDuration(HoverInDurationSeconds);
            Hover.Start(1);
        }
        else
        {
            Hover.SetInterpolator(HoverOutInterpolator);
            Hover.SetDuration(HoverOutDurationSeconds);
            Hover.Start(0);
        }
    }

    public void SetPressed(bool value)
    {
        if (IsPressTarget == value) return;

        IsPressTarget = value;

        if (value)
        {
            Press.SetInterpolator(PressDownInterpolator);
            Press.SetDuration(PressDownDurationSeconds);
            Press.Start(1);
        }
        else
        {
            Press.SetInterpolator(PressUpInterpolator);
            Press.SetDuration(PressUpDurationSeconds);
            Press.Start(0);
        }
    }
}
