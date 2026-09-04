using System;
using Terraria.ID;
using TerrariaEncyclopedia.Compatibility;
using TerrariaEncyclopedia.Content;
using TerrariaEncyclopedia.Runtime;
using TerrariaEncyclopedia.UI;

namespace TerrariaEncyclopedia
{
    public sealed class EncyclopediaFeature : IFeature
    {
        public const string FeatureId = "terraria-encyclopedia";
        public const string FeatureName = "Terraria Encyclopedia";
        public const string FeatureVersion = "0.8.0";

        private IFeatureLogger log;
        private EncyclopediaUi ui;
        private ItemBrowserPanel browser;
        private IEncyclopediaFeatureRenderer renderer;
        private bool supported;

        public bool IsOpen => browser?.IsOpen == true;

        public int LoadedItemCount => browser?.LoadedItemCount ?? 0;

        public int LoadedNpcCount => browser?.LoadedNpcCount ?? 0;

        public void Initialise(IFeatureContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            log = context.Log;
            if (!GameVersionGuard.Check())
            {
                log.Warning(
                    FeatureName + ": unsupported Terraria version. Expected "
                    + GameVersionGuard.ExpectedVersion + ", detected "
                    + GameVersionGuard.DetectedVersion
                    + ". Encyclopedia disabled; Terraria continues normally.");
                return;
            }

            renderer = context.Renderer as IEncyclopediaFeatureRenderer;
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    "The active host does not provide the live Terraria Encyclopedia UI adapters.");
            }

            ui = new EncyclopediaUi(renderer, context.Input);
            EncyclopediaUiContext.Set(ui);
            browser = new ItemBrowserPanel(new TerrariaEncyclopediaDataSource());
            context.KeyBindings.RegisterToggle(new InputKey("P"), Toggle);
            supported = true;
            log.Info(
                FeatureName + " v" + FeatureVersion + " initialised. Terraria "
                + GameVersionGuard.DetectedVersion + ", " + ItemID.Count + " item definitions available.");
        }

        public void Update()
        {
            if (!supported)
                return;
            ui.BeginUpdate();
            browser.Update();
        }

        public void Render()
        {
            if (supported)
            {
                browser.Render();
                if (browser.IsOpen)
                {
                    RenderRectangle viewport = renderer.Viewport;
                    renderer.DrawText(
                        "QoL: I sort inventory  O sort open chest  K quick-stack nearby chests (single-player)",
                        new InputPoint(12, Math.Max(12, viewport.Height - renderer.TextLineHeight - 12)),
                        new RenderColour(190, 190, 190));
                }
            }
        }

        public void Unload()
        {
            if (!supported)
                return;

            browser.Unload();
            renderer = null;
            EncyclopediaUiContext.Clear(ui);
            supported = false;
            log.Info(FeatureName + " unloaded.");
        }

        private void Toggle()
        {
            if (supported && !browser.IsTextEntryFocused)
                browser.Toggle();
        }
    }
}
