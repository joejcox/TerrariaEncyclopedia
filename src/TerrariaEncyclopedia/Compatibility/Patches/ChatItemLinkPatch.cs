using System.Reflection;
using HarmonyLib;
using Terraria;
using Terraria.UI.Chat;

namespace TerrariaEncyclopedia.Compatibility.Patches
{
    /// <summary>
    /// Vanilla item-link chat tags (ItemTagHandler.ItemSnippet, what Alt+Click on an item
    /// produces) only override OnHover(), not OnClick() — confirmed via IL disassembly that
    /// TextSnippet.OnClick() is a real, already-wired-up virtual hook (RemadeChatMonitor.DrawChat
    /// calls it for whichever snippet is under the mouse on click), just unused for item links in
    /// vanilla. This postfix adds "open in the Encyclopedia" on top, without touching or
    /// replacing any vanilla click behavior for this or any other snippet type.
    ///
    /// ItemTagHandler.ItemSnippet itself is a non-public nested class (unlike GenerateTag, which
    /// is public on the outer type), so it's matched here purely by reflection — by full type
    /// name, not a compile-time reference — rather than by a direct `is` check.
    /// </summary>
    [HarmonyPatch(typeof(TextSnippet), nameof(TextSnippet.OnClick))]
    public static class ChatItemLinkPatch
    {
        private const string ItemSnippetTypeName = "Terraria.GameContent.UI.Chat.ItemTagHandler+ItemSnippet";
        private static readonly FieldInfo ItemField = typeof(TextSnippet).Assembly
            .GetType(ItemSnippetTypeName)
            ?.GetField("_item", BindingFlags.NonPublic | BindingFlags.Instance);

        private static void Postfix(TextSnippet __instance)
        {
            if (__instance.GetType().FullName != ItemSnippetTypeName) return;
            if (ItemField?.GetValue(__instance) is Item item && item.type > 0)
                Mod.Browser?.OpenItemById(item.type);
        }
    }
}
