using System;
using System.Collections.Generic;

namespace TerrariaEncyclopedia.Runtime
{
    public readonly struct InputKey : IEquatable<InputKey>
    {
        public InputKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A key name is required.", nameof(name));
            }

            Name = name;
        }

        public string Name { get; }

        public bool Equals(InputKey other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is InputKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Name == null ? 0 : StringComparer.Ordinal.GetHashCode(Name);
        }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }

        public static bool operator ==(InputKey left, InputKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InputKey left, InputKey right)
        {
            return !left.Equals(right);
        }
    }

    public enum MouseButton
    {
        Left,
        Middle,
        Right,
        XButton1,
        XButton2
    }

    public readonly struct InputPoint : IEquatable<InputPoint>
    {
        public InputPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public bool Equals(InputPoint other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is InputPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }
    }

    public sealed class InputSnapshot
    {
        private readonly HashSet<InputKey> keysDown;
        private readonly HashSet<MouseButton> mouseButtonsDown;

        public InputSnapshot(
            IEnumerable<InputKey> keysDown = null,
            InputPoint mousePosition = default(InputPoint),
            IEnumerable<MouseButton> mouseButtonsDown = null,
            int scrollWheelValue = 0)
        {
            this.keysDown = new HashSet<InputKey>(keysDown ?? Array.Empty<InputKey>());
            this.mouseButtonsDown = new HashSet<MouseButton>(
                mouseButtonsDown ?? Array.Empty<MouseButton>());
            MousePosition = mousePosition;
            ScrollWheelValue = scrollWheelValue;
        }

        public static InputSnapshot Empty { get; } = new InputSnapshot();

        public InputPoint MousePosition { get; }

        public int ScrollWheelValue { get; }

        public bool IsKeyDown(InputKey key)
        {
            return keysDown.Contains(key);
        }

        internal bool IsMouseButtonDown(MouseButton button)
        {
            return mouseButtonsDown.Contains(button);
        }
    }

    public interface IFeatureInput
    {
        InputPoint MousePosition { get; }

        int ScrollWheelDelta { get; }

        bool IsKeyDown(InputKey key);

        bool WasKeyPressed(InputKey key);

        bool WasKeyReleased(InputKey key);

        bool IsMouseButtonDown(MouseButton button);

        bool WasMouseButtonPressed(MouseButton button);

        bool WasMouseButtonReleased(MouseButton button);
    }

    public interface IKeyBindings
    {
        void RegisterToggle(InputKey key, Action action);
    }

    internal sealed class FeatureInput : IFeatureInput
    {
        private InputSnapshot previous = InputSnapshot.Empty;
        private InputSnapshot current = InputSnapshot.Empty;

        public InputPoint MousePosition => current.MousePosition;

        public int ScrollWheelDelta => current.ScrollWheelValue - previous.ScrollWheelValue;

        public void Advance(InputSnapshot snapshot)
        {
            previous = current;
            current = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public bool IsKeyDown(InputKey key)
        {
            return current.IsKeyDown(key);
        }

        public bool WasKeyPressed(InputKey key)
        {
            return current.IsKeyDown(key) && !previous.IsKeyDown(key);
        }

        public bool WasKeyReleased(InputKey key)
        {
            return !current.IsKeyDown(key) && previous.IsKeyDown(key);
        }

        public bool IsMouseButtonDown(MouseButton button)
        {
            return current.IsMouseButtonDown(button);
        }

        public bool WasMouseButtonPressed(MouseButton button)
        {
            return current.IsMouseButtonDown(button) && !previous.IsMouseButtonDown(button);
        }

        public bool WasMouseButtonReleased(MouseButton button)
        {
            return !current.IsMouseButtonDown(button) && previous.IsMouseButtonDown(button);
        }
    }
}
