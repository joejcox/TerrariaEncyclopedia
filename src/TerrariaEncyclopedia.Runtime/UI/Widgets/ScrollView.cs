using System;
using TerrariaEncyclopedia.Runtime.UI.Input;
using TerrariaEncyclopedia.Runtime.UI.Rendering;

namespace TerrariaEncyclopedia.Runtime.UI.Widgets
{
    // Adapted from TerrariaModder v0.4.0 ScrollView under the MIT licence.
    public sealed class ScrollView
    {
        public const int ScrollbarWidth = 8;
        public const int ScrollStep = 40;
        private const int ScrollbarInset = 2;
        private const int ScrollbarGap = 4;
        private const int MinimumThumbHeight = 20;

        private RenderRectangle viewport;
        private int contentHeight;
        private bool draggingThumb;
        private int dragStartY;
        private int dragStartOffset;

        public int ScrollOffset { get; private set; }

        public int ContentHeight => contentHeight;

        public int ViewHeight => viewport.Height;

        public int MaxScroll => Math.Max(0, contentHeight - viewport.Height);

        public bool NeedsScrolling => viewport.Width > 0 && viewport.Height > 0 && MaxScroll > 0;

        public int ContentX => viewport.X;

        public int ContentY => viewport.Y - ScrollOffset;

        public int ContentWidth => Math.Max(
            0,
            viewport.Width - (NeedsScrolling ? ScrollbarWidth + ScrollbarGap : 0));

        public RenderRectangle Viewport => viewport;

        public void Begin(RenderRectangle bounds, int totalContentHeight)
        {
            if (bounds.Width < 0 || bounds.Height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bounds));
            }

            viewport = bounds;
            contentHeight = Math.Max(0, totalContentHeight);
            ScrollOffset = ClampOffset(ScrollOffset);
        }

        public void Update(WidgetInput input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (!NeedsScrolling)
            {
                draggingThumb = false;
                ScrollOffset = 0;
                return;
            }

            RenderRectangle thumb = GetThumbBounds();
            if (!draggingThumb && input.TryConsumeLeftPress(thumb))
            {
                draggingThumb = true;
                dragStartY = input.MousePosition.Y;
                dragStartOffset = ScrollOffset;
            }
            else if (!draggingThumb
                && input.TryConsumeLeftPress(GetScrollbarBounds()))
            {
                int relativeY = input.MousePosition.Y - viewport.Y;
                ScrollOffset = ClampOffset((int)((double)relativeY / viewport.Height * MaxScroll));
                draggingThumb = true;
                dragStartY = input.MousePosition.Y;
                dragStartOffset = ScrollOffset;
            }

            if (draggingThumb)
            {
                if (input.IsLeftDown)
                {
                    int trackTravel = viewport.Height - thumb.Height;
                    if (trackTravel > 0)
                    {
                        int delta = input.MousePosition.Y - dragStartY;
                        ScrollOffset = ClampOffset(
                            dragStartOffset + (int)((double)delta / trackTravel * MaxScroll));
                    }
                }
                else
                {
                    draggingThumb = false;
                }
            }

            if (input.TryConsumeScroll(viewport, out int wheelDelta))
            {
                ScrollOffset = ClampOffset(
                    ScrollOffset + (wheelDelta > 0 ? -ScrollStep : ScrollStep));
            }
        }

        public IDisposable PushClip(IUiRenderer renderer)
        {
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            return renderer.PushClip(viewport);
        }

        public bool IsVisible(int itemY, int itemHeight)
        {
            if (itemHeight <= 0)
            {
                return false;
            }

            return itemY < ScrollOffset + viewport.Height
                && itemY + itemHeight > ScrollOffset;
        }

        public void DrawScrollbar(IUiRenderer renderer, WidgetInput input, UiPalette palette)
        {
            if (!NeedsScrolling)
            {
                return;
            }

            RenderRectangle track = GetScrollbarBounds();
            RenderRectangle thumb = GetThumbBounds();
            renderer.FillRectangle(track, palette.ScrollTrack);
            renderer.FillRectangle(
                thumb,
                draggingThumb || input.IsMouseOver(thumb)
                    ? palette.ScrollThumbHover
                    : palette.ScrollThumb);
        }

        public RenderRectangle GetScrollbarBounds()
        {
            return new RenderRectangle(
                viewport.X + viewport.Width - ScrollbarWidth - ScrollbarInset,
                viewport.Y,
                ScrollbarWidth,
                viewport.Height);
        }

        public RenderRectangle GetThumbBounds()
        {
            if (!NeedsScrolling)
            {
                return GetScrollbarBounds();
            }

            int thumbHeight = Math.Min(
                viewport.Height,
                Math.Max(MinimumThumbHeight, (int)((double)viewport.Height * viewport.Height / contentHeight)));
            int travel = viewport.Height - thumbHeight;
            int thumbY = viewport.Y + (MaxScroll == 0 ? 0 : (int)((double)travel * ScrollOffset / MaxScroll));
            RenderRectangle track = GetScrollbarBounds();
            return new RenderRectangle(track.X, thumbY, track.Width, thumbHeight);
        }

        public void ResetScroll()
        {
            ScrollOffset = 0;
            draggingThumb = false;
        }

        private int ClampOffset(int offset)
        {
            return Math.Max(0, Math.Min(offset, MaxScroll));
        }
    }
}
