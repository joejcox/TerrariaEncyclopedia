using TerrariaEncyclopedia.Hosting;
using Xunit;

namespace TerrariaEncyclopedia.Tests
{
    public sealed class TerrariaUiCoordinateSpaceTests
    {
        [Theory]
        [InlineData(1f, 1201, 901, 1201, 901)]
        [InlineData(1.5f, 1201, 901, 800, 600)]
        [InlineData(2f, 1201, 901, 600, 450)]
        public void PointerConvertsTerrariaCoordinatesOnce(
            float uiScale,
            int terrariaX,
            int terrariaY,
            int expectedX,
            int expectedY)
        {
            var pointer = TerrariaUiCoordinateSpace.ToLogicalPointer(
                terrariaX,
                terrariaY,
                uiScale);

            Assert.Equal(expectedX, pointer.X);
            Assert.Equal(expectedY, pointer.Y);
        }

        [Fact]
        public void PointerUsesTerrariaCanonicalCoordinatesOnRetinaDisplays()
        {
            const int rawWindowX = 600;
            const int rawMouseScale = 2;
            const float uiScale = 1.5f;
            int terrariaX = rawWindowX * rawMouseScale;

            var pointer = TerrariaUiCoordinateSpace.ToLogicalPointer(terrariaX, 0, uiScale);

            Assert.Equal(800, pointer.X);
            Assert.NotEqual((int)(rawWindowX / uiScale), pointer.X);
        }

        [Theory]
        [InlineData(1f, 1921, 1081, 1921, 1081)]
        [InlineData(1.5f, 1921, 1081, 1280, 720)]
        [InlineData(2f, 1921, 1081, 960, 540)]
        public void ViewportConvertsCachedRealDimensionsOnce(
            float uiScale,
            int realWidth,
            int realHeight,
            int expectedWidth,
            int expectedHeight)
        {
            var viewport = TerrariaUiCoordinateSpace.ToLogicalViewport(
                realWidth,
                realHeight,
                uiScale);

            Assert.Equal(0, viewport.X);
            Assert.Equal(0, viewport.Y);
            Assert.Equal(expectedWidth, viewport.Width);
            Assert.Equal(expectedHeight, viewport.Height);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        public void InvalidScaleFallsBackToUnscaledCoordinates(float uiScale)
        {
            var pointer = TerrariaUiCoordinateSpace.ToLogicalPointer(640, 360, uiScale);
            var viewport = TerrariaUiCoordinateSpace.ToLogicalViewport(1280, 720, uiScale);

            Assert.Equal(640, pointer.X);
            Assert.Equal(360, pointer.Y);
            Assert.Equal(1280, viewport.Width);
            Assert.Equal(720, viewport.Height);
        }

        [Fact]
        public void ViewportKeepsPositiveBoundsAfterTruncation()
        {
            var viewport = TerrariaUiCoordinateSpace.ToLogicalViewport(1, 1, 2f);

            Assert.Equal(1, viewport.Width);
            Assert.Equal(1, viewport.Height);
        }
    }
}
