#if TERRARIA_FNA
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TerrariaEncyclopedia.Runtime.UI.Rendering;

namespace TerrariaEncyclopedia.Runtime.UI.Fna
{
    public delegate int FnaMeasureText(string text);

    public delegate void FnaDrawText(
        SpriteBatch spriteBatch,
        string text,
        Vector2 position,
        Color colour);

    public delegate FnaTextureRegion FnaResolveTerrariaIcon(TerrariaIcon icon);

    public delegate FnaSpriteBatchState FnaCaptureSpriteBatchState();

    public readonly struct FnaTextureRegion
    {
        public FnaTextureRegion(Texture2D texture, Rectangle? sourceRectangle = null)
        {
            Texture = texture;
            SourceRectangle = sourceRectangle;
        }

        public Texture2D Texture { get; }

        public Rectangle? SourceRectangle { get; }
    }

    /// <summary>
    /// Exact arguments used to begin the active SpriteBatch, plus its scissor rectangle.
    /// Terraria's adapter supplies this state explicitly because a deferred SpriteBatch does
    /// not expose it reliably through GraphicsDevice before End flushes the batch.
    /// </summary>
    public sealed class FnaSpriteBatchState
    {
        public FnaSpriteBatchState(
            SpriteSortMode sortMode,
            BlendState blendState,
            SamplerState samplerState,
            DepthStencilState depthStencilState,
            RasterizerState rasterizerState,
            Effect effect,
            Matrix transformMatrix,
            Rectangle scissorRectangle)
        {
            SortMode = sortMode;
            BlendState = blendState;
            SamplerState = samplerState;
            DepthStencilState = depthStencilState;
            RasterizerState = rasterizerState;
            Effect = effect;
            TransformMatrix = transformMatrix;
            ScissorRectangle = scissorRectangle;
        }

        public SpriteSortMode SortMode { get; }

        public BlendState BlendState { get; }

        public SamplerState SamplerState { get; }

        public DepthStencilState DepthStencilState { get; }

        public RasterizerState RasterizerState { get; }

        public Effect Effect { get; }

        public Matrix TransformMatrix { get; }

        public Rectangle ScissorRectangle { get; }
    }

    /// <summary>
    /// FNA drawing backend for an already-begun Terraria UI SpriteBatch.
    /// The caller supplies live font, icon and ambient batch-state adapters; this assembly
    /// owns no game assets.
    /// </summary>
    public sealed class FnaUiRenderBackend : IUiRenderBackend, IDisposable
    {
        private readonly SpriteBatch spriteBatch;
        private readonly GraphicsDevice graphicsDevice;
        private readonly Texture2D pixel;
        private readonly FnaMeasureText measureText;
        private readonly FnaDrawText drawText;
        private readonly FnaResolveTerrariaIcon resolveIcon;
        private readonly FnaCaptureSpriteBatchState captureAmbientState;
        private RenderState currentState;
        private int clipDepth;

        public FnaUiRenderBackend(
            SpriteBatch spriteBatch,
            Texture2D pixel,
            int textLineHeight,
            FnaMeasureText measureText,
            FnaDrawText drawText,
            FnaResolveTerrariaIcon resolveIcon,
            FnaCaptureSpriteBatchState captureAmbientState)
        {
            this.spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
            this.pixel = pixel ?? throw new ArgumentNullException(nameof(pixel));
            this.measureText = measureText ?? throw new ArgumentNullException(nameof(measureText));
            this.drawText = drawText ?? throw new ArgumentNullException(nameof(drawText));
            this.resolveIcon = resolveIcon ?? throw new ArgumentNullException(nameof(resolveIcon));
            this.captureAmbientState = captureAmbientState
                ?? throw new ArgumentNullException(nameof(captureAmbientState));
            graphicsDevice = spriteBatch.GraphicsDevice;
            TextLineHeight = Math.Max(1, textLineHeight);
        }

        public int TextLineHeight { get; }

        public int MeasureText(string text)
        {
            return Math.Max(0, measureText(text ?? string.Empty));
        }

        public IUiRenderState CaptureState()
        {
            if (clipDepth == 0)
            {
                currentState = SnapshotAmbientState();
            }

            return currentState.Copy();
        }

        public void BeginClip(RenderRectangle bounds, IUiRenderState capturedState)
        {
            RenderState state = RequireState(capturedState);
            Rectangle scissor = ToScissorRectangle(bounds, state.TransformMatrix);
            if (state.RasterizerState != null && state.RasterizerState.ScissorTestEnable)
            {
                scissor = Intersect(scissor, state.ScissorRectangle);
            }

            RasterizerState rasterizerState = state.RasterizerState;
            bool createdRasterizer = rasterizerState == null || !rasterizerState.ScissorTestEnable;
            if (createdRasterizer)
            {
                rasterizerState = CreateClippingRasterizer(state.RasterizerState);
            }

            RenderState clipped = state.WithClip(
                rasterizerState,
                scissor,
                state.OwnsRasterizerState || createdRasterizer);
            try
            {
                TransitionTo(clipped);
            }
            catch
            {
                if (createdRasterizer)
                {
                    rasterizerState.Dispose();
                }

                throw;
            }

            currentState = clipped;
            clipDepth++;
        }

        public void RestoreState(IUiRenderState state)
        {
            RenderState captured = RequireState(state);
            RenderState previous = currentState;
            TransitionTo(captured);
            currentState = captured;
            if (clipDepth > 0)
            {
                clipDepth--;
            }

            previous?.DisposeOwnedRasterizerUnlessSharedWith(captured);
        }

        public void FillRectangle(RenderRectangle bounds, RenderColour colour)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            spriteBatch.Draw(pixel, ToRectangle(bounds), ToColour(colour));
        }

        public void StrokeRectangle(RenderRectangle bounds, RenderColour colour, int thickness)
        {
            int horizontal = Math.Min(thickness, Math.Max(0, bounds.Width));
            int vertical = Math.Min(thickness, Math.Max(0, bounds.Height));
            FillRectangle(new RenderRectangle(bounds.X, bounds.Y, bounds.Width, vertical), colour);
            FillRectangle(
                new RenderRectangle(bounds.X, bounds.Y + bounds.Height - vertical, bounds.Width, vertical),
                colour);
            FillRectangle(new RenderRectangle(bounds.X, bounds.Y, horizontal, bounds.Height), colour);
            FillRectangle(
                new RenderRectangle(bounds.X + bounds.Width - horizontal, bounds.Y, horizontal, bounds.Height),
                colour);
        }

        public void DrawText(string text, InputPoint position, RenderColour colour)
        {
            drawText(
                spriteBatch,
                text ?? string.Empty,
                new Vector2(position.X, position.Y),
                ToColour(colour));
        }

        public void DrawTerrariaIcon(
            TerrariaIcon icon,
            RenderRectangle destination,
            RenderColour colour)
        {
            FnaTextureRegion region = resolveIcon(icon);
            if (region.Texture == null || destination.Width <= 0 || destination.Height <= 0)
            {
                return;
            }

            RenderState state = clipDepth == 0 ? SnapshotAmbientState() : currentState.Copy();
            RenderState pointSampled = state.WithSampler(SamplerState.PointClamp);
            TransitionTo(pointSampled);
            try
            {
                spriteBatch.Draw(
                    region.Texture,
                    ToRectangle(destination),
                    region.SourceRectangle,
                    ToColour(colour),
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0f);
            }
            finally
            {
                TransitionTo(state);
                currentState = state;
            }
        }

        public void Dispose()
        {
            currentState?.DisposeOwnedRasterizerUnlessSharedWith(null);
            currentState = null;
            clipDepth = 0;
        }

        internal static RasterizerState CreateClippingRasterizer(RasterizerState ambient)
        {
            if (ambient == null)
            {
                return new RasterizerState { ScissorTestEnable = true };
            }

            return new RasterizerState
            {
                CullMode = ambient.CullMode,
                FillMode = ambient.FillMode,
                MultiSampleAntiAlias = ambient.MultiSampleAntiAlias,
                ScissorTestEnable = true,
                DepthBias = ambient.DepthBias,
                SlopeScaleDepthBias = ambient.SlopeScaleDepthBias
            };
        }

        private void TransitionTo(RenderState state)
        {
            spriteBatch.End();
            graphicsDevice.ScissorRectangle = state.ScissorRectangle;
            BeginBatch(state);
        }

        private void BeginBatch(RenderState state)
        {
            spriteBatch.Begin(
                state.SortMode,
                state.BlendState,
                state.SamplerState,
                state.DepthStencilState,
                state.RasterizerState,
                state.Effect,
                state.TransformMatrix);
        }

        private RenderState SnapshotAmbientState()
        {
            FnaSpriteBatchState state = captureAmbientState();
            if (state == null)
            {
                throw new InvalidOperationException("The FNA ambient SpriteBatch state provider returned null.");
            }

            return new RenderState(state);
        }

        private static RenderState RequireState(IUiRenderState state)
        {
            var renderState = state as RenderState;
            if (renderState == null)
            {
                throw new ArgumentException("The render state was not created by this FNA backend.", nameof(state));
            }

            return renderState;
        }

        private static Rectangle ToRectangle(RenderRectangle value)
        {
            return new Rectangle(value.X, value.Y, Math.Max(0, value.Width), Math.Max(0, value.Height));
        }

        private Rectangle ToScissorRectangle(RenderRectangle value, Matrix transformMatrix)
        {
            Vector2 topLeft = Vector2.Transform(
                new Vector2(value.X, value.Y),
                transformMatrix);
            Vector2 topRight = Vector2.Transform(
                new Vector2(value.X + value.Width, value.Y),
                transformMatrix);
            Vector2 bottomLeft = Vector2.Transform(
                new Vector2(value.X, value.Y + value.Height),
                transformMatrix);
            Vector2 bottomRight = Vector2.Transform(
                new Vector2(value.X + value.Width, value.Y + value.Height),
                transformMatrix);
            int left = (int)Math.Floor(Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X)));
            int top = (int)Math.Floor(Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y)));
            int right = (int)Math.Ceiling(Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X)));
            int bottom = (int)Math.Ceiling(Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y)));
            Viewport viewport = graphicsDevice.Viewport;
            int clippedLeft = Math.Max(viewport.X, left);
            int clippedTop = Math.Max(viewport.Y, top);
            int clippedRight = Math.Min(viewport.X + viewport.Width, right);
            int clippedBottom = Math.Min(viewport.Y + viewport.Height, bottom);

            return new Rectangle(
                clippedLeft,
                clippedTop,
                Math.Max(0, clippedRight - clippedLeft),
                Math.Max(0, clippedBottom - clippedTop));
        }

        private static Rectangle Intersect(Rectangle left, Rectangle right)
        {
            int x = Math.Max(left.X, right.X);
            int y = Math.Max(left.Y, right.Y);
            int rightEdge = Math.Min(left.X + left.Width, right.X + right.Width);
            int bottomEdge = Math.Min(left.Y + left.Height, right.Y + right.Height);
            return new Rectangle(
                x,
                y,
                Math.Max(0, rightEdge - x),
                Math.Max(0, bottomEdge - y));
        }

        private static Color ToColour(RenderColour value)
        {
            return new Color(value.Red, value.Green, value.Blue, value.Alpha);
        }

        private sealed class RenderState : IUiRenderState
        {
            public RenderState(FnaSpriteBatchState state)
            {
                SortMode = state.SortMode;
                BlendState = state.BlendState;
                SamplerState = state.SamplerState;
                DepthStencilState = state.DepthStencilState;
                RasterizerState = state.RasterizerState;
                Effect = state.Effect;
                TransformMatrix = state.TransformMatrix;
                ScissorRectangle = state.ScissorRectangle;
                OwnsRasterizerState = false;
            }

            private RenderState(
                SpriteSortMode sortMode,
                BlendState blendState,
                SamplerState samplerState,
                DepthStencilState depthStencilState,
                RasterizerState rasterizerState,
                Effect effect,
                Matrix transformMatrix,
                Rectangle scissorRectangle,
                bool ownsRasterizerState)
            {
                SortMode = sortMode;
                BlendState = blendState;
                SamplerState = samplerState;
                DepthStencilState = depthStencilState;
                RasterizerState = rasterizerState;
                Effect = effect;
                TransformMatrix = transformMatrix;
                ScissorRectangle = scissorRectangle;
                OwnsRasterizerState = ownsRasterizerState;
            }

            public SpriteSortMode SortMode { get; }

            public BlendState BlendState { get; }

            public SamplerState SamplerState { get; }

            public DepthStencilState DepthStencilState { get; }

            public RasterizerState RasterizerState { get; }

            public Effect Effect { get; }

            public Matrix TransformMatrix { get; }

            public Rectangle ScissorRectangle { get; }

            public bool OwnsRasterizerState { get; }

            public RenderState Copy()
            {
                return new RenderState(
                    SortMode,
                    BlendState,
                    SamplerState,
                    DepthStencilState,
                    RasterizerState,
                    Effect,
                    TransformMatrix,
                    ScissorRectangle,
                    OwnsRasterizerState);
            }

            public RenderState WithClip(
                RasterizerState rasterizerState,
                Rectangle scissorRectangle,
                bool ownsRasterizerState)
            {
                return new RenderState(
                    SortMode,
                    BlendState,
                    SamplerState,
                    DepthStencilState,
                    rasterizerState,
                    Effect,
                    TransformMatrix,
                    scissorRectangle,
                    ownsRasterizerState);
            }

            public RenderState WithSampler(SamplerState samplerState)
            {
                return new RenderState(
                    SortMode,
                    BlendState,
                    samplerState,
                    DepthStencilState,
                    RasterizerState,
                    Effect,
                    TransformMatrix,
                    ScissorRectangle,
                    OwnsRasterizerState);
            }

            public void DisposeOwnedRasterizerUnlessSharedWith(RenderState replacement)
            {
                if (OwnsRasterizerState
                    && RasterizerState != null
                    && !ReferenceEquals(RasterizerState, replacement?.RasterizerState))
                {
                    RasterizerState.Dispose();
                }
            }
        }
    }
}
#endif
