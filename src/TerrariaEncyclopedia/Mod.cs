using Terraria.ID;
using TerrariaEncyclopedia.Compatibility;
using TerrariaEncyclopedia.UI;
using TerrariaModder.Core;
using TerrariaModder.Core.Events;
using TerrariaModder.Core.Logging;

namespace TerrariaEncyclopedia
{
    /// <summary>
    /// Phase 1: version-guarded proof of concept. Phase 2: read-only item browser
    /// (search, category tabs, icon grid, details strip). No world/save data is
    /// ever read or written — purely reflects already-loaded game state.
    /// </summary>
    public class Mod : IMod
    {
        public string Id => "terraria-encyclopedia";
        public string Name => "Terraria Encyclopedia";
        public string Version => "0.7.0";

        private ILogger _log;
        private ItemBrowserPanel _browser;

        public void Initialize(ModContext context)
        {
            _log = context.Logger;

            if (!GameVersionGuard.Check())
            {
                _log.Warn($"{Name}: unsupported Terraria version. Expected {GameVersionGuard.ExpectedVersion}, " +
                          $"detected {GameVersionGuard.DetectedVersion}. Encyclopedia disabled; Terraria continues normally.");
                return;
            }

            _browser = new ItemBrowserPanel();
            FrameEvents.OnPreUpdate += OnUpdate;

            // ponytail: project.md suggested F7, but F7 and F8 are both vanilla Terraria's own hidden
            // debug overlays (confirmed empirically — frame-timing graphs, then packet-stats — neither
            // registered anywhere in TerrariaModder source) and F6 is Core's own mod menu. F2 turned
            // out to be vanilla's "switch to Loadout 2" hotkey (confirmed live — kept swapping gear
            // sets). P sits outside all of that.
            context.RegisterKeybind("toggle", "Toggle Encyclopedia", "Open/close the Encyclopedia window", "P", OnTogglePressed);

            _log.Info($"{Name} v{Version} initialized. Terraria {GameVersionGuard.DetectedVersion}, {ItemID.Count} items loaded.");
        }

        private void OnTogglePressed() => _browser?.Toggle();

        private void OnUpdate() => _browser?.Update();

        public void Unload()
        {
            FrameEvents.OnPreUpdate -= OnUpdate;
            _browser?.Unload();
            _log.Info($"{Name} unloaded");
        }
    }
}
