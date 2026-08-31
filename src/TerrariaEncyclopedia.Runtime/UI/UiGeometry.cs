using System;

namespace TerrariaEncyclopedia.Runtime.UI
{
    public static class UiGeometry
    {
        public static bool Contains(RenderRectangle bounds, InputPoint point)
        {
            return bounds.Width > 0
                && bounds.Height > 0
                && point.X >= bounds.X
                && point.Y >= bounds.Y
                && point.X < bounds.X + bounds.Width
                && point.Y < bounds.Y + bounds.Height;
        }

        public static bool Intersects(RenderRectangle left, RenderRectangle right)
        {
            return left.Width > 0
                && left.Height > 0
                && right.Width > 0
                && right.Height > 0
                && left.X < right.X + right.Width
                && right.X < left.X + left.Width
                && left.Y < right.Y + right.Height
                && right.Y < left.Y + left.Height;
        }

        public static RenderRectangle Intersect(RenderRectangle left, RenderRectangle right)
        {
            int x = Math.Max(left.X, right.X);
            int y = Math.Max(left.Y, right.Y);
            int rightEdge = Math.Min(left.X + Math.Max(0, left.Width), right.X + Math.Max(0, right.Width));
            int bottomEdge = Math.Min(left.Y + Math.Max(0, left.Height), right.Y + Math.Max(0, right.Height));

            return new RenderRectangle(x, y, Math.Max(0, rightEdge - x), Math.Max(0, bottomEdge - y));
        }

        public static RenderRectangle Clamp(RenderRectangle bounds, RenderRectangle container)
        {
            int width = Math.Min(Math.Max(0, bounds.Width), Math.Max(0, container.Width));
            int height = Math.Min(Math.Max(0, bounds.Height), Math.Max(0, container.Height));
            int maxX = container.X + container.Width - width;
            int maxY = container.Y + container.Height - height;
            int x = Math.Max(container.X, Math.Min(bounds.X, maxX));
            int y = Math.Max(container.Y, Math.Min(bounds.Y, maxY));

            return new RenderRectangle(x, y, width, height);
        }
    }
}
