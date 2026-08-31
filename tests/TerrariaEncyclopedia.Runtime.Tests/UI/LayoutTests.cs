using TerrariaEncyclopedia.Runtime.UI.Tooltips;
using TerrariaEncyclopedia.Runtime.UI.Widgets;
using Xunit;

namespace TerrariaEncyclopedia.Runtime.Tests.UI
{
    public sealed class LayoutTests
    {
        [Fact]
        public void PanelCentresAndCalculatesContentBounds()
        {
            PanelLayout layout = PanelLayout.Calculate(
                new RenderRectangle(0, 0, 1280, 800),
                width: 1200,
                height: 750,
                headerHeight: 35,
                padding: 8);

            AssertRectangle(layout.Bounds, 40, 25, 1200, 750);
            AssertRectangle(layout.ContentBounds, 48, 60, 1184, 707);
            AssertRectangle(layout.CloseButtonBounds, 1205, 27, 30, 30);
        }

        [Fact]
        public void OversizedPanelShrinksAndClampsToViewport()
        {
            PanelLayout layout = PanelLayout.Calculate(
                new RenderRectangle(100, 50, 500, 300),
                width: 1200,
                height: 750,
                headerHeight: 35,
                padding: 8,
                requestedPosition: new InputPoint(-200, 900));

            AssertRectangle(layout.Bounds, 100, 50, 500, 300);
            AssertRectangle(layout.ContentBounds, 108, 85, 484, 257);
        }

        [Fact]
        public void StackLayoutAdvancesDeterministically()
        {
            var layout = new StackLayout(10, 20, 200, spacing: 5);

            AssertRectangle(layout.Advance(30), 10, 20, 200, 30);
            AssertRectangle(layout.Advance(10), 10, 55, 200, 10);
            Assert.Equal(45, layout.TotalHeight);
        }

        [Fact]
        public void TooltipFlipsAroundAnchorAndStaysInsideViewport()
        {
            TooltipLayout layout = TooltipLayout.Place(
                new InputPoint(195, 95),
                new RenderRectangle(0, 0, 200, 100),
                contentWidth: 80,
                contentHeight: 30,
                padding: 8,
                gap: 16);

            AssertRectangle(layout.Bounds, 95, 45, 96, 46);
        }

        [Fact]
        public void TextWrapBreaksLongWordsWithoutDroppingCharacters()
        {
            var lines = TextLayout.Wrap("one abcdef", 24, value => value.Length * 8);

            Assert.Equal(new[] { "one", "abc", "def" }, lines);
            Assert.Equal("def", TextLayout.VisibleTail("abcdef", 24, value => value.Length * 8));
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
