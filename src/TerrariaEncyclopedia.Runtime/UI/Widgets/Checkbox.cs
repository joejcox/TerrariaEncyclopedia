using System;
using TerrariaEncyclopedia.Runtime.UI.Input;
using TerrariaEncyclopedia.Runtime.UI.Rendering;

namespace TerrariaEncyclopedia.Runtime.UI.Widgets
{
    public static class Checkbox
    {
        public static bool DrawWithLabel(
            IUiRenderer renderer,
            WidgetInput input,
            UiPalette palette,
            RenderRectangle bounds,
            string label,
            bool value)
        {
            if (renderer == null || input == null || palette == null)
            {
                throw new ArgumentNullException(
                    renderer == null ? nameof(renderer) : input == null ? nameof(input) : nameof(palette));
            }

            int boxSize = Math.Min(20, bounds.Height);
            var box = new RenderRectangle(bounds.X, bounds.Y, boxSize, boxSize);
            bool hovered = input.IsMouseOver(bounds);
            renderer.FillRectangle(box, value ? palette.Accent : palette.InputBackground);
            renderer.StrokeRectangle(box, hovered ? palette.AccentText : palette.Border);
            if (value)
            {
                renderer.DrawText("✓", new InputPoint(box.X + 3, box.Y), palette.Text);
            }

            renderer.DrawText(
                label ?? string.Empty,
                new InputPoint(box.X + boxSize + 6, bounds.Y),
                hovered ? palette.AccentText : palette.Text);
            return input.TryConsumeLeftPress(bounds) ? !value : value;
        }
    }
}
