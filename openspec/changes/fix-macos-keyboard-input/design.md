# Design: Restore macOS keyboard input

`GameHost` reads `PlayerInput.GetPressedKeys()` once per Terraria update from `Main.OnTickForInternalCodeOnly`, which Terraria raises after its normal input handling. The menu-only third-party tick bridge is used because Terraria returns before the internal tick while the main menu is active. This avoids processing the pre-input third-party callback and avoids combining snapshots observed on different frames. If the helper throws or returns no collection on a compatible game build, the host falls back to FNA's `Keyboard.GetState().GetPressedKeys()`.

The existing `InputSnapshot` and `FeatureRuntime` remain responsible for shortcut edge detection. This keeps P, I, O, K, and Escape behaviour unchanged while allowing the same key names to reach search text entry. No key state is persisted between frames and no game state is mutated.

Verification combines the host Release build, the existing macOS smoke harness, and a manual in-game check of shortcuts and text entry because the smoke harness injects a synthetic open-panel key rather than generating physical macOS key events.
