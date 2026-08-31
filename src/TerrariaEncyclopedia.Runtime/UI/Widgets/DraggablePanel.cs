using System;
using TerrariaEncyclopedia.Runtime.UI.Input;
using TerrariaEncyclopedia.Runtime.UI.Rendering;

namespace TerrariaEncyclopedia.Runtime.UI.Widgets
{
    // Adapted from TerrariaModder v0.4.0 DraggablePanel under the MIT licence.
    public sealed class DraggablePanel
    {
        private InputPoint? requestedPosition;
        private bool dragging;
        private InputPoint dragOffset;

        public DraggablePanel(string id, string title, int width, int height)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A panel id is required.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("A panel title is required.", nameof(title));
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(width <= 0 ? nameof(width) : nameof(height));
            }

            Id = id;
            Title = title;
            Width = width;
            Height = height;
        }

        public string Id { get; }

        public string Title { get; set; }

        public int Width { get; }

        public int Height { get; }

        public int HeaderHeight { get; set; } = 35;

        public int Padding { get; set; } = 8;

        public bool IsOpen { get; private set; }

        public bool ClipContent { get; set; } = true;

        public PanelLayout Layout { get; private set; }

        public RenderRectangle ContentBounds => Layout.ContentBounds;

        public Action OnClose { get; set; }

        public void Open()
        {
            requestedPosition = null;
            IsOpen = true;
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            dragging = false;
            OnClose?.Invoke();
        }

        public void Update(WidgetInput input, RenderRectangle viewport)
        {
            if (!IsOpen)
            {
                return;
            }

            Layout = PanelLayout.Calculate(
                viewport,
                Width,
                Height,
                HeaderHeight,
                Padding,
                requestedPosition);

            if (input.WasKeyPressed("Escape"))
            {
                Close();
                return;
            }

            if (input.TryConsumeLeftPress(Layout.CloseButtonBounds))
            {
                Close();
                return;
            }

            RenderRectangle dragBounds = new RenderRectangle(
                Layout.HeaderBounds.X,
                Layout.HeaderBounds.Y,
                Math.Max(0, Layout.CloseButtonBounds.X - Layout.HeaderBounds.X),
                Layout.HeaderBounds.Height);
            if (!dragging && input.TryConsumeLeftPress(dragBounds))
            {
                dragging = true;
                dragOffset = new InputPoint(
                    input.MousePosition.X - Layout.Bounds.X,
                    input.MousePosition.Y - Layout.Bounds.Y);
            }

            if (dragging)
            {
                if (input.IsLeftDown)
                {
                    requestedPosition = new InputPoint(
                        input.MousePosition.X - dragOffset.X,
                        input.MousePosition.Y - dragOffset.Y);
                    Layout = PanelLayout.Calculate(
                        viewport,
                        Width,
                        Height,
                        HeaderHeight,
                        Padding,
                        requestedPosition);
                    requestedPosition = new InputPoint(Layout.Bounds.X, Layout.Bounds.Y);
                }
                else
                {
                    dragging = false;
                }
            }
        }

        public void DrawChrome(IUiRenderer renderer, WidgetInput input, UiPalette palette)
        {
            if (!IsOpen)
            {
                return;
            }

            renderer.FillRectangle(Layout.Bounds, palette.PanelBackground);
            renderer.StrokeRectangle(Layout.Bounds, palette.Border);
            renderer.FillRectangle(Layout.HeaderBounds, palette.HeaderBackground);
            renderer.DrawText(
                Title,
                new InputPoint(Layout.HeaderBounds.X + 10, Layout.HeaderBounds.Y + 9),
                palette.TitleText);
            renderer.FillRectangle(
                Layout.CloseButtonBounds,
                input.IsMouseOver(Layout.CloseButtonBounds)
                    ? palette.CloseButtonHover
                    : palette.CloseButton);
            renderer.DrawText(
                "X",
                new InputPoint(Layout.CloseButtonBounds.X + 10, Layout.CloseButtonBounds.Y + 6),
                palette.Text);
        }

        public IDisposable PushContentClip(IUiRenderer renderer)
        {
            return ClipContent ? renderer.PushClip(Layout.ContentBounds) : null;
        }
    }
}
