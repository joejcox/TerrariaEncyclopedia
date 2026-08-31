using System;
using TerrariaEncyclopedia.Runtime.UI.Rendering;
using Xunit;

namespace TerrariaEncyclopedia.Runtime.Tests.UI
{
    public sealed class UiRendererTests
    {
        [Fact]
        public void PushClipRestoresCapturedStateWhenDrawingThrows()
        {
            var backend = new RecordingRenderBackend();
            var renderer = new UiRenderer(backend);

            Assert.Throws<InvalidOperationException>((Action)(() =>
            {
                using (renderer.PushClip(new RenderRectangle(10, 20, 100, 80)))
                {
                    throw new InvalidOperationException("draw failed");
                }
            }));

            Assert.Equal(new[] { 1 }, backend.RestoredStates);
        }

        [Fact]
        public void NestedClipsIntersectAndRestoreInReverseOrder()
        {
            var backend = new RecordingRenderBackend();
            var renderer = new UiRenderer(backend);

            using (renderer.PushClip(new RenderRectangle(10, 10, 100, 100)))
            {
                using (renderer.PushClip(new RenderRectangle(50, 0, 100, 80)))
                {
                }
            }

            Assert.Collection(
                backend.Clips,
                first => AssertRectangle(first, 10, 10, 100, 100),
                second => AssertRectangle(second, 50, 10, 60, 70));
            Assert.Equal(new[] { 2, 1 }, backend.RestoredStates);
        }

        [Fact]
        public void FailedClipTransitionRestoresTheCapturedState()
        {
            var backend = new RecordingRenderBackend { ThrowOnBeginClip = true };
            var renderer = new UiRenderer(backend);

            Assert.Throws<InvalidOperationException>((Action)(() =>
                renderer.PushClip(new RenderRectangle(0, 0, 10, 10))));

            Assert.Equal(new[] { 1 }, backend.RestoredStates);
        }

        [Fact]
        public void FeatureRendererUpgradeIsCheckedAndCentralised()
        {
            var renderer = new UiRenderer(new RecordingRenderBackend());
            IFeatureRenderer featureRenderer = renderer;

            Assert.Same(renderer, featureRenderer.RequireUiRenderer());
            Assert.Throws<ArgumentNullException>(
                (Action)(() => UiRendererExtensions.RequireUiRenderer(null)));
        }

        private static void AssertRectangle(
            RenderRectangle actual,
            int x,
            int y,
            int width,
            int height)
        {
            Assert.Equal(x, actual.X);
            Assert.Equal(y, actual.Y);
            Assert.Equal(width, actual.Width);
            Assert.Equal(height, actual.Height);
        }
    }
}
