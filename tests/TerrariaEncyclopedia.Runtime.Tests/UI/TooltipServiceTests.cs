using System.Collections.Generic;
using TerrariaEncyclopedia.Runtime.UI.Input;
using TerrariaEncyclopedia.Runtime.UI.Rendering;
using TerrariaEncyclopedia.Runtime.UI.Tooltips;
using Xunit;

namespace TerrariaEncyclopedia.Runtime.Tests.UI
{
    public sealed class TooltipServiceTests
    {
        [Fact]
        public void InlineItemTooltipWrapsLinesAndReturnsConsumedHeight()
        {
            var backend = new RecordingRenderBackend();
            var renderer = new UiRenderer(backend);
            var source = new ItemSource();
            var service = new TooltipService(source);

            int height = service.DrawItemAt(
                renderer,
                UiPalette.Default,
                type: 42,
                bounds: new RenderRectangle(10, 20, 40, 200));

            Assert.Equal(88, height);
            Assert.Equal(42, source.LastItemType);
            Assert.Collection(
                backend.DrawnTexts,
                first => Assert.Equal("Swo", first),
                second => Assert.Equal("rd", second),
                third => Assert.Equal("Dam", third),
                fourth => Assert.Equal("age", fourth));
            Assert.Equal(88, backend.FilledRectangles[0].Height);
        }

        [Fact]
        public void DeferredTooltipClipsOverflowToItsCappedBounds()
        {
            var backend = new RecordingRenderBackend();
            var renderer = new UiRenderer(backend);
            var featureInput = new StubFeatureInput { MousePosition = new InputPoint(10, 10) };
            var service = new TooltipService();
            var viewport = new RenderRectangle(0, 0, 100, 40);
            service.Set("one\ntwo\nthree\nfour");

            service.DrawDeferred(
                renderer,
                new WidgetInput(featureInput),
                viewport,
                UiPalette.Default);

            RenderRectangle bounds = backend.FilledRectangles[0];
            Assert.Equal(viewport.Height, bounds.Height);
            Assert.True(backend.DrawnTexts.Count * renderer.TextLineHeight + 16 > bounds.Height);
            Assert.Collection(backend.Clips, clip => Assert.Equal(bounds, clip));
            Assert.Equal(new[] { 1 }, backend.RestoredStates);
        }

        private sealed class ItemSource : IItemTooltipSource
        {
            public int LastItemType { get; private set; }

            public IReadOnlyList<TooltipLine> Build(int itemType, int prefix, int stack)
            {
                LastItemType = itemType;
                return new[]
                {
                    new TooltipLine("Sword", new RenderColour(255, 255, 100)),
                    new TooltipLine("Damage", new RenderColour(255, 255, 255))
                };
            }
        }
    }
}
