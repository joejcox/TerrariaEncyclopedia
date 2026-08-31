using TerrariaEncyclopedia.Runtime.UI.Input;
using Xunit;

namespace TerrariaEncyclopedia.Runtime.Tests.UI
{
    public sealed class WidgetInputTests
    {
        [Fact]
        public void PressEdgeCanBeConsumedOnlyOncePerFrame()
        {
            var source = new StubFeatureInput { MousePosition = new InputPoint(15, 15) };
            source.SetMouse(MouseButton.Left, down: true, pressed: true);
            var input = new WidgetInput(source);
            input.BeginFrame();
            var bounds = new RenderRectangle(10, 10, 20, 20);

            Assert.True(input.TryConsumeLeftPress(bounds));
            Assert.False(input.WasLeftPressed);
            Assert.False(input.TryConsumeLeftPress(bounds));

            input.BeginFrame();

            Assert.True(input.WasLeftPressed);
        }

        [Fact]
        public void HoverUsesInclusiveTopLeftAndExclusiveBottomRightEdges()
        {
            var source = new StubFeatureInput();
            var input = new WidgetInput(source);
            var bounds = new RenderRectangle(10, 20, 30, 40);

            source.MousePosition = new InputPoint(10, 20);
            Assert.True(input.IsMouseOver(bounds));

            source.MousePosition = new InputPoint(40, 60);
            Assert.False(input.IsMouseOver(bounds));
        }

        [Fact]
        public void ScrollCanBeConsumedOnlyInsideTheTargetBounds()
        {
            var source = new StubFeatureInput
            {
                MousePosition = new InputPoint(5, 5),
                ScrollWheelDelta = -120
            };
            var input = new WidgetInput(source);
            input.BeginFrame();

            Assert.False(input.TryConsumeScroll(new RenderRectangle(10, 10, 20, 20), out _));
            Assert.True(input.TryConsumeScroll(new RenderRectangle(0, 0, 20, 20), out int delta));
            Assert.Equal(-120, delta);
            Assert.Equal(0, input.ScrollDelta);
        }
    }
}
