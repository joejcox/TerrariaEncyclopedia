using System;
using System.Collections.Generic;
using TerrariaEncyclopedia.Runtime.UI.Rendering;

namespace TerrariaEncyclopedia.Runtime.Tests.UI
{
    internal sealed class StubFeatureInput : IFeatureInput
    {
        private readonly HashSet<InputKey> keysDown = new HashSet<InputKey>();
        private readonly HashSet<InputKey> keysPressed = new HashSet<InputKey>();
        private readonly HashSet<InputKey> keysReleased = new HashSet<InputKey>();
        private readonly HashSet<MouseButton> buttonsDown = new HashSet<MouseButton>();
        private readonly HashSet<MouseButton> buttonsPressed = new HashSet<MouseButton>();
        private readonly HashSet<MouseButton> buttonsReleased = new HashSet<MouseButton>();

        public InputPoint MousePosition { get; set; }

        public int ScrollWheelDelta { get; set; }

        public void SetMouse(MouseButton button, bool down, bool pressed = false, bool released = false)
        {
            Set(buttonsDown, button, down);
            Set(buttonsPressed, button, pressed);
            Set(buttonsReleased, button, released);
        }

        public void SetKey(InputKey key, bool down, bool pressed = false, bool released = false)
        {
            Set(keysDown, key, down);
            Set(keysPressed, key, pressed);
            Set(keysReleased, key, released);
        }

        public bool IsKeyDown(InputKey key) => keysDown.Contains(key);

        public bool WasKeyPressed(InputKey key) => keysPressed.Contains(key);

        public bool WasKeyReleased(InputKey key) => keysReleased.Contains(key);

        public bool IsMouseButtonDown(MouseButton button) => buttonsDown.Contains(button);

        public bool WasMouseButtonPressed(MouseButton button) => buttonsPressed.Contains(button);

        public bool WasMouseButtonReleased(MouseButton button) => buttonsReleased.Contains(button);

        private static void Set<T>(HashSet<T> values, T value, bool present)
        {
            if (present)
            {
                values.Add(value);
            }
            else
            {
                values.Remove(value);
            }
        }
    }

    internal sealed class RecordingRenderBackend : IUiRenderBackend
    {
        private int nextState;

        public int TextLineHeight => 18;

        public List<RenderRectangle> Clips { get; } = new List<RenderRectangle>();

        public List<int> RestoredStates { get; } = new List<int>();

        public List<RenderRectangle> FilledRectangles { get; } = new List<RenderRectangle>();

        public List<string> DrawnTexts { get; } = new List<string>();

        public bool ThrowOnBeginClip { get; set; }

        public int MeasureText(string text) => (text ?? string.Empty).Length * 8;

        public IUiRenderState CaptureState() => new State(++nextState);

        public void BeginClip(RenderRectangle bounds, IUiRenderState capturedState)
        {
            Clips.Add(bounds);
            if (ThrowOnBeginClip)
            {
                throw new InvalidOperationException("clip failed");
            }
        }

        public void RestoreState(IUiRenderState state)
        {
            RestoredStates.Add(((State)state).Id);
        }

        public void FillRectangle(RenderRectangle bounds, RenderColour colour)
        {
            FilledRectangles.Add(bounds);
        }

        public void StrokeRectangle(RenderRectangle bounds, RenderColour colour, int thickness)
        {
        }

        public void DrawText(string text, InputPoint position, RenderColour colour)
        {
            DrawnTexts.Add(text);
        }

        public void DrawTerrariaIcon(
            TerrariaIcon icon,
            RenderRectangle destination,
            RenderColour colour)
        {
        }

        private sealed class State : IUiRenderState
        {
            public State(int id)
            {
                Id = id;
            }

            public int Id { get; }
        }
    }
}
