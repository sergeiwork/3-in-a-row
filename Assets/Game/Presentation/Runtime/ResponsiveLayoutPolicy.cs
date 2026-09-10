using System;

namespace ThreeInARow.Presentation
{
    public enum ResponsiveLayoutMode
    {
        Compact,
        Portrait,
        Wide
    }

    public readonly struct ResponsiveLayoutMetrics
    {
        public ResponsiveLayoutMetrics(
            ResponsiveLayoutMode mode,
            float maxContentWidth,
            float horizontalPadding,
            float verticalPadding,
            float boardViewportShare)
        {
            Mode = mode;
            MaxContentWidth = maxContentWidth;
            HorizontalPadding = horizontalPadding;
            VerticalPadding = verticalPadding;
            BoardViewportShare = boardViewportShare;
        }

        public ResponsiveLayoutMode Mode { get; }
        public float MaxContentWidth { get; }
        public float HorizontalPadding { get; }
        public float VerticalPadding { get; }
        public float BoardViewportShare { get; }
    }

    public static class ResponsiveLayoutPolicy
    {
        public const float MaximumBoardSide = 720f;
        public const float MinimumComfortableBoardSide = 392f;

        public static ResponsiveLayoutMetrics Measure(float width, float height)
        {
            width = Math.Max(1f, width);
            height = Math.Max(1f, height);
            var landscape = width >= height * 1.1f;
            var compact = !landscape && (height < 1080f || height / width < 1.58f);

            if (landscape)
                return new ResponsiveLayoutMetrics(ResponsiveLayoutMode.Wide, 860f, 28f, 16f, 0.58f);
            if (compact)
                return new ResponsiveLayoutMetrics(ResponsiveLayoutMode.Compact, 720f, 16f, 12f, 0.50f);
            return new ResponsiveLayoutMetrics(ResponsiveLayoutMode.Portrait, 760f, 24f, 18f, 0.54f);
        }

        public static float BoardSide(float availableWidth, float viewportHeight)
        {
            var metrics = Measure(availableWidth, viewportHeight);
            var heightBudget = Math.Max(MinimumComfortableBoardSide,
                viewportHeight * metrics.BoardViewportShare);
            return Math.Max(0f, Math.Min(MaximumBoardSide, Math.Min(availableWidth, heightBudget)));
        }
    }
}
