namespace TerrariaEncyclopedia.Runtime.UI.Rendering
{
    using System;

    public interface IUiRenderer : IFeatureRenderer
    {
        int TextLineHeight { get; }

        int MeasureText(string text);

        void FillRectangle(RenderRectangle bounds, RenderColour colour);

        void StrokeRectangle(RenderRectangle bounds, RenderColour colour, int thickness = 1);
    }

    public interface IUiRenderState
    {
    }

    public interface IUiRenderBackend
    {
        int TextLineHeight { get; }

        int MeasureText(string text);

        IUiRenderState CaptureState();

        void BeginClip(RenderRectangle bounds, IUiRenderState capturedState);

        void RestoreState(IUiRenderState state);

        void FillRectangle(RenderRectangle bounds, RenderColour colour);

        void StrokeRectangle(RenderRectangle bounds, RenderColour colour, int thickness);

        void DrawText(string text, InputPoint position, RenderColour colour);

        void DrawTerrariaIcon(
            TerrariaIcon icon,
            RenderRectangle destination,
            RenderColour colour);
    }

    public static class UiRendererExtensions
    {
        public static IUiRenderer RequireUiRenderer(this IFeatureRenderer renderer)
        {
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            var uiRenderer = renderer as IUiRenderer;
            if (uiRenderer == null)
            {
                throw new InvalidOperationException(
                    "The active feature renderer does not provide the Encyclopedia UI surface.");
            }

            return uiRenderer;
        }
    }
}
