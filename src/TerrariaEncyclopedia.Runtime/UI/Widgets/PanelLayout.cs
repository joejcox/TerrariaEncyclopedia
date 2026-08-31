using System;

namespace TerrariaEncyclopedia.Runtime.UI.Widgets
{
    public readonly struct PanelLayout
    {
        private PanelLayout(
            RenderRectangle bounds,
            RenderRectangle headerBounds,
            RenderRectangle contentBounds,
            RenderRectangle closeButtonBounds)
        {
            Bounds = bounds;
            HeaderBounds = headerBounds;
            ContentBounds = contentBounds;
            CloseButtonBounds = closeButtonBounds;
        }

        public RenderRectangle Bounds { get; }

        public RenderRectangle HeaderBounds { get; }

        public RenderRectangle ContentBounds { get; }

        public RenderRectangle CloseButtonBounds { get; }

        public static PanelLayout Calculate(
            RenderRectangle viewport,
            int width,
            int height,
            int headerHeight,
            int padding,
            InputPoint? requestedPosition = null)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (headerHeight < 0 || padding < 0)
            {
                throw new ArgumentOutOfRangeException(
                    headerHeight < 0 ? nameof(headerHeight) : nameof(padding));
            }

            int x = requestedPosition?.X ?? viewport.X + (viewport.Width - width) / 2;
            int y = requestedPosition?.Y ?? viewport.Y + (viewport.Height - height) / 2;
            RenderRectangle bounds = UiGeometry.Clamp(
                new RenderRectangle(x, y, width, height),
                viewport);
            int actualHeaderHeight = Math.Min(headerHeight, bounds.Height);
            int contentX = bounds.X + Math.Min(padding, bounds.Width);
            int contentY = bounds.Y + actualHeaderHeight;
            int contentWidth = Math.Max(0, bounds.Width - padding * 2);
            int contentHeight = Math.Max(0, bounds.Height - actualHeaderHeight - padding);
            int closeSize = Math.Min(30, actualHeaderHeight);

            return new PanelLayout(
                bounds,
                new RenderRectangle(bounds.X, bounds.Y, bounds.Width, actualHeaderHeight),
                new RenderRectangle(contentX, contentY, contentWidth, contentHeight),
                new RenderRectangle(
                    bounds.X + Math.Max(0, bounds.Width - closeSize - 5),
                    bounds.Y + Math.Max(0, (actualHeaderHeight - closeSize) / 2),
                    closeSize,
                    closeSize));
        }
    }
}
