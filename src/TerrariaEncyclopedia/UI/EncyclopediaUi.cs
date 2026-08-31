using System;
using TerrariaEncyclopedia.Runtime;
using RuntimeInput = TerrariaEncyclopedia.Runtime.UI.Input.WidgetInput;
using RuntimeCheckbox = TerrariaEncyclopedia.Runtime.UI.Widgets.Checkbox;
using RuntimeDraggablePanel = TerrariaEncyclopedia.Runtime.UI.Widgets.DraggablePanel;
using RuntimeScrollView = TerrariaEncyclopedia.Runtime.UI.Widgets.ScrollView;
using RuntimeTextInput = TerrariaEncyclopedia.Runtime.UI.Widgets.TextInput;
using TerrariaEncyclopedia.Runtime.UI.Rendering;
using TerrariaEncyclopedia.Runtime.UI.Tooltips;

namespace TerrariaEncyclopedia.UI
{
    internal sealed class EncyclopediaUi
    {
        public EncyclopediaUi(IEncyclopediaFeatureRenderer renderer, IFeatureInput input)
        {
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            Input = new RuntimeInput(input ?? throw new ArgumentNullException(nameof(input)));
            Tooltips = new TooltipService(renderer.ItemTooltips);
        }

        public IEncyclopediaFeatureRenderer Renderer { get; }

        public RuntimeInput Input { get; }

        public TooltipService Tooltips { get; }

        public UiPalette Palette { get; } = UiPalette.Default;

        public void BeginUpdate() => Input.BeginFrame();

        public void BeginRender() => Tooltips.BeginFrame();

        public void DrawDeferredTooltips()
        {
            Tooltips.DrawDeferred(Renderer, Input, Renderer.Viewport, Palette);
        }
    }

    internal static class EncyclopediaUiContext
    {
        private static EncyclopediaUi current;

        public static EncyclopediaUi Current => current
            ?? throw new InvalidOperationException("The Encyclopedia UI has not been initialised.");

        public static void Set(EncyclopediaUi value)
        {
            current = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static void Clear(EncyclopediaUi value)
        {
            if (ReferenceEquals(current, value))
                current = null;
        }
    }

    internal readonly struct Color4
    {
        public Color4(byte red, byte green, byte blue, byte alpha = 255)
        {
            Value = new RenderColour(red, green, blue, alpha);
        }

        public Color4(int red, int green, int blue, int alpha = 255)
            : this((byte)red, (byte)green, (byte)blue, (byte)alpha)
        {
        }

        public Color4(RenderColour value)
        {
            Value = value;
        }

        public RenderColour Value { get; }

        public static implicit operator RenderColour(Color4 colour) => colour.Value;

        public static implicit operator Color4(RenderColour colour) => new Color4(colour);
    }

    internal static class UIColors
    {
        private static UiPalette Palette => EncyclopediaUiContext.Current.Palette;

        public static Color4 PanelBg => Palette.PanelBackground;
        public static Color4 SectionBg => Palette.SectionBackground;
        public static Color4 ItemBg => Palette.ItemBackground;
        public static Color4 ItemActiveBg => Palette.ItemActiveBackground;
        public static Color4 Text => Palette.Text;
        public static Color4 TextDim => Palette.DimText;
        public static Color4 TextTitle => Palette.TitleText;
        public static Color4 Accent => Palette.Accent;
        public static Color4 AccentText => Palette.AccentText;
        public static Color4 Border => Palette.Border;
        public static Color4 InputBg => Palette.InputBackground;
        public static Color4 InputFocusBg => Palette.InputFocusBackground;
        public static Color4 Warning => Palette.Warning;
        public static Color4 Error => Palette.Error;
    }

    internal static class UIRenderer
    {
        private static IUiRenderer Renderer => EncyclopediaUiContext.Current.Renderer;

        public static int TextLineHeight => Renderer.TextLineHeight;

        public static void DrawRect(int x, int y, int width, int height, Color4 colour)
        {
            Renderer.FillRectangle(new RenderRectangle(x, y, width, height), colour);
        }

        public static void DrawRectOutline(
            int x,
            int y,
            int width,
            int height,
            Color4 colour,
            int thickness = 1)
        {
            Renderer.StrokeRectangle(new RenderRectangle(x, y, width, height), colour, thickness);
        }

        public static void DrawText(string text, int x, int y, Color4 colour)
        {
            Renderer.DrawText(text, new InputPoint(x, y), colour);
        }

        public static void DrawItem(int id, int x, int y, int width, int height)
        {
            DrawIcon(TerrariaIconKind.Item, id, x, y, width, height);
        }

        public static void DrawNPC(int id, int x, int y, int width, int height)
        {
            DrawIcon(TerrariaIconKind.Npc, id, x, y, width, height);
        }

        public static void DrawBuff(int id, int x, int y, int width, int height)
        {
            DrawIcon(TerrariaIconKind.Buff, id, x, y, width, height);
        }

        private static void DrawIcon(
            TerrariaIconKind kind,
            int id,
            int x,
            int y,
            int width,
            int height)
        {
            Renderer.DrawTerrariaIcon(
                new TerrariaIcon(kind, id),
                new RenderRectangle(x, y, width, height),
                new RenderColour(255, 255, 255));
        }
    }

    internal static class TextUtil
    {
        public static int MeasureWidth(string text)
        {
            return EncyclopediaUiContext.Current.Renderer.MeasureText(text ?? string.Empty);
        }
    }

    internal static class WidgetInput
    {
        private static RuntimeInput Input => EncyclopediaUiContext.Current.Input;

        public static bool MouseLeftClick => Input.WasLeftPressed;

        public static bool IsMouseOver(int x, int y, int width, int height)
        {
            return Input.IsMouseOver(new RenderRectangle(x, y, width, height));
        }

        public static void ConsumeClick() => Input.ConsumeLeftPress();
    }

    internal static class Tooltip
    {
        public static void Set(string text) => EncyclopediaUiContext.Current.Tooltips.Set(text);

        public static void Set(string title, string text)
        {
            EncyclopediaUiContext.Current.Tooltips.Set(title, text);
        }
    }

    internal static class ItemTooltip
    {
        public static void Set(int itemId)
        {
            EncyclopediaUiContext.Current.Tooltips.SetItem(itemId);
        }

        public static int DrawAt(int itemId, int x, int y, int width)
        {
            EncyclopediaUi ui = EncyclopediaUiContext.Current;
            return ui.Tooltips.DrawItemAt(
                ui.Renderer,
                ui.Palette,
                itemId,
                new RenderRectangle(x, y, width, 0));
        }
    }

    internal sealed class DraggablePanel
    {
        private readonly RuntimeDraggablePanel panel;
        private Action drawCallback;
        private IDisposable contentClip;

        public DraggablePanel(string id, string title, int width, int height)
        {
            panel = new RuntimeDraggablePanel(id, title, width, height);
        }

        public string Title { get => panel.Title; set => panel.Title = value; }
        public bool IsOpen => panel.IsOpen;
        public bool ClipContent { get => panel.ClipContent; set => panel.ClipContent = value; }
        public int ContentX => panel.ContentBounds.X;
        public int ContentY => panel.ContentBounds.Y;
        public int ContentWidth => panel.ContentBounds.Width;
        public int ContentHeight => panel.ContentBounds.Height;
        public Action OnClose { get => panel.OnClose; set => panel.OnClose = value; }

        public void RegisterDrawCallback(Action callback) => drawCallback = callback;

        public void UnregisterDrawCallback() => drawCallback = null;

        public void Open() => panel.Open();

        public void Close() => panel.Close();

        public void Update()
        {
            EncyclopediaUi ui = EncyclopediaUiContext.Current;
            panel.Update(ui.Input, ui.Renderer.Viewport);
        }

        public void Draw()
        {
            if (!panel.IsOpen)
                return;
            EncyclopediaUiContext.Current.BeginRender();
            drawCallback?.Invoke();
        }

        public bool BeginDraw()
        {
            if (!panel.IsOpen)
                return false;
            EncyclopediaUi ui = EncyclopediaUiContext.Current;
            panel.DrawChrome(ui.Renderer, ui.Input, ui.Palette);
            contentClip = panel.PushContentClip(ui.Renderer);
            return true;
        }

        public void EndDraw()
        {
            contentClip?.Dispose();
            contentClip = null;
            EncyclopediaUiContext.Current.DrawDeferredTooltips();
        }
    }

    internal sealed class TextInput
    {
        private readonly RuntimeTextInput input;

        public TextInput(string ownerId, string placeholder, int maxLength)
        {
            input = new RuntimeTextInput(
                ownerId,
                placeholder,
                maxLength,
                EncyclopediaUiContext.Current.Renderer.TextEntry);
        }

        public string Text { get => input.Text; set => input.Text = value; }
        public bool HasChanged => input.HasChanged;
        public bool IsFocused => input.IsFocused;

        public void Update()
        {
            // Bounds are known during Draw; input processing happens there.
        }

        public void Draw(int x, int y, int width, int height)
        {
            EncyclopediaUi ui = EncyclopediaUiContext.Current;
            var bounds = new RenderRectangle(x, y, width, height);
            input.Update(ui.Input, bounds);
            input.Draw(ui.Renderer, bounds, ui.Palette);
        }

        public void Unfocus() => input.Unfocus();
    }

    internal sealed class ScrollView
    {
        private readonly RuntimeScrollView scroll = new RuntimeScrollView();
        private IDisposable clip;

        public int ContentWidth => scroll.ContentWidth;
        public int ContentY => scroll.ContentY;
        public int ScrollOffset => scroll.ScrollOffset;

        public void Begin(int x, int y, int width, int height, int contentHeight)
        {
            EncyclopediaUi ui = EncyclopediaUiContext.Current;
            scroll.Begin(new RenderRectangle(x, y, width, height), contentHeight);
            scroll.Update(ui.Input);
            clip = scroll.PushClip(ui.Renderer);
        }

        public bool IsVisible(int itemY, int itemHeight) => scroll.IsVisible(itemY, itemHeight);

        public void End()
        {
            clip?.Dispose();
            clip = null;
            EncyclopediaUi ui = EncyclopediaUiContext.Current;
            scroll.DrawScrollbar(ui.Renderer, ui.Input, ui.Palette);
        }

        public void ResetScroll() => scroll.ResetScroll();
    }

    internal static class Checkbox
    {
        public static bool DrawWithLabel(
            int x,
            int y,
            int width,
            int height,
            string label,
            bool value)
        {
            EncyclopediaUi ui = EncyclopediaUiContext.Current;
            bool next = RuntimeCheckbox.DrawWithLabel(
                ui.Renderer,
                ui.Input,
                ui.Palette,
                new RenderRectangle(x, y, width, height),
                label,
                value);
            return next != value;
        }
    }

    internal sealed class StackLayout
    {
        private readonly TerrariaEncyclopedia.Runtime.UI.Widgets.StackLayout layout;

        public StackLayout(int x, int y, int width)
        {
            layout = new TerrariaEncyclopedia.Runtime.UI.Widgets.StackLayout(x, y, width);
        }

        public int X => layout.X;
        public int CurrentY => layout.CurrentY;
        public int Width => layout.Width;
        public int TotalHeight => layout.TotalHeight;
        public int Advance(int height) => layout.Advance(height).Y;
    }
}
