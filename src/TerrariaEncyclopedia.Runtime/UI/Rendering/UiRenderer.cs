using System;
using System.Collections.Generic;

namespace TerrariaEncyclopedia.Runtime.UI.Rendering
{
    public sealed class UiRenderer : IUiRenderer
    {
        private readonly IUiRenderBackend backend;
        private readonly Stack<ClipFrame> clips = new Stack<ClipFrame>();

        public UiRenderer(IUiRenderBackend backend)
        {
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public int TextLineHeight => backend.TextLineHeight;

        public int MeasureText(string text)
        {
            return backend.MeasureText(text ?? string.Empty);
        }

        public IDisposable PushClip(RenderRectangle bounds)
        {
            RenderRectangle effectiveBounds = clips.Count == 0
                ? bounds
                : UiGeometry.Intersect(clips.Peek().Bounds, bounds);
            IUiRenderState state = backend.CaptureState()
                ?? throw new InvalidOperationException("The UI render backend returned no state to restore.");
            var frame = new ClipFrame(effectiveBounds, state);

            try
            {
                backend.BeginClip(effectiveBounds, state);
                clips.Push(frame);
                return new ClipScope(this, frame);
            }
            catch
            {
                backend.RestoreState(state);
                throw;
            }
        }

        public void FillRectangle(RenderRectangle bounds, RenderColour colour)
        {
            backend.FillRectangle(bounds, colour);
        }

        public void StrokeRectangle(RenderRectangle bounds, RenderColour colour, int thickness = 1)
        {
            if (thickness <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(thickness));
            }

            backend.StrokeRectangle(bounds, colour, thickness);
        }

        public void DrawText(string text, InputPoint position, RenderColour colour)
        {
            backend.DrawText(text ?? string.Empty, position, colour);
        }

        public void DrawTerrariaIcon(
            TerrariaIcon icon,
            RenderRectangle destination,
            RenderColour colour)
        {
            backend.DrawTerrariaIcon(icon, destination, colour);
        }

        private void PopClip(ClipFrame frame)
        {
            if (clips.Count == 0 || !ReferenceEquals(clips.Peek(), frame))
            {
                throw new InvalidOperationException("UI clip scopes must be disposed in reverse order.");
            }

            clips.Pop();
            backend.RestoreState(frame.State);
        }

        private sealed class ClipFrame
        {
            public ClipFrame(RenderRectangle bounds, IUiRenderState state)
            {
                Bounds = bounds;
                State = state;
            }

            public RenderRectangle Bounds { get; }

            public IUiRenderState State { get; }
        }

        private sealed class ClipScope : IDisposable
        {
            private UiRenderer owner;
            private readonly ClipFrame frame;

            public ClipScope(UiRenderer owner, ClipFrame frame)
            {
                this.owner = owner;
                this.frame = frame;
            }

            public void Dispose()
            {
                UiRenderer currentOwner = owner;
                if (currentOwner == null)
                {
                    return;
                }

                currentOwner.PopClip(frame);
                owner = null;
            }
        }
    }
}
