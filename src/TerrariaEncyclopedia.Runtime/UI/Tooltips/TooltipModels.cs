using System;
using System.Collections.Generic;

namespace TerrariaEncyclopedia.Runtime.UI.Tooltips
{
    public readonly struct TooltipLine
    {
        public TooltipLine(string text, RenderColour colour)
        {
            Text = text ?? string.Empty;
            Colour = colour;
        }

        public string Text { get; }

        public RenderColour Colour { get; }
    }

    public interface IItemTooltipSource
    {
        IReadOnlyList<TooltipLine> Build(int itemType, int prefix, int stack);
    }

    public readonly struct TooltipLayout
    {
        public TooltipLayout(RenderRectangle bounds, int contentWidth, int contentHeight)
        {
            Bounds = bounds;
            ContentWidth = contentWidth;
            ContentHeight = contentHeight;
        }

        public RenderRectangle Bounds { get; }

        public int ContentWidth { get; }

        public int ContentHeight { get; }

        public static TooltipLayout Place(
            InputPoint anchor,
            RenderRectangle viewport,
            int contentWidth,
            int contentHeight,
            int padding = 8,
            int gap = 16)
        {
            if (contentWidth < 0 || contentHeight < 0 || padding < 0 || gap < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            int width = Math.Min(viewport.Width, contentWidth + padding * 2);
            int height = Math.Min(viewport.Height, contentHeight + padding * 2);
            int x = anchor.X + gap;
            int y = anchor.Y + gap;

            if (x + width > viewport.X + viewport.Width)
            {
                x = anchor.X - width - 4;
            }

            if (y + height > viewport.Y + viewport.Height)
            {
                y = anchor.Y - height - 4;
            }

            RenderRectangle bounds = UiGeometry.Clamp(
                new RenderRectangle(x, y, width, height),
                viewport);
            return new TooltipLayout(bounds, Math.Max(0, width - padding * 2), Math.Max(0, height - padding * 2));
        }
    }
}
