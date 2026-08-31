using Terraria.ID;
using TerrariaEncyclopedia.Compatibility;
using TerrariaModder.Core;
using TerrariaModder.Core.Logging;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace TerrariaEncyclopedia
{
    /// <summary>
    /// Phase 1 proof of concept: F7 opens a read-only panel showing the detected
    /// Terraria version and loaded item count, then closes cleanly. No world/save
    /// data is read or written.
    /// </summary>
    public class Mod : IMod
    {
        public string Id => "terraria-encyclopedia";
        public string Name => "Terraria Encyclopedia";
        public string Version => "0.1.0";

        private ILogger _log;
        private DraggablePanel _panel;

        public void Initialize(ModContext context)
        {
            _log = context.Logger;

            if (!GameVersionGuard.Check())
            {
                _log.Warn($"{Name}: unsupported Terraria version. Expected {GameVersionGuard.ExpectedVersion}, " +
                          $"detected {GameVersionGuard.DetectedVersion}. Encyclopedia disabled; Terraria continues normally.");
                return;
            }

            _panel = new DraggablePanel("terraria-encyclopedia", "Terraria Encyclopedia", 360, 100);
            // ponytail: Core's own BeginClip sets GraphicsDevice.ScissorRectangle in raw device
            // pixels, which isn't affected by the SpriteBatch UI-scale transform — the clip region
            // and the actual (transformed) draw position drift apart based on screen position
            // (confirmed: text vanished near the bottom, fine at the top). ItemSpawner's own source
            // disables ClipContent for the same reason ("BeginClip causes transform issues"). Our
            // content is two static lines with no overflow risk, so no clipping is needed anyway.
            _panel.ClipContent = false;
            _panel.RegisterDrawCallback(OnDraw);

            // ponytail: project.md suggested F7, but F7 and F8 are both vanilla Terraria's own hidden
            // debug overlays (confirmed empirically — frame-timing graphs, then packet-stats — neither
            // registered anywhere in TerrariaModder source) and F6 is Core's own mod menu. The bundled
            // mods in this framework use F5/F9/F10 for their own keybinds, implying their authors already
            // mapped F7/F8 as vanilla-reserved and avoided them — F2 sits outside all of that.
            context.RegisterKeybind("toggle", "Toggle Encyclopedia", "Open/close the Encyclopedia window", "F2", OnTogglePressed);

            _log.Info($"{Name} v{Version} initialized. Terraria {GameVersionGuard.DetectedVersion}, {ItemID.Count} items loaded.");
        }

        private void OnTogglePressed()
        {
            if (_panel == null) return; // disabled by version guard
            if (_panel.IsOpen) _panel.Close();
            else _panel.Open();
        }

        private void OnDraw()
        {
            if (!_panel.BeginDraw()) return;
            try
            {
                // Panel header already shows the title — don't repeat it as a content line.
                UIRenderer.DrawText($"Terraria version: {GameVersionGuard.DetectedVersion}", _panel.ContentX, _panel.ContentY, UIColors.Text);
                UIRenderer.DrawText($"Loaded items: {ItemID.Count}", _panel.ContentX, _panel.ContentY + 22, UIColors.Text);
            }
            finally
            {
                _panel.EndDraw();
            }
        }

        public void Unload()
        {
            _panel?.Close();
            _log.Info($"{Name} unloaded");
        }
    }
}
