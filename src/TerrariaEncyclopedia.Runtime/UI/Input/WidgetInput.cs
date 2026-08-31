using System;

namespace TerrariaEncyclopedia.Runtime.UI.Input
{
    public sealed class WidgetInput
    {
        private readonly IFeatureInput input;
        private bool leftPressConsumed;
        private bool scrollConsumed;

        public WidgetInput(IFeatureInput input)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public bool Blocked { get; set; }

        public InputPoint MousePosition => input.MousePosition;

        public bool IsLeftDown => !Blocked && input.IsMouseButtonDown(MouseButton.Left);

        public bool WasLeftPressed => !Blocked
            && !leftPressConsumed
            && input.WasMouseButtonPressed(MouseButton.Left);

        public bool WasLeftReleased => !Blocked
            && input.WasMouseButtonReleased(MouseButton.Left);

        public int ScrollDelta => Blocked || scrollConsumed ? 0 : input.ScrollWheelDelta;

        public void BeginFrame()
        {
            Blocked = false;
            leftPressConsumed = false;
            scrollConsumed = false;
        }

        public bool IsMouseOver(RenderRectangle bounds)
        {
            return !Blocked && UiGeometry.Contains(bounds, MousePosition);
        }

        public bool TryConsumeLeftPress(RenderRectangle bounds)
        {
            if (!WasLeftPressed || !IsMouseOver(bounds))
            {
                return false;
            }

            leftPressConsumed = true;
            return true;
        }

        public bool TryConsumeScroll(RenderRectangle bounds, out int delta)
        {
            delta = ScrollDelta;
            if (delta == 0 || !IsMouseOver(bounds))
            {
                delta = 0;
                return false;
            }

            scrollConsumed = true;
            return true;
        }

        public void ConsumeLeftPress()
        {
            leftPressConsumed = true;
        }

        public void ConsumeScroll()
        {
            scrollConsumed = true;
        }

        public bool IsKeyDown(string key)
        {
            return !Blocked && input.IsKeyDown(new InputKey(key));
        }

        public bool WasKeyPressed(string key)
        {
            return !Blocked && input.WasKeyPressed(new InputKey(key));
        }
    }
}
