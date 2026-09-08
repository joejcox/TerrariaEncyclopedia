#if ENCYCLOPEDIA_FEATURE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;
using TerrariaEncyclopedia.Content;
using TerrariaEncyclopedia.Hosting;
using TerrariaEncyclopedia.Runtime;
using TerrariaEncyclopedia.Runtime.UI.Fna;
using TerrariaEncyclopedia.Runtime.UI.Input;
using TerrariaEncyclopedia.Runtime.UI.Rendering;
using TerrariaEncyclopedia.Runtime.UI.Tooltips;
using TerrariaEncyclopedia.UI;
using TerrariaEncyclopedia.Qol;

namespace TerrariaEncyclopedia.Host.Mono
{
    internal static class GameHost
    {
        private const string SupportedVersion = "1.4.5.8";
        private static readonly object Sync = new object();
        private static string markerPath;
        private static Assembly terrariaAssembly;
        private static bool reLogicLoaded;
        private static bool installationQueued;
        private static bool patchesInstalled;
        private static bool initialised;
        private static FeatureRuntime runtime;
        private static TerrariaFeatureRenderer renderer;
        private static EncyclopediaFeature feature;
        private static long updateCount;
        private static long renderCount;
        private static bool smokeToggleSent;
        private static bool smokeHoverSent;
        private static bool overlayOwnsInputBlock;
        private static bool previousInputBlock;
        private static InventoryQolController inventoryQol;
        private static PlayerClass currentPlayerClass = PlayerClass.Unknown;
        internal static PlayerClass CurrentPlayerClass => currentPlayerClass;
        private static readonly IInventoryQolGame inventoryQolGame = new TerrariaInventoryQolGame();
        private static readonly string inventorySortKey = ReadQolKey("TERRARIA_ENCYCLOPEDIA_SORT_INVENTORY_KEY", "I");
        private static readonly string chestSortKey = ReadQolKey("TERRARIA_ENCYCLOPEDIA_SORT_CHEST_KEY", "O");
        private static readonly string quickStackKey = ReadQolKey("TERRARIA_ENCYCLOPEDIA_QUICK_STACK_KEY", "K");

        public static void Configure(string featureMarkerPath)
        {
            markerPath = featureMarkerPath;
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            AppDomain.CurrentDomain.ProcessExit += AfterProcessExit;
            foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
                ObserveAssembly(loaded);
            InstallPatchesWhenReady();
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            ObserveAssembly(args.LoadedAssembly);
            InstallPatchesWhenReady();
        }

        private static void ObserveAssembly(Assembly assembly)
        {
            string name = assembly.GetName().Name;
            if (string.Equals(name, "Terraria", StringComparison.Ordinal))
                terrariaAssembly = assembly;
            else if (string.Equals(name, "ReLogic", StringComparison.Ordinal))
                reLogicLoaded = true;
        }

        private static void InstallPatchesWhenReady()
        {
            lock (Sync)
            {
                if (terrariaAssembly == null || !reLogicLoaded || patchesInstalled || installationQueued)
                    return;
                installationQueued = true;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    InstallPatches(terrariaAssembly);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine("Terraria Encyclopedia hook installation failed: " + exception);
                    WriteMarker("feature-host-failed", exception.GetType().Name);
                }
            });
        }

        private static void InstallPatches(Assembly terrariaAssembly)
        {
            lock (Sync)
            {
                if (patchesInstalled)
                    return;

                string detected = System.Diagnostics.FileVersionInfo
                    .GetVersionInfo(terrariaAssembly.Location).FileVersion;
                if (!string.Equals(detected, SupportedVersion, StringComparison.Ordinal))
                {
                    Console.Error.WriteLine(
                        "Terraria Encyclopedia host rejected Terraria version "
                        + (detected ?? "unknown") + "; expected " + SupportedVersion + ".");
                    WriteMarker("unsupported-version", detected ?? "unknown");
                    return;
                }

                Main.OnEngineLoad += AfterInitialise;
                // Terraria invokes the third-party tick both before and after input handling.
                // Use the internal tick for normal gameplay so snapshots contain this frame's
                // key and mouse state; the menu-only bridge keeps the feature available while
                // Terraria returns early before the internal tick.
                Main.OnTickForThirdPartySoftwareOnly += AfterMenuTick;
                Main.OnTickForInternalCodeOnly += AfterUpdate;
                Main.OnPostDraw += AfterDraw;

                patchesInstalled = true;
                WriteMarker("patches-installed", detected);
                Console.WriteLine("Terraria Encyclopedia lifecycle patches installed for Terraria " + detected + ".");
            }
        }

        private static void AfterInitialise()
        {
            if (initialised)
                return;

            try
            {
                renderer = new TerrariaFeatureRenderer();
                feature = new EncyclopediaFeature();
                var registry = new FirstPartyFeatureRegistry(
                    new FirstPartyFeature(
                        EncyclopediaFeature.FeatureId,
                        feature));
                runtime = new FeatureRuntime(registry, new ConsoleLogSink(), renderer);
                runtime.Initialise();
                inventoryQol = new InventoryQolController(
                    inventoryQolGame,
                    diagnostic => Console.Error.WriteLine(
                        "Terraria Encyclopedia inventory QoL action failed: " + diagnostic.Exception));

                FeatureStatus status = runtime.Features.Single();
                initialised = status.IsInitialised && status.IsEnabled;
                if (!initialised)
                {
                    WriteMarker("feature-disabled", status.Failure?.Stage.ToString() ?? "initialise");
                    return;
                }

                WriteMarker("feature-initialised", EncyclopediaFeature.FeatureId);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Terraria Encyclopedia initialisation failed: " + exception);
                DisableFeature("feature-host-failed", exception.GetType().Name);
            }
        }

        private static void AfterUpdate()
        {
            if (!initialised)
                return;

            try
            {
                InputSnapshot snapshot = CaptureInput();
                currentPlayerClass = InferCurrentPlayerClass();
                feature?.SetPlayerClass(currentPlayerClass);
                UpdateInventoryQol(snapshot);
                if (!smokeToggleSent
                    && string.Equals(
                        Environment.GetEnvironmentVariable("TERRARIA_ENCYCLOPEDIA_SMOKE_OPEN"),
                        "1",
                        StringComparison.Ordinal))
                {
                    snapshot = new InputSnapshot(
                        new[] { new InputKey("P") },
                        snapshot.MousePosition,
                        null,
                        snapshot.ScrollWheelValue);
                    smokeToggleSent = true;
                }
                else if (smokeToggleSent
                    && !smokeHoverSent
                    && string.Equals(
                        Environment.GetEnvironmentVariable("TERRARIA_ENCYCLOPEDIA_SMOKE_OPEN"),
                        "1",
                        StringComparison.Ordinal))
                {
                    RenderRectangle viewport = renderer.Viewport;
                    snapshot = new InputSnapshot(
                        null,
                        new InputPoint(viewport.Width / 2, viewport.Height / 2),
                        null,
                        snapshot.ScrollWheelValue);
                    smokeHoverSent = true;
                }

                runtime.Update(snapshot);
                SynchroniseInputBlock();
                updateCount++;
                CheckHealthAndWriteMarker();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Terraria Encyclopedia update host failed: " + exception);
                DisableFeature("feature-host-failed", "update-" + exception.GetType().Name);
            }
        }

        private static void AfterMenuTick()
        {
            if (Main.gameMenu)
                AfterUpdate();
        }

        private static void AfterDraw(GameTime gameTime)
        {
            if (!initialised || Main.spriteBatch == null)
                return;

            try
            {
                renderer.BeginFrame(Main.spriteBatch);
                runtime.Render();
                renderCount++;
                CheckHealthAndWriteMarker();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Terraria Encyclopedia render host failed: " + exception);
                DisableFeature("feature-host-failed", "render-" + exception.GetType().Name);
            }
            finally
            {
                try
                {
                    renderer.EndFrame();
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine("Terraria Encyclopedia render cleanup failed: " + exception);
                    DisableFeature("feature-host-failed", "render-cleanup-" + exception.GetType().Name);
                }
                if (initialised && feature?.IsOpen == true)
                {
                    try
                    {
                        renderer.DrawCursor();
                    }
                    catch (Exception exception)
                    {
                        Console.Error.WriteLine("Terraria Encyclopedia cursor redraw failed: " + exception.Message);
                    }
                }
            }
        }

        private static void AfterExit()
        {
            try
            {
                var cleanup = new FeatureFailureCleanup(
                    () => runtime?.Unload(),
                    () => renderer?.ReleaseTextEntry(),
                    RestoreInputBlock);
                cleanup.Run();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Terraria Encyclopedia unload host failed: " + exception);
            }
            finally
            {
                initialised = false;
            }
        }

        private static void AfterProcessExit(object sender, EventArgs args) => AfterExit();

        private static InputSnapshot CaptureInput()
        {
            IEnumerable<Keys> pressedKeys = ReadPressedKeys();
            MouseState mouse = Mouse.GetState();
            IEnumerable<InputKey> keys = pressedKeys
                .Select(key => new InputKey(key.ToString()));
            var buttons = new List<MouseButton>();
            if (mouse.LeftButton == ButtonState.Pressed)
                buttons.Add(MouseButton.Left);
            if (mouse.MiddleButton == ButtonState.Pressed)
                buttons.Add(MouseButton.Middle);
            if (mouse.RightButton == ButtonState.Pressed)
                buttons.Add(MouseButton.Right);
            if (mouse.XButton1 == ButtonState.Pressed)
                buttons.Add(MouseButton.XButton1);
            if (mouse.XButton2 == ButtonState.Pressed)
                buttons.Add(MouseButton.XButton2);

            return new InputSnapshot(
                keys,
                TerrariaUiCoordinateSpace.ToLogicalPointer(
                    PlayerInput.MouseX,
                    PlayerInput.MouseY,
                    Main.UIScale),
                buttons,
                mouse.ScrollWheelValue);
        }

        private static IEnumerable<Keys> ReadPressedKeys()
        {
            // Terraria's public helper is the authoritative snapshot used by the game's
            // input pipeline. Do not merge it with a second snapshot: a lagging FNA snapshot
            // can keep a released key alive and break edge-triggered shortcuts.
            try
            {
                List<Keys> terrariaKeys = PlayerInput.GetPressedKeys();
                if (terrariaKeys != null)
                    return terrariaKeys;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Terraria keyboard state read failed: " + exception.Message);
            }

            // Keep a narrow fallback for game builds where the public helper is unavailable.
            try
            {
                return Keyboard.GetState().GetPressedKeys();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Fallback keyboard state read failed: " + exception.Message);
                return Array.Empty<Keys>();
            }
        }

        private static void UpdateInventoryQol(InputSnapshot snapshot)
        {
            // QoL actions operate on Terraria's native inventory state and remain available while
            // the Encyclopaedia is open. Release edge state when no snapshot is available.
            if (inventoryQol == null)
                return;

            inventoryQol.TryInvokeEdge(IsKeyDown(snapshot, inventorySortKey), InventoryQolAction.SortInventory, out _);
            inventoryQol.TryInvokeEdge(IsKeyDown(snapshot, chestSortKey), InventoryQolAction.SortChest, out _);
            inventoryQol.TryInvokeEdge(IsKeyDown(snapshot, quickStackKey), InventoryQolAction.QuickStackNearby, out _);
        }

        private static PlayerClass InferCurrentPlayerClass()
        {
            try
            {
                Player player = Main.LocalPlayer;
                if (Main.gameMenu || player == null || !player.active)
                    return PlayerClass.Unknown;

                var items = new List<PlayerClassItem>();
                int held = player.selectedItem;
                if (player.inventory != null)
                {
                    for (int i = 0; i < player.inventory.Length; i++)
                    {
                        if (i != held)
                            AddClassItem(items, player.inventory[i], false, false);
                    }
                }
                AddClassItems(items, player.armor, true);
                if (held >= 0 && player.inventory != null && held < player.inventory.Length)
                    AddClassItem(items, player.inventory[held], false, true);
                return PlayerClassInference.Infer(items);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Terraria player class inference failed: " + exception.Message);
                return PlayerClass.Unknown;
            }
        }

        private static void AddClassItems(List<PlayerClassItem> target, Item[] items, bool equipped)
        {
            if (items == null) return;
            // Player.armor stores functional armour/accessories first; later entries are
            // vanity/social slots and must not change the inferred active class.
            int functionalCount = Math.Min(10, items.Length);
            for (int i = 0; i < functionalCount; i++)
                AddClassItem(target, items[i], equipped, false);
        }

        private static void AddClassItem(List<PlayerClassItem> target, Item item, bool equipped, bool held)
        {
            if (item == null || item.IsAir) return;
            target.Add(new PlayerClassItem
            {
                Melee = item.melee,
                Ranged = item.ranged,
                Magic = item.magic,
                Summon = item.summon,
                Sentry = item.sentry,
                Equipped = equipped,
                Held = held,
                Damage = item.damage
            });
        }

        private static string ReadQolKey(string variable, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(variable);
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static bool IsKeyDown(InputSnapshot snapshot, string key)
        {
            return snapshot != null && snapshot.IsKeyDown(new InputKey(key));
        }

        private static void WriteMarker(string phase, string detail)
        {
            if (string.IsNullOrEmpty(markerPath))
                return;

            try
            {
                string temporary = markerPath + "." + System.Diagnostics.Process.GetCurrentProcess().Id
                    .ToString(CultureInfo.InvariantCulture) + ".tmp";
                File.WriteAllLines(temporary, new[]
                {
                    "marker=terraria-encyclopedia-feature-host",
                    "phase=" + phase,
                    "detail=" + (detail ?? string.Empty),
                    "feature-id=" + EncyclopediaFeature.FeatureId,
                    "feature-initialised=" + initialised.ToString().ToLowerInvariant(),
                    "feature-enabled=" + initialised.ToString().ToLowerInvariant(),
                    "version=" + SupportedVersion,
                    "update-count=" + updateCount.ToString(CultureInfo.InvariantCulture),
                    "render-count=" + renderCount.ToString(CultureInfo.InvariantCulture),
                    "panel-open=" + (feature?.IsOpen == true).ToString().ToLowerInvariant(),
                    "item-count=" + (feature?.LoadedItemCount ?? 0).ToString(CultureInfo.InvariantCulture),
                    "npc-count=" + (feature?.LoadedNpcCount ?? 0).ToString(CultureInfo.InvariantCulture),
                    "input-blocked=" + (initialised && Main.blockInput).ToString().ToLowerInvariant(),
                    "smoke-hover-sent=" + smokeHoverSent.ToString().ToLowerInvariant(),
                    "tooltip-build-count=" + (renderer?.TooltipBuildCount ?? 0).ToString(CultureInfo.InvariantCulture),
                    "process-id=" + System.Diagnostics.Process.GetCurrentProcess().Id
                        .ToString(CultureInfo.InvariantCulture)
                });
                if (File.Exists(markerPath))
                    File.Replace(temporary, markerPath, null);
                else
                    File.Move(temporary, markerPath);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Could not write Encyclopedia feature marker: " + exception.Message);
            }
        }

        private static void CheckHealthAndWriteMarker()
        {
            FeatureStatus status = runtime?.Features.SingleOrDefault();
            if (status != null && !status.IsEnabled)
            {
                DisableFeature(
                    "feature-disabled",
                    status.Failure?.Stage.ToString() ?? "unknown");
                return;
            }

            bool smokeOpenExpected = string.Equals(
                Environment.GetEnvironmentVariable("TERRARIA_ENCYCLOPEDIA_SMOKE_OPEN"),
                "1",
                StringComparison.Ordinal);
            bool contentReady = !smokeOpenExpected
                || (feature != null
                    && feature.IsOpen
                    && feature.LoadedItemCount > 0
                    && feature.LoadedNpcCount > 0
                    && smokeHoverSent
                    && renderer != null
                    && renderer.TooltipBuildCount > 0);
            long minimumCallbacks = smokeOpenExpected ? 2 : 1;
            if (status != null
                && status.IsInitialised
                && updateCount >= minimumCallbacks
                && renderCount >= minimumCallbacks
                && contentReady)
                WriteMarker("feature-ready", EncyclopediaFeature.FeatureId);
        }

        private static void SynchroniseInputBlock()
        {
            bool shouldBlock = feature?.IsOpen == true;
            if (shouldBlock && !overlayOwnsInputBlock)
            {
                previousInputBlock = Main.blockInput;
                Main.blockInput = true;
                overlayOwnsInputBlock = true;
            }
            else if (!shouldBlock)
            {
                RestoreInputBlock();
            }
        }

        private static void RestoreInputBlock()
        {
            if (!overlayOwnsInputBlock)
                return;
            Main.blockInput = previousInputBlock;
            overlayOwnsInputBlock = false;
        }

        private static void DisableFeature(string phase, string detail)
        {
            try
            {
                var cleanup = new FeatureFailureCleanup(
                    () => runtime?.Unload(),
                    () => renderer?.ReleaseTextEntry(),
                    RestoreInputBlock);
                cleanup.Run();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Terraria Encyclopedia failure cleanup failed: " + exception);
            }
            finally
            {
                initialised = false;
                WriteMarker(phase, detail);
            }
        }

        private sealed class ConsoleLogSink : IFeatureLogSink
        {
            public void Write(FeatureLogEntry entry)
            {
                string line = "[TerrariaEncyclopedia:" + entry.FeatureId + "] "
                    + entry.Level + ": " + entry.Message;
                if (entry.Level == FeatureLogLevel.Error || entry.Level == FeatureLogLevel.Warning)
                    Console.Error.WriteLine(line);
                else
                    Console.WriteLine(line);
                if (entry.Exception != null)
                    Console.Error.WriteLine(entry.Exception);
            }
        }

        private sealed class TerrariaInventoryQolGame : IInventoryQolGame
        {
            public bool IsGameMenu => Main.gameMenu;

            public bool HasLocalPlayer => Main.LocalPlayer != null && Main.LocalPlayer.active;

            private bool IsClientSession => Main.netMode == 0 || Main.netMode == 1;

            public bool CanSortInventory => IsClientSession && HasLocalPlayer;

            public bool HasOpenChest => IsClientSession && HasLocalPlayer && Main.LocalPlayer.chest >= 0;

            // Vanilla quick-stack acts from the open inventory and discovers nearby chests;
            // an already-open chest is not required.
            public bool CanQuickStack => IsClientSession && HasLocalPlayer && Main.playerInventory;

            public void SortInventory() => ItemSorting.SortInventory();

            public void SortChest() => ItemSorting.SortChest();

            public void SendQuickStackToNearbyChests() => Main.LocalPlayer.QuickStackAllChests();
        }

        private sealed class TerrariaFeatureRenderer : IEncyclopediaFeatureRenderer
        {
            private readonly TerrariaTextEntrySession textEntry = new TerrariaTextEntrySession();
            private readonly TerrariaItemTooltipSource itemTooltips = new TerrariaItemTooltipSource();
            private FnaUiRenderBackend backend;
            private UiRenderer renderer;
            private bool batchActive;

            public RenderRectangle Viewport
            {
                get
                {
                    return TerrariaUiCoordinateSpace.ToLogicalViewport(
                        PlayerInput.RealScreenWidth,
                        PlayerInput.RealScreenHeight,
                        Main.UIScale);
                }
            }

            public ITextEntrySession TextEntry => textEntry;

            public IItemTooltipSource ItemTooltips => itemTooltips;

            public int TooltipBuildCount => itemTooltips.BuildCount;

            public void ReleaseTextEntry() => textEntry.Release();

            public int TextLineHeight => RequireRenderer().TextLineHeight;

            public int MeasureText(string text) => RequireRenderer().MeasureText(text);

            public IDisposable PushClip(RenderRectangle bounds) => RequireRenderer().PushClip(bounds);

            public void FillRectangle(RenderRectangle bounds, RenderColour colour)
            {
                RequireRenderer().FillRectangle(bounds, colour);
            }

            public void StrokeRectangle(RenderRectangle bounds, RenderColour colour, int thickness = 1)
            {
                RequireRenderer().StrokeRectangle(bounds, colour, thickness);
            }

            public void DrawText(string text, InputPoint position, RenderColour colour)
            {
                RequireRenderer().DrawText(text, position, colour);
            }

            public void DrawTerrariaIcon(
                TerrariaIcon icon,
                RenderRectangle destination,
                RenderColour colour)
            {
                FnaTextureRegion region = ResolveIcon(icon);
                if (region.Texture == null)
                    return;
                Rectangle source = region.SourceRectangle
                    ?? new Rectangle(0, 0, region.Texture.Width, region.Texture.Height);
                RequireRenderer().DrawTerrariaIcon(
                    icon,
                    FitDestination(destination, source.Width, source.Height),
                    colour);
            }

            public void BeginFrame(SpriteBatch spriteBatch)
            {
                if (renderer != null)
                    throw new InvalidOperationException("A Terraria UI render frame is already active.");

                object font = ResolveAsset(typeof(FontAssets), "MouseText");
                Matrix transform = Main.UIScaleMatrix;
                var ambient = new FnaSpriteBatchState(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.LinearClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone,
                    null,
                    transform,
                    spriteBatch.GraphicsDevice.ScissorRectangle);
                spriteBatch.Begin(
                    ambient.SortMode,
                    ambient.BlendState,
                    ambient.SamplerState,
                    ambient.DepthStencilState,
                    ambient.RasterizerState,
                    ambient.Effect,
                    ambient.TransformMatrix);
                batchActive = true;
                backend = new FnaUiRenderBackend(
                    spriteBatch,
                    (Texture2D)ResolveAsset(typeof(TextureAssets), "MagicPixel"),
                    Math.Max(1, (int)Math.Ceiling(GetNumericProperty(font, "LineSpacing"))),
                    text => (int)Math.Ceiling(MeasureFont(font, text ?? string.Empty).X),
                    (batch, text, position, colour) => DrawFont(font, batch, text, position, colour),
                    ResolveIcon,
                    () => ambient);
                renderer = new UiRenderer(backend);
            }

            public void EndFrame()
            {
                renderer = null;
                backend?.Dispose();
                backend = null;
                if (batchActive)
                {
                    Main.spriteBatch.End();
                    batchActive = false;
                }
            }

            public void DrawCursor()
            {
                int mouseX = Main.mouseX;
                int mouseY = Main.mouseY;
                int lastMouseX = Main.lastMouseX;
                int lastMouseY = Main.lastMouseY;
                int screenWidth = Main.screenWidth;
                int screenHeight = Main.screenHeight;
                bool cursorBatchActive = false;

                try
                {
                    Main.spriteBatch.Begin(
                        SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        Main.SamplerStateForCursor,
                        DepthStencilState.None,
                        RasterizerState.CullNone,
                        null,
                        Main.UIScaleMatrix);
                    cursorBatchActive = true;
                    PlayerInput.SetZoom_UI();
                    bool smart = Main.SmartCursorIsUsed;
                    Main.DrawCursor(Main.DrawThickCursor(smart), smart);
                }
                finally
                {
                    try
                    {
                        if (cursorBatchActive)
                            Main.spriteBatch.End();
                    }
                    finally
                    {
                        Main.mouseX = mouseX;
                        Main.mouseY = mouseY;
                        Main.lastMouseX = lastMouseX;
                        Main.lastMouseY = lastMouseY;
                        Main.screenWidth = screenWidth;
                        Main.screenHeight = screenHeight;
                    }
                }
            }

            private static FnaTextureRegion ResolveIcon(TerrariaIcon icon)
            {
                switch (icon.Kind)
                {
                    case TerrariaIconKind.Item:
                        Main.instance.LoadItem(icon.Id);
                        Texture2D itemTexture;
                        Rectangle itemFrame;
                        Main.GetItemDrawFrame(icon.Id, out itemTexture, out itemFrame);
                        return new FnaTextureRegion(itemTexture, itemFrame);
                    case TerrariaIconKind.Npc:
                        Main.instance.LoadNPC(icon.Id);
                        Texture2D npc = ResolveAssetArray(typeof(TextureAssets), "Npc", icon.Id);
                        int frameCount = Math.Max(1, Main.npcFrameCount[icon.Id]);
                        return new FnaTextureRegion(
                            npc,
                            new Rectangle(0, 0, npc.Width, Math.Max(1, npc.Height / frameCount)));
                    case TerrariaIconKind.Buff:
                        return new FnaTextureRegion(ResolveAssetArray(typeof(TextureAssets), "Buff", icon.Id));
                    default:
                        return default(FnaTextureRegion);
                }
            }

            private static RenderRectangle FitDestination(
                RenderRectangle bounds,
                int sourceWidth,
                int sourceHeight)
            {
                if (bounds.Width <= 0 || bounds.Height <= 0 || sourceWidth <= 0 || sourceHeight <= 0)
                    return new RenderRectangle(bounds.X, bounds.Y, 0, 0);
                double scale = Math.Min(
                    (double)bounds.Width / sourceWidth,
                    (double)bounds.Height / sourceHeight);
                int width = Math.Max(1, (int)Math.Round(sourceWidth * scale));
                int height = Math.Max(1, (int)Math.Round(sourceHeight * scale));
                return new RenderRectangle(
                    bounds.X + (bounds.Width - width) / 2,
                    bounds.Y + (bounds.Height - height) / 2,
                    width,
                    height);
            }

            private static object ResolveAsset(Type declaringType, string fieldName)
            {
                FieldInfo field = declaringType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
                object asset = field?.GetValue(null)
                    ?? throw new MissingFieldException(declaringType.FullName, fieldName);
                PropertyInfo value = asset.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                return value?.GetValue(asset, null)
                    ?? throw new InvalidOperationException("Terraria asset is unavailable: " + fieldName);
            }

            private static double GetNumericProperty(object target, string propertyName)
            {
                PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                object value = property?.GetValue(target, null)
                    ?? throw new MissingMemberException(target.GetType().FullName, propertyName);
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }

            private static Vector2 MeasureFont(object font, string text)
            {
                MethodInfo method = font.GetType().GetMethod(
                    "MeasureString",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(string) },
                    null);
                return method == null
                    ? Vector2.Zero
                    : (Vector2)method.Invoke(font, new object[] { text });
            }

            private static void DrawFont(
                object font,
                SpriteBatch spriteBatch,
                string text,
                Vector2 position,
                Color colour)
            {
                Utils.DrawBorderString(spriteBatch, text, position, colour, 1f, 0f, 0f, -1);
            }

            private static Texture2D ResolveAssetArray(Type declaringType, string fieldName, int index)
            {
                FieldInfo field = declaringType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
                var assets = field?.GetValue(null) as Array;
                if (assets == null || index < 0 || index >= assets.Length)
                    return null;
                object asset = assets.GetValue(index);
                if (asset == null)
                    return null;
                PropertyInfo value = asset.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                return value?.GetValue(asset, null) as Texture2D;
            }

            private UiRenderer RequireRenderer()
            {
                return renderer ?? throw new InvalidOperationException(
                    "The Encyclopedia attempted to draw outside Terraria's UI render frame.");
            }
        }

        private sealed class TerrariaTextEntrySession : ITextEntrySession
        {
            private string owner;
            private bool previousWritingText;
            private bool previousBlockInput;

            public void Begin(string ownerId)
            {
                if (owner != null)
                    return;
                owner = ownerId;
                previousWritingText = PlayerInput.WritingText;
                previousBlockInput = Main.blockInput;
                PlayerInput.WritingText = true;
                Main.blockInput = true;
            }

            public string Read(string currentText, int maxLength)
            {
                string next = Main.GetInputText(currentText ?? string.Empty);
                return next != null && next.Length > maxLength
                    ? next.Substring(0, maxLength)
                    : next ?? currentText ?? string.Empty;
            }

            public void End(string ownerId)
            {
                if (!string.Equals(owner, ownerId, StringComparison.Ordinal))
                    return;
                Release();
            }

            public void Release()
            {
                if (owner == null)
                    return;
                PlayerInput.WritingText = previousWritingText;
                Main.blockInput = previousBlockInput;
                owner = null;
            }
        }

        private sealed class TerrariaItemTooltipSource : IItemTooltipSource
        {
            public int BuildCount { get; private set; }

            public IReadOnlyList<TooltipLine> Build(int itemType, int prefix, int stack)
            {
                BuildCount++;
                var item = new Item();
                item.SetDefaults(itemType);
                if (prefix > 0)
                    item.Prefix(prefix);
                item.stack = Math.Max(1, stack);

                var lines = new List<TooltipLine>
                {
                    new TooltipLine(item.AffixName(), RarityColour(item.rare))
                };
                if (item.damage > 0)
                {
                    string damageType = item.melee ? "melee"
                        : item.ranged ? "ranged"
                        : item.magic ? "magic"
                        : item.summon ? "summon"
                        : string.Empty;
                    lines.Add(Line(item.damage + (damageType.Length == 0 ? "" : " " + damageType) + " damage"));
                    int criticalChance = EncyclopediaQueries.DisplayedCriticalChance(
                        item.crit,
                        item.melee,
                        item.ranged,
                        item.magic,
                        item.summon);
                    if (criticalChance > 0)
                        lines.Add(Line(criticalChance + "% critical strike chance"));
                    if (item.useTime > 0)
                        lines.Add(Line(UseSpeed(item.useTime)));
                    if (item.knockBack > 0f)
                        lines.Add(Line(Knockback(item.knockBack)));
                }
                if (item.defense > 0)
                    lines.Add(Line(item.defense + " defence"));
                if (item.pick > 0)
                    lines.Add(Line(item.pick + "% pickaxe power"));
                if (item.axe > 0)
                    lines.Add(Line(item.axe * 5 + "% axe power"));
                if (item.hammer > 0)
                    lines.Add(Line(item.hammer + "% hammer power"));
                if (item.healLife > 0)
                    lines.Add(Line("Restores " + item.healLife + " life"));
                if (item.healMana > 0)
                    lines.Add(Line("Restores " + item.healMana + " mana"));
                if (item.accessory)
                    lines.Add(Line("Equippable"));
                if (item.vanity)
                    lines.Add(Line("Vanity item"));
                if (item.consumable)
                    lines.Add(Line("Consumable"));
                if (item.material)
                    lines.Add(Line("Material"));
                AddVanillaDescription(lines, itemType);
                if (item.buffType > 0 && item.buffTime > 0)
                    lines.Add(Line("Grants " + Lang.GetBuffName(item.buffType) + " for " + item.buffTime / 60 + " seconds"));
                if (item.maxStack > 1)
                    lines.Add(DimLine("Max stack: " + item.maxStack));
                if (item.value > 0)
                    lines.Add(DimLine("Value: " + FormatCoins(item.value)));
                if (item.expert)
                    lines.Add(new TooltipLine("Expert", new RenderColour(255, 200, 100)));
                return lines;
            }

            private static TooltipLine Line(string text) =>
                new TooltipLine(text, new RenderColour(255, 255, 255));

            private static TooltipLine DimLine(string text) =>
                new TooltipLine(text, new RenderColour(180, 180, 180));

            private static RenderColour RarityColour(int rarity)
            {
                if (rarity < 0) return new RenderColour(130, 130, 130);
                switch (rarity)
                {
                    case 1: return new RenderColour(150, 150, 255);
                    case 2: return new RenderColour(150, 255, 150);
                    case 3: return new RenderColour(255, 200, 150);
                    case 4: return new RenderColour(255, 150, 150);
                    case 5: return new RenderColour(255, 150, 255);
                    case 6: return new RenderColour(210, 160, 255);
                    case 7: return new RenderColour(150, 255, 255);
                    case 8: return new RenderColour(255, 255, 100);
                    case 9: return new RenderColour(120, 255, 120);
                    case 10: return new RenderColour(255, 80, 80);
                    default: return new RenderColour(255, 255, 255);
                }
            }

            private static string UseSpeed(int useTime)
            {
                if (useTime <= 8) return "Insanely fast speed";
                if (useTime <= 20) return "Very fast speed";
                if (useTime <= 25) return "Fast speed";
                if (useTime <= 30) return "Average speed";
                if (useTime <= 35) return "Slow speed";
                if (useTime <= 45) return "Very slow speed";
                return "Extremely slow speed";
            }

            private static string Knockback(float value)
            {
                if (value <= 1f) return "Extremely weak knockback";
                if (value <= 3f) return "Very weak knockback";
                if (value <= 4f) return "Weak knockback";
                if (value <= 6f) return "Average knockback";
                if (value <= 7f) return "Strong knockback";
                if (value <= 9f) return "Very strong knockback";
                if (value <= 11f) return "Extremely strong knockback";
                return "Insane knockback";
            }

            private static string FormatCoins(int copper)
            {
                int platinum = copper / 1000000;
                int gold = copper / 10000 % 100;
                int silver = copper / 100 % 100;
                int remainder = copper % 100;
                var parts = new List<string>();
                if (platinum > 0) parts.Add(platinum + " platinum");
                if (gold > 0) parts.Add(gold + " gold");
                if (silver > 0) parts.Add(silver + " silver");
                if (remainder > 0) parts.Add(remainder + " copper");
                return parts.Count == 0 ? "0 copper" : string.Join(" ", parts);
            }

            private static void AddVanillaDescription(List<TooltipLine> lines, int itemType)
            {
                MethodInfo getTooltip = typeof(Lang).GetMethod(
                    "GetTooltip",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new[] { typeof(int) },
                    null);
                object tooltip = getTooltip?.Invoke(null, new object[] { itemType });
                if (tooltip == null)
                    return;

                PropertyInfo lineCountProperty = tooltip.GetType().GetProperty(
                    "Lines",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo getLine = tooltip.GetType().GetMethod(
                    "GetLine",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new[] { typeof(int) },
                    null);
                if (lineCountProperty == null || getLine == null)
                    return;
                int lineCount = Convert.ToInt32(lineCountProperty.GetValue(tooltip, null), CultureInfo.InvariantCulture);
                for (int index = 0; index < lineCount; index++)
                {
                    string text = getLine.Invoke(tooltip, new object[] { index }) as string;
                    if (!string.IsNullOrWhiteSpace(text))
                        lines.Add(Line(text));
                }
            }
        }
    }
}
#endif
