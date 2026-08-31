using System;

namespace TerrariaEncyclopedia.Runtime.UI.Widgets
{
    public sealed class StackLayout
    {
        private readonly int originY;

        public StackLayout(int x, int y, int width, int spacing = 4)
        {
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (spacing < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spacing));
            }

            X = x;
            originY = y;
            CurrentY = y;
            Width = width;
            Spacing = spacing;
        }

        public int X { get; }

        public int CurrentY { get; private set; }

        public int Width { get; }

        public int Spacing { get; }

        public int TotalHeight => Math.Max(0, CurrentY - originY - Spacing);

        public RenderRectangle Advance(int height)
        {
            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            var bounds = new RenderRectangle(X, CurrentY, Width, height);
            CurrentY += height + Spacing;
            return bounds;
        }
    }
}
