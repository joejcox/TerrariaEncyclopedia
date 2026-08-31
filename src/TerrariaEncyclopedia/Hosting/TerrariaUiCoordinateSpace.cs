using System;
using TerrariaEncyclopedia.Runtime;

namespace TerrariaEncyclopedia.Hosting
{
    internal static class TerrariaUiCoordinateSpace
    {
        public static InputPoint ToLogicalPointer(int terrariaX, int terrariaY, float uiScale)
        {
            float scale = SafeScale(uiScale);
            return new InputPoint((int)(terrariaX / scale), (int)(terrariaY / scale));
        }

        public static RenderRectangle ToLogicalViewport(
            int realWidth,
            int realHeight,
            float uiScale)
        {
            float scale = SafeScale(uiScale);
            return new RenderRectangle(
                0,
                0,
                Math.Max(1, (int)(realWidth / scale)),
                Math.Max(1, (int)(realHeight / scale)));
        }

        private static float SafeScale(float uiScale)
        {
            return uiScale > 0f && !float.IsInfinity(uiScale) ? uiScale : 1f;
        }
    }
}
