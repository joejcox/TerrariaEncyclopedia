using System;
using System.Collections.Generic;
using TerrariaEncyclopedia.Runtime.UI.Input;
using TerrariaEncyclopedia.Runtime.UI.Rendering;

namespace TerrariaEncyclopedia.Runtime.UI.Tooltips
{
    // Adapted from TerrariaModder v0.4.0 Tooltip and ItemTooltip under the MIT licence.
    public sealed class TooltipService
    {
        private const int MaximumWidth = 300;
        private const int Padding = 8;
        private readonly IItemTooltipSource itemSource;
        private string title;
        private string text;
        private int? itemType;
        private int itemPrefix;
        private int itemStack;

        public TooltipService(IItemTooltipSource itemSource = null)
        {
            this.itemSource = itemSource;
        }

        public void BeginFrame()
        {
            title = null;
            text = null;
            itemType = null;
            itemPrefix = 0;
            itemStack = 1;
        }

        public void Set(string value)
        {
            title = null;
            text = value;
            itemType = null;
        }

        public void Set(string tooltipTitle, string description)
        {
            title = tooltipTitle;
            text = description;
            itemType = null;
        }

        public void SetItem(int type, int prefix = 0, int stack = 1)
        {
            if (type <= 0)
            {
                return;
            }

            itemType = type;
            itemPrefix = prefix;
            itemStack = Math.Max(1, stack);
            title = null;
            text = null;
        }

        public void DrawDeferred(
            IUiRenderer renderer,
            WidgetInput input,
            RenderRectangle viewport,
            UiPalette palette)
        {
            IReadOnlyList<TooltipLine> lines = BuildPendingLines(palette);
            if (lines.Count == 0)
            {
                return;
            }

            int maximumContentWidth = Math.Max(0, Math.Min(MaximumWidth, viewport.Width) - Padding * 2);
            IReadOnlyList<TooltipLine> wrapped = WrapLines(lines, maximumContentWidth, renderer.MeasureText);
            int contentWidth = MeasureMaximumWidth(wrapped, renderer.MeasureText);
            int contentHeight = wrapped.Count * renderer.TextLineHeight;
            TooltipLayout layout = TooltipLayout.Place(
                input.MousePosition,
                viewport,
                contentWidth,
                contentHeight,
                Padding);
            DrawBox(renderer, palette, layout.Bounds);
            using (renderer.PushClip(layout.Bounds))
            {
                DrawLines(renderer, wrapped, layout.Bounds.X + Padding, layout.Bounds.Y + Padding);
            }
        }

        public int DrawItemAt(
            IUiRenderer renderer,
            UiPalette palette,
            int type,
            RenderRectangle bounds,
            int prefix = 0,
            int stack = 1)
        {
            if (itemSource == null || type <= 0 || bounds.Width <= 0)
            {
                return 0;
            }

            IReadOnlyList<TooltipLine> lines = itemSource.Build(type, prefix, Math.Max(1, stack))
                ?? Array.Empty<TooltipLine>();
            IReadOnlyList<TooltipLine> wrapped = WrapLines(
                lines,
                Math.Max(0, bounds.Width - Padding * 2),
                renderer.MeasureText);
            int height = wrapped.Count * renderer.TextLineHeight + Padding * 2;
            var actualBounds = new RenderRectangle(bounds.X, bounds.Y, bounds.Width, height);
            DrawBox(renderer, palette, actualBounds);
            DrawLines(renderer, wrapped, bounds.X + Padding, bounds.Y + Padding);
            return height;
        }

        private IReadOnlyList<TooltipLine> BuildPendingLines(UiPalette palette)
        {
            if (itemType.HasValue && itemSource != null)
            {
                return itemSource.Build(itemType.Value, itemPrefix, itemStack)
                    ?? Array.Empty<TooltipLine>();
            }

            var lines = new List<TooltipLine>();
            if (!string.IsNullOrEmpty(title))
            {
                lines.Add(new TooltipLine(title, palette.TitleText));
            }

            if (!string.IsNullOrEmpty(text))
            {
                lines.Add(new TooltipLine(text, palette.Text));
            }

            return lines;
        }

        private static IReadOnlyList<TooltipLine> WrapLines(
            IReadOnlyList<TooltipLine> lines,
            int maxWidth,
            Func<string, int> measure)
        {
            var result = new List<TooltipLine>();
            foreach (TooltipLine line in lines)
            {
                foreach (string wrapped in TextLayout.Wrap(line.Text, maxWidth, measure))
                {
                    result.Add(new TooltipLine(wrapped, line.Colour));
                }
            }

            return result;
        }

        private static int MeasureMaximumWidth(
            IReadOnlyList<TooltipLine> lines,
            Func<string, int> measure)
        {
            int width = 0;
            foreach (TooltipLine line in lines)
            {
                width = Math.Max(width, measure(line.Text));
            }

            return width;
        }

        private static void DrawBox(IUiRenderer renderer, UiPalette palette, RenderRectangle bounds)
        {
            renderer.FillRectangle(bounds, palette.TooltipBackground);
            renderer.StrokeRectangle(bounds, palette.Border);
        }

        private static void DrawLines(
            IUiRenderer renderer,
            IReadOnlyList<TooltipLine> lines,
            int x,
            int y)
        {
            for (int index = 0; index < lines.Count; index++)
            {
                TooltipLine line = lines[index];
                renderer.DrawText(
                    line.Text,
                    new InputPoint(x, y + index * renderer.TextLineHeight),
                    line.Colour);
            }
        }
    }
}
