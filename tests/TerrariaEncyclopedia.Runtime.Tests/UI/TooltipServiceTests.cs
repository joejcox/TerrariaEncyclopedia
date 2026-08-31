using System.Collections.Generic;
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
