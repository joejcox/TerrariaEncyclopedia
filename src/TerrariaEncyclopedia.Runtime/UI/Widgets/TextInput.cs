using System;
using TerrariaEncyclopedia.Runtime.UI.Input;
using TerrariaEncyclopedia.Runtime.UI.Rendering;
using TerrariaEncyclopedia.Runtime.UI.Tooltips;

namespace TerrariaEncyclopedia.Runtime.UI.Widgets
{
    // Adapted from TerrariaModder v0.4.0 TextInput under the MIT licence.
    public sealed class TextInput
    {
        private readonly string ownerId;
        private readonly string placeholder;
        private readonly int maxLength;
        private readonly ITextEntrySession textEntry;
        private string text = string.Empty;
        private bool changed;

        public TextInput(
            string ownerId,
            string placeholder,
            int maxLength,
            ITextEntrySession textEntry = null)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                throw new ArgumentException("A text-input owner id is required.", nameof(ownerId));
            }

            if (maxLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            }

            this.ownerId = ownerId;
            this.placeholder = placeholder ?? string.Empty;
            this.maxLength = maxLength;
            this.textEntry = textEntry ?? NullTextEntrySession.Instance;
        }

        public string Text
        {
            get => text;
            set
            {
                string next = value ?? string.Empty;
                text = next.Length <= maxLength ? next : next.Substring(0, maxLength);
            }
        }

        public bool IsFocused { get; private set; }

        public bool HasChanged
        {
            get
            {
                bool result = changed;
                changed = false;
                return result;
            }
        }

        public void Update(WidgetInput input, RenderRectangle bounds)
        {
            if (input.TryConsumeLeftPress(bounds))
            {
                Focus();
            }
            else if (input.WasLeftPressed && !input.IsMouseOver(bounds))
            {
                Unfocus();
            }

            if (!IsFocused)
            {
                return;
            }

            if (input.WasKeyPressed("Escape"))
            {
                Unfocus();
                return;
            }

            string next = textEntry.Read(text, maxLength) ?? text;
            if (next.Length > maxLength)
            {
                next = next.Substring(0, maxLength);
            }

            if (!string.Equals(next, text, StringComparison.Ordinal))
            {
                text = next;
                changed = true;
            }
        }

        public void Draw(IUiRenderer renderer, RenderRectangle bounds, UiPalette palette)
        {
            renderer.FillRectangle(
                bounds,
                IsFocused ? palette.InputFocusBackground : palette.InputBackground);
            renderer.StrokeRectangle(bounds, IsFocused ? palette.Accent : palette.Border);

            string displayText = string.IsNullOrEmpty(text) ? placeholder : text;
            RenderColour colour = string.IsNullOrEmpty(text) ? palette.HintText : palette.Text;
            string visible = TextLayout.VisibleTail(
                displayText,
                Math.Max(0, bounds.Width - 12),
                renderer.MeasureText);
            renderer.DrawText(
                visible,
                new InputPoint(bounds.X + 6, bounds.Y + Math.Max(0, (bounds.Height - renderer.TextLineHeight) / 2)),
                colour);
        }

        public void Unfocus()
        {
            if (!IsFocused)
            {
                return;
            }

            IsFocused = false;
            textEntry.End(ownerId);
        }

        private void Focus()
        {
            if (IsFocused)
            {
                return;
            }

            IsFocused = true;
            textEntry.Begin(ownerId);
        }
    }
}
