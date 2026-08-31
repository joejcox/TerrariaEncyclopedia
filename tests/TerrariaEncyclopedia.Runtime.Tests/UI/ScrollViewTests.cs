using TerrariaEncyclopedia.Runtime.UI.Input;
using TerrariaEncyclopedia.Runtime.UI.Widgets;
using Xunit;

namespace TerrariaEncyclopedia.Runtime.Tests.UI
{
    public sealed class ScrollViewTests
    {
        [Fact]
        public void WheelScrollsInFixedStepsAndClampsToContent()
        {
            var source = new StubFeatureInput
            {
                MousePosition = new InputPoint(20, 20),
                ScrollWheelDelta = -120
            };
            var input = new WidgetInput(source);
            var scroll = new ScrollView();
            scroll.Begin(new RenderRectangle(10, 10, 100, 100), 230);

            for (int index = 0; index < 10; index++)
            {
                input.BeginFrame();
                scroll.Update(input);
            }

            Assert.Equal(130, scroll.MaxScroll);
            Assert.Equal(130, scroll.ScrollOffset);
            Assert.Equal(-120, scroll.ContentY);

            source.ScrollWheelDelta = 120;
            input.BeginFrame();
            scroll.Update(input);

            Assert.Equal(90, scroll.ScrollOffset);
        }

        [Fact]
        public void ContentWidthAndThumbReflectWhetherScrollingIsNeeded()
        {
            var scroll = new ScrollView();
            scroll.Begin(new RenderRectangle(5, 10, 120, 100), 250);

            Assert.True(scroll.NeedsScrolling);
            Assert.Equal(108, scroll.ContentWidth);
            RenderRectangle thumb = scroll.GetThumbBounds();
            Assert.Equal(40, thumb.Height);
            Assert.Equal(115, thumb.X);

            scroll.Begin(new RenderRectangle(5, 10, 120, 100), 80);

            Assert.False(scroll.NeedsScrolling);
            Assert.Equal(120, scroll.ContentWidth);
            Assert.Equal(0, scroll.ScrollOffset);
        }

        [Fact]
        public void VisibilityIncludesPartiallyVisibleRowsBecauseTheViewportClipsThem()
        {
            var source = new StubFeatureInput
            {
                MousePosition = new InputPoint(10, 10),
                ScrollWheelDelta = -120
            };
            var input = new WidgetInput(source);
            var scroll = new ScrollView();
            scroll.Begin(new RenderRectangle(0, 0, 100, 50), 200);
            input.BeginFrame();
            scroll.Update(input);

            Assert.True(scroll.IsVisible(30, 20));
            Assert.True(scroll.IsVisible(80, 20));
            Assert.False(scroll.IsVisible(100, 20));
        }
    }
}
