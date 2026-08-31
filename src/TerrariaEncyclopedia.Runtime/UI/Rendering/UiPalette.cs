namespace TerrariaEncyclopedia.Runtime.UI.Rendering
{
    public sealed class UiPalette
    {
        public static UiPalette Default { get; } = new UiPalette();

        public RenderColour PanelBackground { get; } = new RenderColour(30, 30, 45, 240);
        public RenderColour HeaderBackground { get; } = new RenderColour(50, 50, 70);
        public RenderColour SectionBackground { get; } = new RenderColour(35, 35, 55);
        public RenderColour TooltipBackground { get; } = new RenderColour(20, 20, 30, 240);
        public RenderColour ItemBackground { get; } = new RenderColour(45, 45, 65, 200);
        public RenderColour ItemActiveBackground { get; } = new RenderColour(70, 70, 100);
        public RenderColour Text { get; } = new RenderColour(255, 255, 255);
        public RenderColour DimText { get; } = new RenderColour(180, 180, 180);
        public RenderColour HintText { get; } = new RenderColour(120, 120, 120);
        public RenderColour TitleText { get; } = new RenderColour(255, 255, 100);
        public RenderColour Success { get; } = new RenderColour(100, 255, 100);
        public RenderColour Error { get; } = new RenderColour(255, 100, 100);
        public RenderColour Warning { get; } = new RenderColour(255, 200, 100);
        public RenderColour Accent { get; } = new RenderColour(100, 150, 200);
        public RenderColour AccentText { get; } = new RenderColour(255, 255, 180);
        public RenderColour Border { get; } = new RenderColour(100, 100, 140);
        public RenderColour InputBackground { get; } = new RenderColour(35, 35, 50);
        public RenderColour InputFocusBackground { get; } = new RenderColour(50, 50, 70);
        public RenderColour ScrollTrack { get; } = new RenderColour(30, 30, 40, 200);
        public RenderColour ScrollThumb { get; } = new RenderColour(80, 80, 100);
        public RenderColour ScrollThumbHover { get; } = new RenderColour(110, 110, 140);
        public RenderColour CloseButton { get; } = new RenderColour(110, 55, 65);
        public RenderColour CloseButtonHover { get; } = new RenderColour(160, 65, 75);
    }
}
