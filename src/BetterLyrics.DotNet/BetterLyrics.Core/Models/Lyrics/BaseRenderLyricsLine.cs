using System.Numerics;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Models.Lyrics;

public class BaseRenderLyricsLine : BaseRenderLyrics
{
    public BaseRenderLyricsLine(LyricsLine lyricsLine) : base(lyricsLine)
    {
        AngleTransition = new ValueTransition<double>(
            0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            AnimationDuration
        );
        BlurAmountTransition = new ValueTransition<double>(
            0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            AnimationDuration
        );
        TertiaryOpacityTransition = new ValueTransition<double>(
            0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            AnimationDuration
        );
        PlayedPrimaryOpacityTransition = new ValueTransition<double>(
            0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            AnimationDuration
        );
        UnplayedPrimaryOpacityTransition = new ValueTransition<double>(
            0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            AnimationDuration
        );
        SecondaryOpacityTransition = new ValueTransition<double>(
            0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            AnimationDuration
        );
        ScaleTransition = new ValueTransition<double>(
            1.0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            AnimationDuration
        );
        PrimaryXOffsetTransition = new ValueTransition<double>(
            0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            AnimationDuration
        );
        SecondaryXOffsetTransition = new ValueTransition<double>(
            0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            AnimationDuration
        );
        TertiaryXOffsetTransition = new ValueTransition<double>(
            0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            AnimationDuration
        );
        OffsetTransition = new ValueTransition<double>(
            0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
            AnimationDuration
        );
        PlayedFillColorTransition = new ValueTransition<AppColor>(
            Colors.Transparent,
            defaultTotalDuration: 0.3f,
            interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        UnplayedFillColorTransition = new ValueTransition<AppColor>(
            Colors.Transparent,
            defaultTotalDuration: 0.3f,
            interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        PlayedStrokeColorTransition = new ValueTransition<AppColor>(
            Colors.Transparent,
            defaultTotalDuration: 0.3f,
            interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
        );
        UnplayedStrokeColorTransition = new ValueTransition<AppColor>(
            Colors.Transparent,
            defaultTotalDuration: 0.3f,
            interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
        );

        StartMs = lyricsLine.StartMs;
        EndMs = lyricsLine.EndMs;
        TertiaryText = lyricsLine.TertiaryText;
        PrimaryText = lyricsLine.PrimaryText;
        SecondaryText = lyricsLine.SecondaryText;
        IsPrimaryHasRealSyllableInfo = lyricsLine.IsPrimaryHasRealSyllableInfo;
        AgentId = lyricsLine.AgentId;
    }

    public double AnimationDuration { get; set; } = 0.3;

    public List<BaseRenderLyricsChar> PrimaryRenderChars { get; set; } = [];
    public List<BaseRenderLyricsSyllable> PrimaryRenderSyllables { get; set; } = [];

    public ValueTransition<double> AngleTransition { get; set; }
    public ValueTransition<double> BlurAmountTransition { get; set; }
    public ValueTransition<double> ScaleTransition { get; set; }

    public ValueTransition<double> PlayedPrimaryOpacityTransition { get; set; }
    public ValueTransition<double> UnplayedPrimaryOpacityTransition { get; set; }
    public ValueTransition<double> SecondaryOpacityTransition { get; set; }
    public ValueTransition<double> TertiaryOpacityTransition { get; set; }

    public ValueTransition<double> PrimaryXOffsetTransition { get; set; }
    public ValueTransition<double> SecondaryXOffsetTransition { get; set; }
    public ValueTransition<double> TertiaryXOffsetTransition { get; set; }

    public ValueTransition<double> OffsetTransition { get; set; }

    public ValueTransition<AppColor> PlayedFillColorTransition { get; set; }
    public ValueTransition<AppColor> UnplayedFillColorTransition { get; set; }
    public ValueTransition<AppColor> PlayedStrokeColorTransition { get; set; }
    public ValueTransition<AppColor> UnplayedStrokeColorTransition { get; set; }

    /// <summary>
    ///     原文坐标（相对于坐标原点）
    /// </summary>
    public Vector2 PrimaryPosition { get; set; }

    /// <summary>
    ///     译文坐标（相对于坐标原点）
    /// </summary>
    public Vector2 SecondaryPosition { get; set; }

    /// <summary>
    ///     注音坐标（相对于坐标原点）
    /// </summary>
    public Vector2 TertiaryPosition { get; set; }

    /// <summary>
    ///     顶部坐标（相对于坐标原点）
    /// </summary>
    public Vector2 TopLeftPosition { get; set; }

    /// <summary>
    ///     中心坐标（相对于坐标原点）
    /// </summary>
    public Vector2 CenterPosition { get; set; }

    /// <summary>
    ///     底部坐标（相对于坐标原点）
    /// </summary>
    public Vector2 BottomRightPosition { get; set; }

    public string PrimaryText { get; set; } = "";
    public string SecondaryText { get; set; } = "";
    public string TertiaryText { get; set; } = "";

    public AppRect? PrimaryTextLayoutBounds { get; set; }
    public AppRect? SecondaryTextLayoutBounds { get; set; }
    public AppRect? TertiaryTextLayoutBounds { get; set; }

    /// <summary>
    ///     轨道索引 (0 = 主轨道, 1 = 第一副轨道, etc.)
    ///     用于布局计算时的堆叠逻辑
    /// </summary>
    public int LaneIndex { get; set; } = 0;

    public bool IsPrimaryHasRealSyllableInfo { get; set; }

    public string AgentId { get; set; }

    public TextAlignmentType? HorizontalAlignmentType { get; set; }

    public double? PrimaryLineHeight => PrimaryRenderChars.FirstOrDefault()?.LayoutRect.Height;

    public void Update(TimeSpan elapsedTime)
    {
        AngleTransition.Update(elapsedTime);
        ScaleTransition.Update(elapsedTime);
        BlurAmountTransition.Update(elapsedTime);

        PlayedPrimaryOpacityTransition.Update(elapsedTime);
        UnplayedPrimaryOpacityTransition.Update(elapsedTime);
        SecondaryOpacityTransition.Update(elapsedTime);
        TertiaryOpacityTransition.Update(elapsedTime);

        PrimaryXOffsetTransition.Update(elapsedTime);
        SecondaryXOffsetTransition.Update(elapsedTime);
        TertiaryXOffsetTransition.Update(elapsedTime);
        OffsetTransition.Update(elapsedTime);

        PlayedFillColorTransition.Update(elapsedTime);
        UnplayedFillColorTransition.Update(elapsedTime);
        PlayedStrokeColorTransition.Update(elapsedTime);
        UnplayedStrokeColorTransition.Update(elapsedTime);
    }
}